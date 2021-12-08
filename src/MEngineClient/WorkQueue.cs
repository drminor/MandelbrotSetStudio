using MEngineDataContracts;
using MSS.Common;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class WorkQueue
	{
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;

		private const int MAX_WORK_ITEMS = 4;
		private readonly BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> _workQueue;

		private readonly CancellationTokenSource _cts;
		private Task _workQueueProcessor1;
		private Task _workQueueProcessor2;

		public WorkQueue(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_workQueue = new BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> (MAX_WORK_ITEMS);
			_cts = new CancellationTokenSource();

			_workQueueProcessor1 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
			_workQueueProcessor2 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
		}

		public void AddWork(MapSectionRequest mapSectionRequest, Action<MapSectionResponse> workAction)
		{
			var mapSectionWorkItem = new WorkItem<MapSectionRequest, MapSectionResponse>(mapSectionRequest, workAction);
			_workQueue.Add(mapSectionWorkItem);
		}

		public void Stop()
		{
			_workQueue.CompleteAdding();
			_workQueueProcessor1.Wait(120 * 1000);
			_workQueueProcessor2.Wait(120 * 1000);
			_cts.Cancel();
		}

		private async Task ProcessTheQueueAsync(CancellationToken ct)
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
						var mapSectionId = await _mapSectionRepo.SaveMapSectionAsync(mapSectionResponse);

						mapSectionResponse.MapSectionId = mapSectionId;
					}

					workItem.WorkAction(mapSectionResponse);
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("The work queue got a TCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Got Exception: {e}.");
					throw;
				}
			}
		}

	}
}
