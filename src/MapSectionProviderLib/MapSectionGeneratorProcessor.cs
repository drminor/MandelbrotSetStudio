using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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

		private bool _stopped;

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient[] mEngineClients, bool useAllCores)
		{
			_stopped = false;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSectionGenerateRequest>(QUEUE_CAPACITY);
			_cancelledJobIds = new List<int>();

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
					//nClient = new MClientLocalScalar();
					nClient = new MClientLocal(mClientLocal.UsingSingleLimb, mClientLocal.UsingDepthFirst);
				}
				else if (clients[0] is MClient mClient)
				{
					nClient = new MClient(mClient.EndPointAddress);
				}
				else
				{
					throw new NotSupportedException("Currently, only the MClient and MClientLocal implementations of IMEngineClient are supported.");
					//nClient = new MClientLocalVector();
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

					if (IsJobCancelled(mapSectionGenerateRequest.JobId))
					{
						mapSectionResponse = null;
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionResponse = await mEngineClient.GenerateMapSectionAsync(mapSectionRequest);
						mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

						if (mapSectionResponse.MapSectionVectors == null)
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

					if (IsJobCancelled(mapSectionGenerateRequest.JobId))
					{
						mapSectionResponse = null;
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionResponse = mEngineClient.GenerateMapSection(mapSectionRequest);
						mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

						if (mapSectionResponse.MapSectionVectors == null)
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
