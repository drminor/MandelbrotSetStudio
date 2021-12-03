using MEngineDataContracts;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class WorkQueue
	{
		private readonly MClient _mClient;
		private const int MAX_WORK_ITEMS = 4;
		private readonly BlockingCollection<MapSectionWorkItem> _workQueue;

		private readonly CancellationTokenSource _cts;
		//private Task _workQueueProcessor;

		public WorkQueue(MClient mClient)
		{
			_mClient = mClient;
			_workQueue = new BlockingCollection<MapSectionWorkItem>(MAX_WORK_ITEMS);
			_cts = new CancellationTokenSource();

			//_workQueueProcessor = ProcessTheQueueAsync(_cts.Token);

			Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
		}

		public void AddWork(MapSectionRequest mapSectionRequest, Action<MapSectionResponse> workAction)
		{
			var mapSectionWorkItem = new MapSectionWorkItem(mapSectionRequest, workAction);
			_workQueue.Add(mapSectionWorkItem);
		}

		public void Stop()
		{
			_cts.Cancel();
			//_workQueueProcessor = null;
		}

		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			while(!ct.IsCancellationRequested)
			{
				try
				{
					var workItem = _workQueue.Take(ct);
					var mapSectionResponse = await _mClient.GenerateMapSectionAsync(workItem.MapSectionRequest);
					workItem.WorkAction(mapSectionResponse);
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("The work queue got a TCE.");
				}

			}
		}

	}
}
