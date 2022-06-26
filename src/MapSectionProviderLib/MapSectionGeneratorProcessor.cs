using MEngineDataContracts;
using MSS.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using MapSecWorkGenType = MapSectionProviderLib.WorkItem<MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>, MEngineDataContracts.MapSectionResponse>;

namespace MapSectionProviderLib
{
	public class MapSectionGeneratorProcessor : IDisposable
	{
		//private const int NUMBER_OF_CONSUMERS = 4;
		private const int QUEUE_CAPACITY = 200;

		private readonly IMEngineClient[] _mEngineClients;
		private int _nextMEngineClientPtr;

		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSecWorkGenType> _workQueue;

		private readonly IList<Task> _workQueueProcessors;

		private readonly object _cancelledJobsLock = new();
		private readonly List<int> _cancelledJobIds;

		private bool disposedValue;

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient[] mEngineClients, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_mEngineClients = mEngineClients;
			_nextMEngineClientPtr = 0;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSecWorkGenType>(QUEUE_CAPACITY);
			_cancelledJobIds = new List<int>();

			var numberOfLogicalProc = Environment.ProcessorCount;
			var localTaskCnt = numberOfLogicalProc - 1;
			var remoteTaskCnt = localTaskCnt / 3;

			_workQueueProcessors = new List<Task>();
			foreach (var client in mEngineClients)
			{
				if (client.EndPointAddress.ToLower().Contains("localhost"))
				{
					for (var i = 0; i < localTaskCnt; i++)
					{
						_workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(client, _mapSectionPersistProcessor, _cts.Token)));
					}
				}
				else
				{
					for (var i = 0; i < remoteTaskCnt; i++)
					{
						_workQueueProcessors.Add(Task.Run(async () => await ProcessTheQueueAsync(client, _mapSectionPersistProcessor, _cts.Token)));
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void AddWork(MapSecWorkGenType mapSectionWorkItem)
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

		// TODO: Add a timestamp the cancelledJobIds list and then use it to determine when that entry can be deleted.
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
				}
			}
			catch { }

			_mapSectionPersistProcessor?.Stop(immediately);

		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(IMEngineClient mEngineClient, MapSectionPersistProcessor mapSectionPersistProcessor, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkItem = _workQueue.Take(ct);

					// The original request is in the Request's Request property.
					var mapSectionRequest = mapSectionWorkItem.Request.Request;

					MapSectionResponse mapSectionResponse;

					if (IsJobCancelled(mapSectionWorkItem.JobId))
					{
						mapSectionResponse = null;
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						//mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(mapSectionRequest);
						mapSectionResponse = await mEngineClient.GenerateMapSectionAsyncR(mapSectionRequest);

						mapSectionResponse.MapSectionId = mapSectionWorkItem.Request.Request.MapSectionId;

						if (mapSectionPersistProcessor != null)
						{
							mapSectionPersistProcessor.AddWork(mapSectionResponse);
						}
					}

					mapSectionWorkItem.RunWorkAction(mapSectionResponse);
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The response queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The response queue got an exception. The current client has address: {mEngineClient?.EndPointAddress ?? "No Current Client" }. The exception is {e}.");
					throw;
				}
			}
		}

		private IMEngineClient GetNextClient()
		{
			var result = _mEngineClients[_nextMEngineClientPtr++];

			if (_nextMEngineClientPtr > _mEngineClients.Length - 1)
			{
				_nextMEngineClientPtr = 0;
			}

			return result;
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

					// Dispose managed state (managed objects)
					if (_cts != null)
					{
						_cts.Dispose();
					}

					if (_workQueue != null)
					{
						_workQueue.Dispose();
					}

					for (var i = 0; i < _workQueueProcessors.Count; i++)
					{
						if (_workQueueProcessors[i] != null)
						{
							_workQueueProcessors[i].Dispose();
						}
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

		#endregion
	}
}
