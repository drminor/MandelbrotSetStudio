using MapSectionProviderLib.Support;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionGeneratorProcessor : IDisposable
	{
		#region Private Properties

		private const int QUEUE_CAPACITY = 250;

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private readonly CancellationTokenSource _cts;
		private readonly JobsProducerConsumerQueue<MapSectionGenerateRequest> _workQueue;
		private readonly IList<Task> _workQueueProcessors;

		private bool _stopped;
		private readonly object _jobsStatusLock = new();
		private bool disposedValue;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient[] mEngineClients, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_stopped = false;

			_mapSectionVectorProvider = mapSectionVectorProvider;
			_cts = new CancellationTokenSource();
			_workQueue = new JobsProducerConsumerQueue<MapSectionGenerateRequest>(new JobQueues<MapSectionGenerateRequest>(), QUEUE_CAPACITY);

			_workQueueProcessors = CreateTheQueueProcessors(mEngineClients);
		}

		private IList<Task> CreateTheQueueProcessors(IMEngineClient[] clients)
		{
			var workQueueProcessors = new List<Task>();

			foreach(var client in clients)
			{
				var tsk = Task.Run(() => ProcessTheQueue(client));
				workQueueProcessors.Add(tsk);

				//var processQueueTask = Task.Factory.StartNew(ProcessTheQueue, client, _cts.Token, TaskCreationOptions.LongRunning, scheduler: TaskScheduler.Default);
				//workQueueProcessors.Add(processQueueTask);
			}

			return workQueueProcessors;
		}

		#endregion

		#region Public Properties

		public int NumberOfRequestsPending => _workQueue.Count;

		#endregion

		#region Public Methods

		internal void AddWork(MapSectionGenerateRequest mapSectionWorkItem, CancellationToken ct)
		{
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem, ct);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionWorkItem.Request}, The MapSectionGeneratorProcessor's WorkQueue IsAddingComplete has been set.");
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
					if (_workQueueProcessors[i].Wait(RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS * 1000))
					{
						Debug.WriteLine($"The MapSectionGeneratorProcesssor's WorkQueueProcessor Task #{i} has completed.");
					}
					else
					{
						Debug.WriteLine($"The MapSectionGeneratorProcesssor's WorkQueueProcessor Task #{i} did not complete after waiting for {RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS} seconds.");
					}

				}
			}
			catch { }
		}

		#endregion

		#region Private Methods

		private void ProcessTheQueue(IMEngineClient mEngineClient)
		{
			while (!_cts.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionGenerateRequests = _workQueue.Take(_cts.Token);

					if (_cts.IsCancellationRequested) break;

					MapSectionGenerateRequest? mapSectionGenerateRequest;

					if (mapSectionGenerateRequests.Count > 1)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"MapSectionGeneratorProcessor's WorkQueue is returning {mapSectionGenerateRequests.Count} items for Job: {mapSectionGenerateRequests[0].JobNumber}.");

						mapSectionGenerateRequest = GetGenerateRequest(mapSectionGenerateRequests);
					}
					else
					{
						mapSectionGenerateRequest = mapSectionGenerateRequests[0];
					}

					if (mapSectionGenerateRequest != null)
					{
						if (mapSectionGenerateRequest.JobIsCancelled || mapSectionGenerateRequest.Request.Request.NeitherRegularOrInvertedRequestIsInPlay)
						{
							if (mapSectionGenerateRequest.Response != null)
							{
								_mapSectionVectorProvider.ReturnToPool(mapSectionGenerateRequest.Response);
							}
						}
						else
						{
							var mapSectionRequest = mapSectionGenerateRequest.Request.Request;

							ReportProcesssARequest(mapSectionRequest, jobIsCancelled: false);

							var mapSectionResponse = mEngineClient.GenerateMapSection(mapSectionRequest, mapSectionRequest.CancellationTokenSource.Token);

							CheckResponse(mapSectionRequest, mapSectionResponse);
							mapSectionGenerateRequest.RunWorkAction(mapSectionResponse); // The WorkAction is simply calling the MapSectionRequestProcessor's HandleGeneratedResponse method.

							//// Update the [numberOf] SectionsGenerated only after the next handler has handled the request.
							//UpdateTheJobsSectionsGeneratedCount(mapSectionRequest);
						}
					}
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

		private MapSectionGenerateRequest? GetGenerateRequest(List<MapSectionGenerateRequest> mapSectionGenerateRequests)
		{
			if (mapSectionGenerateRequests.Count == 0)
			{
				return null;
			}

			var jobNumber = mapSectionGenerateRequests[0].JobNumber;

			var requestPtr = 0;

			while (requestPtr < mapSectionGenerateRequests.Count && mapSectionGenerateRequests[requestPtr].JobIsCancelled)
			{
				var mapSectionWorkRequest = mapSectionGenerateRequests[requestPtr].Request;
				if (mapSectionWorkRequest.Response != null)
				{
					_mapSectionVectorProvider.ReturnToPool(mapSectionWorkRequest.Response);
				}

				requestPtr++;
			}

			Debug.WriteLineIf(_useDetailedDebug, $"Skipped {requestPtr} requests for Job: {jobNumber}.");

			var result = requestPtr < mapSectionGenerateRequests.Count ? mapSectionGenerateRequests[requestPtr] : null;

			return result;
		}

		//private void UpdateTheJobsSectionsGeneratedCount(MapSectionRequest mapSectionRequest)
		//{
		//	if (mapSectionRequest.RegularPosition != null && !mapSectionRequest.RegularPosition.IsCancelled)
		//	{
		//		mapSectionRequest.MsrJob.IncrementSectionsGenerated();
		//	}

		//	if (mapSectionRequest.InvertedPosition != null && !mapSectionRequest.InvertedPosition.IsCancelled)
		//	{
		//		mapSectionRequest.MsrJob.IncrementSectionsGenerated();
		//	}
		//}

		[Conditional("DEBUG2")]
		private void ReportProcesssARequest(MapSectionRequest mapSectionRequest, bool jobIsCancelled)
		{
			if (_useDetailedDebug)
			{
				if (jobIsCancelled || mapSectionRequest.NeitherRegularOrInvertedRequestIsInPlay)
				{
					var msg = $"The MapSectionGeneratorProcessor is skipping request with RequestId: {mapSectionRequest.RequestId}.";
					msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
					Debug.WriteLine(msg);
				}
				else
				{
					var sendingVectorsMsg = mapSectionRequest.IncreasingIterations ? "Sending current counts for iteration update." : string.Empty;
					var haveZValuesMsg = mapSectionRequest.MapSectionZVectors != null ? "Sending ZValues." : null;

					Debug.WriteLine($"Generating MapSection for RequestId: {mapSectionRequest.RequestId}.. " +
						$"BlockPos: {mapSectionRequest.SectionBlockOffset}. {sendingVectorsMsg} {haveZValuesMsg}");

				}
			}
		}

		[Conditional("DEBUG2")]
		private void CheckResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			if (mapSectionResponse.MapSectionVectors2 == null)
			{
				Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
			}

			if (mapSectionRequest.MapSectionId != null)
			{
				Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
			}

			Debug.Assert(mapSectionRequest.MapSectionVectors2 == null, "MapSectionVectors2 should be Null.");
			Debug.Assert(mapSectionRequest.MapSectionZVectors == null, "MapSectionZVectors should be Null.");
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
