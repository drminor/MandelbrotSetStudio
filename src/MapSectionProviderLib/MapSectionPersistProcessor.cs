using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionPersistProcessor : IDisposable
	{
		private readonly MapSectionValuesPool _mapSectionValuesPool;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private const int QUEUE_CAPACITY = 200;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionResponse> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		private readonly object _queueLock = new();

		#region Constructor

		public MapSectionPersistProcessor(MapSectionValuesPool mapSectionValuesPool, IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionValuesPool = mapSectionValuesPool;
			_mapSectionAdapter = mapSectionAdapter;
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSectionResponse>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
		}

		#endregion

		#region Public Methods

		public void AddWork(MapSectionResponse mapSectionResponse)
		{
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionResponse);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionResponse}, Adding has been completed.");
			}
		}

		public void Stop(bool immediately)
		{
			lock (_queueLock)
			{
				if (immediately)
				{
					_cts.Cancel();
				}
				else
				{
					if (!_workQueue.IsCompleted && !_workQueue.IsAddingCompleted)
					{
						_workQueue.CompleteAdding();
					}
				}
			}

			try
			{
				_ =_workQueueProcessor.Wait(120 * 1000);
				Debug.WriteLine("The MapSectionPersistProcesssor's WorkQueueProcessor Task has completed.");
			}
			catch
			{ }
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionResponse = _workQueue.Take(ct);

					var mapSectionValues = mapSectionResponse.MapSectionValues;

					if (mapSectionValues != null)
					{
						if (mapSectionResponse.RecordOnFile)
						{
							//Debug.WriteLine($"Updating Z Values for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");
							//_ = await _mapSectionAdapter.UpdateMapSectionZValuesAsync(mapSectionResponse);

							// TODO: The OwnerId may already be on file for this MapSection -- or not.
						}
						else
						{
							Debug.WriteLine($"Creating MapSection with block position: {mapSectionResponse.BlockPosition}. MapSectionId: {mapSectionResponse.MapSectionId}.");
							var mapSectionId = await _mapSectionAdapter.SaveMapSectionAsync(mapSectionResponse);
							mapSectionResponse.MapSectionId = mapSectionId.ToString();

							_ = await _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse);
						}

						if (!_mapSectionValuesPool.Free(mapSectionValues))
						{
							mapSectionValues.Dispose();
						}
					}
					else
					{
						Debug.WriteLine($"The MapSectionPersist Processor received an empty MapSectionResponse.");
					}
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The persist queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The persist queue got an exception: {e}.");
					Console.WriteLine($"\n\nWARNING:The persist queue got an exception: {e}.\n\n");
					//throw;
				}
			}
		}

		#endregion

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					Stop(true);

					if (_cts != null)
					{
						_cts.Dispose();
					}

					if (_workQueue != null)
					{
						_workQueue.Dispose();
					}

					//if (_workQueueProcessor != null)
					//{
					//	_workQueueProcessor.Dispose();
					//}
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
