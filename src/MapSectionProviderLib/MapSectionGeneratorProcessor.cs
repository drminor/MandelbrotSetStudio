using MEngineDataContracts;
using MSS.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionGeneratorProcessor : IDisposable
	{
		//private const int NUMBER_OF_CONSUMERS = 4;
		private const int QUEUE_CAPACITY = 200;

		private readonly IMEngineClient[] _mEngineClients;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionGenerateRequest> _workQueue;

		private readonly IList<Task> _workQueueProcessors;

		private readonly object _cancelledJobsLock = new();
		private readonly List<int> _cancelledJobIds;

		private bool disposedValue;

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient[] mEngineClients)
		{
			_mEngineClients = mEngineClients;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSectionGenerateRequest>(QUEUE_CAPACITY);
			_cancelledJobIds = new List<int>();

			var numberOfLogicalProc = Environment.ProcessorCount;
			//var localTaskCnt = 1;
			//var remoteTaskCnt = 0;

			var localTaskCnt = numberOfLogicalProc - 1;
			var remoteTaskCnt = localTaskCnt - 1;

			_workQueueProcessors = new List<Task>();
			foreach (var client in _mEngineClients)
			{
				if (client.IsLocal)
				{
					for (var i = 0; i < localTaskCnt; i++)
					{
						_workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(client/*, _mapSectionPersistProcessor*/, _cts.Token)));
					}
				}
				else
				{
					for (var i = 0; i < remoteTaskCnt; i++)
					{
						_workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(client/*, _mapSectionPersistProcessor*/, _cts.Token)));
					}
				}
			}
		}

		#endregion

		#region Public Methods

		internal void AddWork(MapSectionGenerateRequest mapSectionWorkItem)
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

		public void CancelJob(int jobId)
		{
			lock (_cancelledJobsLock)
			{
				if (!_cancelledJobIds.Contains(jobId))
				{
					_cancelledJobIds.Add(jobId);
				}
			}
		}

		public void Stop(bool immediately)
		{
			lock (_cancelledJobsLock)
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
				for (var i = 0; i < _workQueueProcessors.Count; i++)
				{
					_ = _workQueueProcessors[i].Wait(120 * 1000);
					Debug.WriteLine($"The MapSectionGeneratorProcesssor's WorkQueueProcessor Task #{i} has completed.");
				}
			}
			catch { }
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(IMEngineClient mEngineClient, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionGenerateRequest = _workQueue.Take(ct);

					// The original request is in the Request's Request property.
					var mapSectionRequest = mapSectionGenerateRequest.Request.Request;

					MapSectionResponse? mapSectionResponse;

					if (IsJobCancelled(mapSectionGenerateRequest.JobId))
					{
						mapSectionResponse = null;
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionResponse = await mEngineClient.GenerateMapSectionAsync(mapSectionRequest);

						if (mapSectionResponse.IsEmpty)
						{
							Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
						}

						if (mapSectionRequest.MapSectionId != null)
						{
							Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
						}

						mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;
					}

					mapSectionGenerateRequest.Response = mapSectionResponse;
					mapSectionGenerateRequest.RunWorkAction();
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The response queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"ERROR: The response queue got an exception. The current client has address: {mEngineClient?.EndPointAddress ?? "No Current Client" }. The exception is {e}.");
					throw;
				}
			}
		}

		private bool IsJobCancelled(int jobId)
		{
			bool result;
			lock (_cancelledJobsLock)
			{
				result = _cancelledJobIds.Contains(jobId);
			}

			return result;
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

					//for (var i = 0; i < _workQueueProcessors.Count; i++)
					//{
					//	if (_workQueueProcessors[i] != null)
					//	{
					//		_workQueueProcessors[i].Dispose();
					//	}
					//}

					//if (_mapSectionPersistProcessor != null)
					//{
					//	_mapSectionPersistProcessor.Dispose();
					//}
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
