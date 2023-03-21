using MEngineClient;
using MSS.Common;
using MSS.Types.MSet;
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

		#region Private Properties

		private const int QUEUE_CAPACITY = 200;

		private readonly IMEngineClient[] _mEngineClients;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionGenerateRequest> _workQueue;

		private readonly IList<Task> _workQueueProcessors;

		private readonly object _jobsStatusLock = new();
		private readonly Dictionary<int, CancellationTokenSource> _jobs;

		private bool disposedValue;

		private bool _stopped;

		#endregion

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient[] mEngineClients, bool useAllCores)
		{
			_stopped = false;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSectionGenerateRequest>(QUEUE_CAPACITY);
			_jobs = new Dictionary<int, CancellationTokenSource>();

			if (mEngineClients.Length == 1)
			{
				_workQueueProcessors = CreateTheQueueProcessorsN(useAllCores, ref mEngineClients);
				_mEngineClients = mEngineClients;
			}
			else
			{
				_mEngineClients = mEngineClients;
				_workQueueProcessors = CreateTheQueueProcessorsStandard(useAllCores);
			}
		}

		private IList<Task> CreateTheQueueProcessorsStandard(bool useAllCores)
		{
			int localTaskCnt;
			int remoteTaskCnt;

			if (useAllCores)
			{
				var numberOfLogicalProc = Environment.ProcessorCount;
				localTaskCnt = numberOfLogicalProc - 1;
				//localTaskCnt = 2;
				remoteTaskCnt = localTaskCnt - 1;
			}
			else
			{
				localTaskCnt = 1;
				remoteTaskCnt = 0;
			}

			var workQueueProcessors = new List<Task>();
			foreach (var client in _mEngineClients)
			{
				if (client.IsLocal)
				{
					for (var i = 0; i < localTaskCnt; i++)
					{
						//workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(client/*, _mapSectionPersistProcessor*/, _cts.Token)));
						workQueueProcessors.Add(Task.Run(() => ProcessTheQueue(client, _cts.Token)));
					}
				}
				else
				{
					for (var i = 0; i < remoteTaskCnt; i++)
					{
						//workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(client/*, _mapSectionPersistProcessor*/, _cts.Token)));
						workQueueProcessors.Add(Task.Run(() => ProcessTheQueue(client, _cts.Token)));
					}
				}
			}

			return workQueueProcessors;
		}

		// TODO: Fix the MapSectionGeneratorProcessor's Constructor. We need to provide a IMEngineClient factory instead of an array of IMEngineClient instances.

		private IList<Task> CreateTheQueueProcessorsN(bool useAllCores, ref IMEngineClient[] clients)
		{
			int localTaskCnt;

			if (useAllCores)
			{
				var numberOfLogicalProc = Environment.ProcessorCount;
				localTaskCnt = numberOfLogicalProc - 1;
			}
			else
			{
				localTaskCnt = 1;
			}

			var workQueueProcessors = new List<Task>();

			var newClients = new IMEngineClient[localTaskCnt];

			for (var i = 0; i < localTaskCnt; i++)
			{
				IMEngineClient nClient;

				if (clients[0] is MClientLocal mClientLocal)
				{
					nClient = new MClientLocal(mClientLocal.MSetGenerationStrategy);
				}
				else if (clients[0] is MClient mClient)
				{
					nClient = new MClient(mClient.EndPointAddress);
				}
				else
				{
					throw new NotSupportedException("Currently, only the MClient and MClientLocal implementations of IMEngineClient are supported.");
				}

				//workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(nClient/*, _mapSectionPersistProcessor*/, _cts.Token)));
				workQueueProcessors.Add(Task.Run(() => ProcessTheQueue(nClient, _cts.Token)));

				newClients[i] = nClient;
			}

			clients = newClients;

			return workQueueProcessors;
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
			lock (_jobsStatusLock)
			{
				if (!_jobs.ContainsKey(jobId))
				{
					var cts = new CancellationTokenSource();
					cts.Cancel();
					_jobs.Add(jobId, cts);
				}
				else
				{
					_jobs[jobId].Cancel();
				}
			}
		}

		public void MarkJobAsComplete(int jobId)
		{
			lock (_jobsStatusLock)
			{
				if (_jobs.TryGetValue(jobId, out var cts))
				{
					_jobs.Remove(jobId);
				}
			}
		}


		public void Stop(bool immediately)
		{
			lock (_jobsStatusLock)
			{
				if (_stopped)
				{
					return;
				}

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

				_stopped = true;
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

					if (IsJobCancelled(mapSectionGenerateRequest.JobId, out var cts))
					{
						mapSectionResponse = new MapSectionResponse(mapSectionRequest, isCancelled: true);
						var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
						mapSectionResponse.MapSectionVectors = msv;
						mapSectionResponse.MapSectionZVectors = mszv;

					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
						mapSectionResponse = await mEngineClient.GenerateMapSectionAsync(mapSectionRequest, cts.Token);
						//mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

						if (!cts.Token.IsCancellationRequested && mapSectionResponse.MapSectionVectors == null)
						{
							Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
						}

						if (mapSectionRequest.MapSectionId != null)
						{
							Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
						}
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

		private void ProcessTheQueue(IMEngineClient mEngineClient, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionGenerateRequest = _workQueue.Take(ct);

					// The original request is in the Request's Request property.
					var mapSectionRequest = mapSectionGenerateRequest.Request.Request;

					MapSectionResponse? mapSectionResponse;

					if (IsJobCancelled(mapSectionGenerateRequest.JobId, out var cts))
					{
						mapSectionResponse = new MapSectionResponse(mapSectionRequest, isCancelled: true);
						var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
						mapSectionResponse.MapSectionVectors = msv;
						mapSectionResponse.MapSectionZVectors = mszv;
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
						mapSectionResponse = mEngineClient.GenerateMapSection(mapSectionRequest, cts.Token);
						//mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

						if (!cts.Token.IsCancellationRequested && mapSectionResponse.MapSectionVectors == null)
						{
							Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
						}

						if (mapSectionRequest.MapSectionId != null)
						{
							Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
						}
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
					Debug.WriteLine($"ERROR: The response queue got an exception. The current client has address: {mEngineClient?.EndPointAddress ?? "No Current Client"}. The exception is {e}.");
					throw;
				}
			}
		}

		private bool IsJobCancelled(int jobId, out CancellationTokenSource cts)
		{
			bool result;
			lock (_jobsStatusLock)
			{
				if (_jobs.ContainsKey(jobId))
				{
					cts = _jobs[jobId];
					result = cts.IsCancellationRequested;
				}
				else
				{
					cts = new CancellationTokenSource();
					_jobs.Add(jobId, cts);
					result = false;
				}
			}

			if (result)
			{
				Debug.WriteLine($"The Job: {jobId} has been cancelled.");
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
