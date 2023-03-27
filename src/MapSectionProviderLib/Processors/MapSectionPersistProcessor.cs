using MongoDB.Bson;
using MSS.Common;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionPersistProcessor : IDisposable
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionHelper _mapSectionHelper;

		private const int QUEUE_CAPACITY = 200;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionPersistRequest> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		private readonly object _queueLock = new();

		#region Constructor

		public MapSectionPersistProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionHelper mapSectionHelper)
		{
			_mapSectionHelper = mapSectionHelper;
			_mapSectionAdapter = mapSectionAdapter;
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSectionPersistRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
		}

		#endregion

		#region Public Methods

		internal void AddWork(MapSectionPersistRequest mapSectionWorkItem)
		{
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionWorkItem.Request}, Adding has been completed.");
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
					var mapSectionPersistRequest = _workQueue.Take(ct);
					var mapSectionResponse = mapSectionPersistRequest.Response;

					if (mapSectionPersistRequest.OnlyInsertJobMapSectionRecord)
					{
						var mapSectionRequest = mapSectionPersistRequest.Request;
						_ = await _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse, mapSectionRequest.BlockPosition, mapSectionRequest.IsInverted);
					}
					else
					{
						if (mapSectionResponse.MapSectionVectors != null)
						{
							await PersistTheCountsAndZValuesAsync(mapSectionPersistRequest.Request, mapSectionResponse);
						}
						else
						{
							Debug.WriteLine($"The MapSectionPersist Processor received an empty MapSectionResponse.");
						}
					}

					_mapSectionHelper.ReturnMapSectionResponse(mapSectionResponse);
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

		private async Task PersistTheCountsAndZValuesAsync(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			if (mapSectionResponse.RecordOnFile)
			{
				var mapSectionId = new ObjectId(mapSectionResponse.MapSectionId!);
				Debug.WriteLine($"Updating Z Values for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");

				_ = await _mapSectionAdapter.UpdateCountValuesAync(mapSectionResponse);

				if (mapSectionResponse.MapSectionZVectors != null)
				{
					if (mapSectionResponse.AllRowsHaveEscaped)
					{
						_ = await _mapSectionAdapter.DeleteZValuesAync(mapSectionId);
					}
					else
					{
						_ = await _mapSectionAdapter.UpdateZValuesAync(mapSectionResponse, mapSectionId);
					}

					// TODO: The OwnerId may already be on file for this MapSection -- or not.
				}
			}
			else
			{
				//Debug.WriteLine($"Creating MapSection for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");
				var mapSectionId = await _mapSectionAdapter.SaveMapSectionAsync(mapSectionResponse);

				if (mapSectionId.HasValue)
				{
					mapSectionResponse.MapSectionId = mapSectionId.ToString();

					if (mapSectionResponse.MapSectionZVectors != null & !mapSectionResponse.AllRowsHaveEscaped)
					{
						_ = await _mapSectionAdapter.SaveMapSectionZValuesAsync(mapSectionResponse, mapSectionId.Value);
					}

					 _ = await _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse, mapSectionRequest.BlockPosition, mapSectionRequest.IsInverted);
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
