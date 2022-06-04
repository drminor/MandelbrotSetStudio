using MEngineDataContracts;
using MSS.Common.MSetRepo;
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
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private const int QUEUE_CAPACITY = 200;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionResponse> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		private readonly object _queueLock = new();

		private StringBuilder _sbForDiag;

		#region Constructor

		public MapSectionPersistProcessor(IMapSectionAdapter mapSectionAdapter)
		{
			_sbForDiag = new StringBuilder();
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
				_workQueueProcessor.Wait(120 * 1000);
			}
			catch
			{ }
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionResponse = _workQueue.Take(ct);

					if (mapSectionResponse.Counts != null)
					{
						if (mapSectionResponse.MapSectionId != null)
						{
							_sbForDiag.AppendLine($"Updating Z Values for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");
							_ = await _mapSectionAdapter.UpdateMapSectionZValuesAsync(mapSectionResponse);
						}
						else
						{
							_sbForDiag.AppendLine($"Creating new MapSection. bp: {mapSectionResponse.BlockPosition}.");
							_ = await _mapSectionAdapter.SaveMapSectionAsync(mapSectionResponse);
						}
					}
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The persist queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The persist queue got an exception: {e}.");
					Debug.WriteLine($"The recent operations are\r\n {_sbForDiag}");
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

					if (_workQueueProcessor != null)
					{
						_workQueueProcessor.Dispose();
					}
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
