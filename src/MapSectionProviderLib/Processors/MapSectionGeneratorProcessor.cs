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

		private bool _useDetailedDebug = false;

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
				var tsk = Task.Run(() => ProcessTheQueue(client, _cts.Token));
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

		private void ProcessTheQueue(IMEngineClient mEngineClient, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionGenerateRequests = _workQueue.Take(ct);

					if (ct.IsCancellationRequested) break;

					MapSectionGenerateRequest? mapSectionGenerateRequest;

					if (mapSectionGenerateRequests.Count > 1)
					{
						Debug.WriteLine($"MapSectionGeneratorProcessor's WorkQueue is returning {mapSectionGenerateRequests.Count} items for Job: {mapSectionGenerateRequests[0].JobNumber}.");

						mapSectionGenerateRequest = GetGenerateRequest(mapSectionGenerateRequests);
					}
					else
					{
						mapSectionGenerateRequest = mapSectionGenerateRequests[0];
					}

					if (mapSectionGenerateRequest != null)
					{
						if (mapSectionGenerateRequest.JobIsCancelled)
						{
							if (mapSectionGenerateRequest.Response != null)
							{
								_mapSectionVectorProvider.ReturnToPool(mapSectionGenerateRequest.Response);
							}
						}
						else
						{
							var mapSectionWorkRequest = mapSectionGenerateRequest.Request;

							var mapSectionResponse = HandleMapSectionRequest(mapSectionWorkRequest.Request, mEngineClient);
							mapSectionGenerateRequest.RunWorkAction(mapSectionResponse);
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

			Debug.WriteLine($"Skipped {requestPtr} requests for Job: {jobNumber}.");

			var result = requestPtr < mapSectionGenerateRequests.Count ? mapSectionGenerateRequests[requestPtr] : null;

			return result;
		}

		private MapSectionResponse HandleMapSectionRequest(MapSectionRequest mapSectionRequest, IMEngineClient mEngineClient)
		{
			MapSectionResponse mapSectionResponse;
			ReportProcesssARequest(mapSectionRequest, jobIsCancelled:false);

			if (mapSectionRequest.NeitherRequestNorMirrorIsInPlay)
			{
				mapSectionResponse = MapSectionResponse.CreateCancelledResponseWithVectors(mapSectionRequest);
				mapSectionRequest.MsrJob.SectionsCancelled++;
			}
			else
			{
				mapSectionResponse = mEngineClient.GenerateMapSection(mapSectionRequest, mapSectionRequest.CancellationTokenSource.Token);
				if (!mapSectionRequest.NeitherRequestNorMirrorIsInPlay)
				{
					if (!mapSectionRequest.Cancelled)
					{
						mapSectionRequest.MsrJob.SectionsGenerated++;
					}

					if (mapSectionRequest.Mirror != null && !mapSectionRequest.Mirror.Cancelled)
					{
						mapSectionRequest.Mirror.MsrJob.SectionsGenerated++;
					}
				}

				if (mapSectionResponse.MapSectionVectors2 == null)
				{
					Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
				}

				if (mapSectionRequest.MapSectionId != null)
				{
					Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
				}
			}

			Debug.Assert(mapSectionRequest.MapSectionVectors2 == null, "MapSectionVectors2 should be Null.");
			Debug.Assert(mapSectionRequest.MapSectionZVectors == null, "MapSectionZVectors should be Null.");

			return mapSectionResponse;
		}

		//private MapSectionResponse HandleItemJobIsCancelled(MapSectionRequest mapSectionRequest, IMEngineClient mEngineClient)
		//{
		//	// The original request is in the Request's Request property.

		//	ReportProcesssARequest(mapSectionRequest, jobIsCancelled: true);

		//	var mapSectionResponse = MapSectionResponse.CreateCancelledResponseWithVectors(mapSectionRequest);

		//	Debug.Assert(mapSectionRequest.MapSectionVectors2 == null, "MapSectionVectors2 should be Null.");
		//	Debug.Assert(mapSectionRequest.MapSectionZVectors == null, "MapSectionZVectors should be Null.");

		//	return mapSectionResponse;
		//}

		//// Handle the first item -- it's job has been cancelled
		//var mapSectionResponse = HandleItemJobIsCancelled(mapSectionWorkRequest.Request, mEngineClient);
		//msGenerateRequest.RunWorkAction(mapSectionResponse);

		//var currentJobNumber = mapSectionWorkRequest.Request.MapLoaderJobNumber;
		//var requestPtr = 1;

		//var skipAmount = 0;
		//// Skip over requests from this same cancelled job
		//while (requestPtr < mapSectionGenerateRequests.Count && mapSectionGenerateRequests[requestPtr].Request.Request.MapLoaderJobNumber == currentJobNumber)
		//{
		//	requestPtr++;
		//	skipAmount++;
		//}

		//Debug.WriteLine($"Skipped {skipAmount} requests for Job: {currentJobNumber}.");

		//while (requestPtr < mapSectionGenerateRequests.Count)
		//{
		//	// This request is for a different job than the previous request.
		//	msGenerateRequest = mapSectionGenerateRequests[requestPtr];
		//	mapSectionWorkRequest = msGenerateRequest.Request;

		//	if (mapSectionWorkRequest.Request.MsrJob.IsCancelled)
		//	{
		//		mapSectionResponse = HandleItemJobIsCancelled(mapSectionWorkRequest.Request, mEngineClient);
		//		msGenerateRequest.RunWorkAction(mapSectionResponse);
		//	}
		//	else
		//	{
		//		mapSectionResponse = HandleItemJobNotCancelled(mapSectionWorkRequest.Request, mEngineClient);
		//		msGenerateRequest.RunWorkAction(mapSectionResponse);
		//	}

		//	currentJobNumber = mapSectionWorkRequest.Request.MapLoaderJobNumber;
		//	requestPtr++;

		//	skipAmount = 0;
		//	// Skip over requests from this same cancelled job
		//	while (requestPtr < mapSectionGenerateRequests.Count && mapSectionGenerateRequests[requestPtr].Request.Request.MapLoaderJobNumber == currentJobNumber)
		//	{
		//		requestPtr++;
		//		skipAmount++;
		//	}

		//	Debug.WriteLine($"Skipped {skipAmount} requests for Job: {currentJobNumber}.");
		//}


		[Conditional("DEBUG")]
		private void ReportProcesssARequest(MapSectionRequest mapSectionRequest, bool jobIsCancelled)
		{
			if (_useDetailedDebug)
			{
				if (jobIsCancelled || mapSectionRequest.NeitherRequestNorMirrorIsInPlay)
				{
					var msg = $"The MapSectionGeneratorProcessor is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
					msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
					Debug.WriteLine(msg);
				}
				else
				{
					var sendingVectorsMsg = mapSectionRequest.IncreasingIterations ? "Sending current counts for iteration update." : string.Empty;
					var haveZValuesMsg = mapSectionRequest.MapSectionZVectors != null ? "Sending ZValues." : null;

					Debug.WriteLine($"Generating MapSection for Request: {mapSectionRequest.MapLoaderJobNumber}/{mapSectionRequest.RequestNumber}. " +
						$"BlockPos: {mapSectionRequest.SectionBlockOffset}. {sendingVectorsMsg} {haveZValuesMsg}");

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
