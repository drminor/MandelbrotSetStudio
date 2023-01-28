using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		private const int NUMBER_OF_CONSUMERS = 2;
		private const int QUEUE_CAPACITY = 10; //200;

		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionGeneratorProcessor _mapSectionGeneratorProcessor;
		private readonly MapSectionResponseProcessor _mapSectionResponseProcessor;
		private readonly bool _fetchZValues;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionWorkRequest> _workQueue;

		private readonly Task[] _workQueueProcessors;

		private readonly object _cancelledJobsLock = new();

		private readonly ReaderWriterLockSlim _requestsLock;

		private readonly List<int> _cancelledJobIds;
		private readonly List<MapSectionWorkRequest> _pendingRequests;

		private int _nextJobId;
		private bool disposedValue;

		//private bool _isStopped;

		#region Constructor

		public MapSectionRequestProcessor(MapSectionVectorsPool mapSectionVectorsPool, IMapSectionAdapter mapSectionAdapter, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor, bool fetchZValues)
		{
			//_isStopped = false;

			_nextJobId = 0;
			_mapSectionAdapter = mapSectionAdapter;
			_dtoMapper = new DtoMapper();

			_mapSectionVectorsPool = mapSectionVectorsPool;
			_mapSectionGeneratorProcessor = mapSectionGeneratorProcessor;
			_mapSectionResponseProcessor = mapSectionResponseProcessor;
			_fetchZValues = fetchZValues;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSectionWorkRequest>(QUEUE_CAPACITY);
			_pendingRequests = new List<MapSectionWorkRequest>();
			_requestsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			_cancelledJobIds = new List<int>();

			_workQueueProcessors = new Task[NUMBER_OF_CONSUMERS];

			for (var i = 0; i < _workQueueProcessors.Length; i++)
			{
				_workQueueProcessors[i] = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionGeneratorProcessor, _cts.Token));
			}
		}

		#endregion

		#region Public Methods

		public void AddWork(int jobNumber, MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSectionResponse?> responseHandler) 
		{
			var mapSectionWorkItem = new MapSectionWorkRequest(jobNumber, mapSectionRequest, responseHandler);

			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionWorkItem.Request}, Adding has been completed.");
			}
		}

		public int GetNumberOfPendingRequests(int jobNumber)
		{
			var result = DoWithReadLock(() => { return _pendingRequests.Count(x => x.JobId == jobNumber); });
			return result;
		}

		//public IList<MapSectionRequest> GetPendingRequests(int jobNumber)
		//{
		//	var result = DoWithReadLock(() => { return new List<MapSectionRequest>(_pendingRequests.Where(x => x.JobId == jobNumber).Select(x => x.Request)); } );
		//	return result;
		//}

		public void CancelJob(int jobId)
		{
			lock (_cancelledJobsLock)
			{
				if (!_cancelledJobIds.Contains(jobId))
				{
					_cancelledJobIds.Add(jobId);
				}

				_mapSectionGeneratorProcessor.CancelJob(jobId);
			}
		}

		public void Stop(bool immediately)
		{
			lock (_cancelledJobsLock)
			{
				//if (_isStopped)
				//{
				//	return;
				//}

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

				//_isStopped = true;
			}

			try
			{
				for (var i = 0; i < _workQueueProcessors.Length; i++)
				{
					_ = _workQueueProcessors[i].Wait(120 * 1000);
					Debug.WriteLine($"The MapSectionRequestProcesssor's WorkQueueProcessor Task #{i} has completed.");
				}
			}
			catch { }

			_mapSectionGeneratorProcessor?.Stop(immediately);
			_mapSectionResponseProcessor?.Stop(immediately);
		}

		public int GetNextRequestId()
		{
			lock(_cancelledJobsLock)
			{
				var nextJobId = _nextJobId++;
			}

			return _nextJobId;
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _workQueue.Take(ct);

					var mapSectionResponse = IsJobCancelled(mapSectionWorkRequest.JobId)
						? CreateCancelledResponse(mapSectionWorkRequest.Request)
						: await FetchOrQueueForGenerationAsync(mapSectionWorkRequest, mapSectionGeneratorProcessor, ct);

					if (mapSectionResponse != null)
					{
						mapSectionWorkRequest.Response = mapSectionResponse;
						_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);
					}
					else
					{
						//Debug.WriteLine($"FetchOrQueueForGenerationAsync returned null.");
					}
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The work queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The work queue got an exception: {e}.");
					throw;
				}
			}
		}

		private MapSectionResponse CreateCancelledResponse(MapSectionRequest mapSectionRequest)
		{
			var result = new MapSectionResponse(mapSectionRequest)
			{
				RequestCancelled = true
			};

			return result;
		}

		private async Task<MapSectionResponse?> FetchOrQueueForGenerationAsync(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;

			var mapSectionResponse = await FetchAsync(mapSectionWorkRequest, ct);

			if (mapSectionResponse != null)
			{
				var requestedIterations = mapSectionWorkRequest.Request.MapCalcSettings.TargetIterations;

				if (IsResponseComplete(mapSectionResponse, requestedIterations))
				{
					request.FoundInRepo = true;
					request.ProcessingEndTime = DateTime.UtcNow;

					mapSectionResponse.OwnerId = request.OwnerId;
					mapSectionResponse.JobOwnerType = request.JobOwnerType;
					_ = await _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse);

					return mapSectionResponse;
				}
				else
				{
					// Update the request with the values (in progress) retrieved from the repository.
					request.MapSectionId = mapSectionResponse.MapSectionId;
					request.IncreasingIterations = true;

					// TODO: Implement the 'Update the request with values (in progress) retrieved from the repositry.
					//request.Counts = mapSectionResponse.Counts;
					//request.EscapeVelocities = mapSectionResponse.EscapeVelocities;
					//request.HasEscapedFlags = mapSectionResponse.HasEscapedFlags;
					//request.ZValues = null;

					QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
					return null;
				}
			}
			else
			{
				request.MapSectionId = null;

				var mapSectionVectors = _mapSectionVectorsPool.Obtain();
				request.MapSectionVectors = mapSectionVectors;

				QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
				return null;
			}
		}

		private bool IsResponseComplete(MapSectionResponse mapSectionResponse, int requestedIterations)
		{
			if (mapSectionResponse.MapSectionValues == null)
			{
				return false;
			}

			var fetchedTargetIterations = mapSectionResponse.MapCalcSettings?.TargetIterations ?? 0;

			if (fetchedTargetIterations >= requestedIterations)
			{
				//The MapSection fetched from the repository is the result of a request to generate at or above the current request's target iterations.
				return true;
			}

			// TODO: Implement the IsResponseComplete on the MapSectionRequestProcessor
			//if (mapSectionResponse.HasEscapedFlags.Length == 1)
			//{
			//	// All are either done or not done
			//	var result = mapSectionResponse.HasEscapedFlags[0];
			//	return result;
			//}

			//for (var i = 0; i < mapSectionResponse.Counts.Length; i++)
			//{
			//	if (!mapSectionResponse.HasEscapedFlags[i] && mapSectionResponse.Counts[i] < requestedIterations)
			//	{
			//		return false;
			//	}
			//}

			return false;
			//return true;
		}

		private void QueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor)
		{
			if (mapSectionWorkRequest == null)
			{
				throw new ArgumentNullException(nameof(mapSectionWorkRequest), "The mapSectionWorkRequest must be non-null.");
			}

			_requestsLock.EnterWriteLock();

			try
			{
				if (ThereIsAMatchingRequest(mapSectionWorkRequest.Request))
				{
					// There is already a request made for this same block, add our request to the queue
					mapSectionWorkRequest.Request.Pending = true;
					_pendingRequests.Add(mapSectionWorkRequest);
				}
				else
				{
					// Let other's know about our request.
					_pendingRequests.Add(mapSectionWorkRequest);

					var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, HandleGeneratedResponse);
					mapSectionGeneratorProcessor.AddWork(mapSectionGenerateRequest);
				}
			}
			finally
			{
				_requestsLock.ExitWriteLock();
			}
		}

		private async Task<MapSectionResponse?> FetchAsync(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
		{
			//var mapSectionRequest = mapSectionWorkRequest.Request;
			//var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			//var blockPosition = mapSectionRequest.BlockPosition;
			//var mapSectionResponse = await _mapSectionAdapter.GetMapSectionAsync(subdivisionId, blockPosition, _fetchZValues, ct);

			//return mapSectionResponse;

			if (ct.IsCancellationRequested)
			{
				await Task.Delay(100);
			}

			return null;
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse? mapSectionResponse)
		{
			if (mapSectionResponse?.MapSectionVectors == null)
			{
				Debug.WriteLine("The MapSectionResponse has no MapSectionVectors in the HandleGeneratedResponse callback for the MapSectionRequestProcessor.");
			}

			var mapSectionRequest = mapSectionWorkRequest.Request;

			// TODO: Now that we creating a separate copy of the response for each request,
			// Consider updating each request with the contents of the response and then disposing the response
			// instead of keeping the request and response. 

			// Set the original request's repsonse to the generated response. If the response is null create a new response using the request data.
			mapSectionWorkRequest.Response = mapSectionResponse ?? new MapSectionResponse(mapSectionRequest);

			//// Send the original request to the response processor.
			//_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);

			// Start a list of new work items
			var workList = new List<MapSectionWorkRequest> { mapSectionWorkRequest };

			_requestsLock.EnterUpgradeableReadLock();

			try
			{
				var pendingRequests = GetPendingRequests(mapSectionRequest);
				//Debug.WriteLine($"Handling generated response, the count is {pendingRequests.Count} for request: {mapSectionRequest}");

				if (pendingRequests.Count > 0)
				{
					_requestsLock.EnterWriteLock();

					try
					{
						RemoveFoundRequests(pendingRequests);
					}
					finally
					{
						_requestsLock.ExitWriteLock();
					}
				}

				// For diagnostics only
				if (!RequestExists(mapSectionWorkRequest.Request, pendingRequests))
				{
					Debug.WriteLine("WARNING: The primary request was not included in the list of pending requests.");
				}

				foreach (var workItem in pendingRequests)
				{
					if (workItem != mapSectionWorkRequest)
					{
						workItem.Response = new MapSectionResponse(workItem.Request);

						if (mapSectionResponse?.MapSectionVectors != null)
						{
							var newCopyOfMapSectionVectors = _mapSectionVectorsPool.DuplicateFrom(mapSectionResponse.MapSectionVectors);
							workItem.Response.MapSectionVectors = newCopyOfMapSectionVectors;

						}

						workList.Add(workItem);
					}
				}
			}
			finally
			{
				_requestsLock.ExitUpgradeableReadLock();
			}

			foreach(var workItem in workList)
			{
				_mapSectionResponseProcessor.AddWork(workItem);
			}
		}

		// Returns true, if there is a "Primary" Request already in the queue
		private bool ThereIsAMatchingRequest(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			//var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			//var result = _pendingRequests.Any(x => (!x.Request.Pending) && x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition);
			var result = _pendingRequests.Any(x => (!x.Request.Pending) && x.Request.SubdivisionId == subdivisionId && x.Request.BlockPosition == mapSectionRequest.BlockPosition);

			return result;
		}

		// Find all matching requests.
		private List<MapSectionWorkRequest> GetPendingRequests(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			//var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);

			//var result = _pendingRequests.Where(x => x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition).ToList();
			var result = _pendingRequests.Where(x => x.Request.SubdivisionId == subdivisionId && x.Request.BlockPosition == mapSectionRequest.BlockPosition).ToList();

			return result;
		}

		private bool RemoveFoundRequests(IList<MapSectionWorkRequest> requests)
		{
			var result = true;

			foreach (var workItem in requests)
			{
				if (!_pendingRequests.Remove(workItem))
				{
					result = false;
				}
			}

			return result;
		}

		private bool RequestExists(MapSectionRequest mapSectionRequest, IEnumerable<MapSectionWorkRequest> workRequests)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			//var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);

			//var result = workRequests.Any(x => x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition);
			var result = workRequests.Any(x => x.Request.SubdivisionId == subdivisionId && x.Request.BlockPosition == mapSectionRequest.BlockPosition);

			return result;
		}

		private bool IsJobCancelled(int jobId)
		{
			bool result;
			lock(_cancelledJobsLock)
			{
				result = _cancelledJobIds.Contains(jobId);
			}

			return result;
		}

		#endregion

		#region Lock Helpers

		private T DoWithReadLock<T>(Func<T> function)
		{
			_requestsLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_requestsLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_requestsLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_requestsLock.ExitWriteLock();
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

					//for(var i = 0; i < _workQueueProcessors.Length; i++)
					//{
					//	if (_workQueueProcessors[i] != null)
					//	{
					//		_workQueueProcessors[i].Dispose();
					//	}
					//}

					if (_mapSectionGeneratorProcessor != null)
					{
						_mapSectionGeneratorProcessor.Dispose();
					}

					if (_mapSectionResponseProcessor != null)
					{
						_mapSectionResponseProcessor.Dispose();
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
