using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionGeneratorProcessor : IDisposable
	{
		#region Private Properties

		private const int QUEUE_CAPACITY = 500;

		//private readonly IMEngineClient[] _mEngineClients;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionGenerateRequest> _workQueue;

		private readonly IList<Task> _workQueueProcessors;

		private readonly object _jobsStatusLock = new();
		private readonly Dictionary<int, CancellationTokenSource> _jobs;

		private bool disposedValue;

		private bool _stopped;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient[] mEngineClients)
		{
			_stopped = false;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSectionGenerateRequest>(QUEUE_CAPACITY);
			_jobs = new Dictionary<int, CancellationTokenSource>();

			_workQueueProcessors = CreateTheQueueProcessors(mEngineClients);
			//_mEngineClients = mEngineClients;
		}

		private IList<Task> CreateTheQueueProcessors(IMEngineClient[] clients)
		{
			var workQueueProcessors = new List<Task>();

			foreach(var client in clients)
			{
				workQueueProcessors.Add(Task.Run(() => ProcessTheQueue(client, _cts.Token)));
			}

			return workQueueProcessors;
		}

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

		//private async Task ProcessTheQueueAsync(IMEngineClient mEngineClient, CancellationToken ct)
		//{
		//	while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
		//	{
		//		try
		//		{
		//			var mapSectionGenerateRequest = _workQueue.Take(ct);

		//			// The original request is in the Request's Request property.
		//			var mapSectionRequest = mapSectionGenerateRequest.Request.Request;
		//			var cts = mapSectionRequest.CancellationTokenSource;

		//			MapSectionResponse mapSectionResponse;

		//			if (IsJobCancelled(mapSectionGenerateRequest.JobId, cts))
		//			{
		//				mapSectionResponse = new MapSectionResponse(mapSectionRequest, isCancelled: true);
		//				var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
		//				mapSectionResponse.MapSectionVectors = msv;
		//				mapSectionResponse.MapSectionZVectors = mszv;

		//			}
		//			else
		//			{
		//				//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
		//				mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
		//				mapSectionResponse = await mEngineClient.GenerateMapSectionAsync(mapSectionRequest, mapSectionRequest.CancellationTokenSource.Token);
		//				//mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

		//				if (!cts.Token.IsCancellationRequested && mapSectionResponse.MapSectionVectors == null)
		//				{
		//					Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
		//				}

		//				if (mapSectionRequest.MapSectionId != null)
		//				{
		//					Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
		//				}
		//			}

		//			mapSectionGenerateRequest.RunWorkAction(mapSectionResponse);
		//		}
		//		catch (OperationCanceledException)
		//		{
		//			//Debug.WriteLine("The response queue got a OCE.");
		//		}
		//		catch (Exception e)
		//		{
		//			Debug.WriteLine($"ERROR: The response queue got an exception. The current client has address: {mEngineClient?.EndPointAddress ?? "No Current Client" }. The exception is {e}.");
		//			throw;
		//		}
		//	}
		//}

		private void ProcessTheQueue(IMEngineClient mEngineClient, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionGenerateRequest = _workQueue.Take(ct);

					// The original request is in the Request's Request property.
					var mapSectionRequest = mapSectionGenerateRequest.Request.Request;
					var cts = mapSectionRequest.CancellationTokenSource;

					MapSectionResponse mapSectionResponse;
					var jobIsCancelled = IsJobCancelled(mapSectionGenerateRequest.JobId);

					if (jobIsCancelled || cts.IsCancellationRequested)
					{
						mapSectionResponse = new MapSectionResponse(mapSectionRequest, isCancelled: true);
						var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
						mapSectionResponse.MapSectionVectors = msv;
						mapSectionResponse.MapSectionZVectors = mszv;

						var msg = $"The MapSectionGeneratorProcessor is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
						Debug.WriteLineIf(_useDetailedDebug, msg);
					}
					else
					{
						var sendingVectorsMsg = mapSectionRequest.IncreasingIterations ? "Sending current counts for iteration update." : string.Empty;
						var haveZValuesMsg = mapSectionRequest.MapSectionZVectors != null ? "Sending ZValues" : null;

						Debug.WriteLine($"Generating MapSection for block: {mapSectionRequest.BlockPosition} {sendingVectorsMsg} {haveZValuesMsg}.");
						mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
						mapSectionResponse = mEngineClient.GenerateMapSection(mapSectionRequest, mapSectionRequest.CancellationTokenSource.Token);
						//mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

						// Now that the mEngineClient has completed the task, once again check to see if the Job is cancelled.
						jobIsCancelled = IsJobCancelled(mapSectionGenerateRequest.JobId);
						//Debug.Assert(!jobIsCancelled, "How is it possible that the job is now cancelled.");

						if (jobIsCancelled)
						{
							Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionGeneratorProcessor. Job {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber} is found to be cancelled after call to Generate MapSection.");
							var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
							mapSectionResponse.MapSectionVectors = msv;
							mapSectionResponse.MapSectionZVectors = mszv;
						}

						if (!jobIsCancelled && !cts.Token.IsCancellationRequested && mapSectionResponse.MapSectionVectors == null)
						{
							Debug.WriteLine($"WARNING: The MapSectionGenerator Processor received an empty MapSectionResponse.");
						}

						if (mapSectionRequest.MapSectionId != null)
						{
							Debug.Assert(mapSectionResponse.MapSectionId == mapSectionRequest.MapSectionId, "The MapSectionResponse has an ID different from the request.");
						}
					}

					Debug.Assert(mapSectionRequest.MapSectionVectors == null, "MapSectionVectors is not Null.");
					Debug.Assert(mapSectionRequest.MapSectionZVectors == null, "MapSectionZVectors is not Null.");

					mapSectionGenerateRequest.RunWorkAction(mapSectionResponse);
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
			lock (_jobsStatusLock)
			{
				if (_jobs.ContainsKey(jobId))
				{
					var cts = _jobs[jobId];
					result = cts.IsCancellationRequested;
				}
				else
				{
					var cts = new CancellationTokenSource();
					_jobs.Add(jobId, cts);
					result = false;
				}
			}

			if (result)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The Job: {jobId} has been cancelled.");
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
