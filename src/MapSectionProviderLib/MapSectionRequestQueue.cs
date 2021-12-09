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
	public class MapSectionRequestQueue
	{
		private const int MAX_WORK_ITEMS = 4;

		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;

		private readonly MapSectionPersistQueue _mapSectionPersistQueue;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> _workQueue;

		private Task _workQueueProcessor1;
		private Task _workQueueProcessor2;

		public MapSectionRequestQueue(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo, MapSectionPersistQueue mapSectionPersistQueue)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_mapSectionPersistQueue = mapSectionPersistQueue;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> (MAX_WORK_ITEMS);

			if (mapSectionPersistQueue != null)
			{
				_workQueueProcessor1 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistQueue, _cts.Token));
				_workQueueProcessor2 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistQueue, _cts.Token));
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

			_mapSectionPersistQueue?.Stop(immediately);
		}

		private async Task ProcessTheQueueAsync(MapSectionPersistQueue mapSectionPersistQueue, CancellationToken ct)
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

						mapSectionPersistQueue.AddWork(mapSectionResponse);
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

	}
}
