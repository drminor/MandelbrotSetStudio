using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		private const int MAX_WORK_ITEMS = 4;

		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;

		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> _workQueue;

		private readonly Task _workQueueProcessor1;
		private readonly Task _workQueueProcessor2;
		private bool disposedValue;

		public MapSectionRequestProcessor(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> (MAX_WORK_ITEMS);

			if (mapSectionPersistProcessor != null)
			{
				_workQueueProcessor1 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
				_workQueueProcessor2 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
			}
			else
			{
				_workQueueProcessor1 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
				_workQueueProcessor2 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
			}
		}

		public void AddWork(MapSectionRequest mapSectionRequest, Action<MapSectionResponse> workAction)
		{
			var mapSectionWorkItem = new WorkItem<MapSectionRequest, MapSectionResponse>(mapSectionRequest, workAction);
			_workQueue.Add(mapSectionWorkItem);
		}

		public void Stop(bool immediately)
		{
			if (immediately)
			{
				_cts.Cancel();
			}
			else
			{
				_workQueue.CompleteAdding();
			}

			_workQueueProcessor1.Wait(120 * 1000);
			_workQueueProcessor2.Wait(120 * 1000);

			_mapSectionPersistProcessor?.Stop(immediately);
		}

		public long? ClearMapSections(string subdivisionId)
		{
			return _mapSectionRepo.ClearMapSections(subdivisionId);
		}

		private async Task ProcessTheQueueAsync(MapSectionPersistProcessor mapSectionPersistProcessor, CancellationToken ct)
		{
			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var workItem = _workQueue.Take(ct);
					var blockPosition = workItem.Request.BlockPosition;
					var mapSectionResponse = await _mapSectionRepo.GetMapSectionAsync(workItem.Request.SubdivisionId, blockPosition);

					if (mapSectionResponse is null)
					{
						Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(workItem.Request);

						mapSectionPersistProcessor.AddWork(mapSectionResponse);
					}

					workItem.WorkAction(mapSectionResponse);
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("The work queue got a TCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The work queue got an exception: {e}.");
					throw;
				}
			}
		}

		// Without using the repository.
		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var workItem = _workQueue.Take(ct);
					var blockPosition = workItem.Request.BlockPosition;
					Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
					var mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(workItem.Request);

					workItem.WorkAction(mapSectionResponse);
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("The work queue got a TCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The work queue got an exception: {e}.");
					throw;
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Stop(false);
					// Dispose managed state (managed objects)
					if (_cts != null)
					{
						_cts.Dispose();
					}

					if (_workQueue != null)
					{
						_workQueue.Dispose();
					}

					if (_workQueueProcessor1 != null)
					{
						_workQueueProcessor1.Dispose();
					}

					if (_workQueueProcessor2 != null)
					{
						_workQueueProcessor2.Dispose();
					}

					if (_mapSectionPersistProcessor != null)
					{
						_mapSectionPersistProcessor.Dispose();
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
	}
}
