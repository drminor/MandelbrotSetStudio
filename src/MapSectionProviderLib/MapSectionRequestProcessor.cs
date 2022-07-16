using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
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

		#region Constructor

		public MapSectionRequestProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor, bool fetchZValues)
		{
			_nextJobId = 0;
			_mapSectionAdapter = mapSectionAdapter;
			_dtoMapper = new DtoMapper();
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

					MapSectionResponse? mapSectionResponse;
					if (IsJobCancelled(mapSectionWorkRequest.JobId))
					{
						mapSectionResponse = new MapSectionResponse(mapSectionWorkRequest.Request)
						{
							RequestCancelled = true
						};
					}
					else
					{
						mapSectionResponse = await FetchOrQueueForGenerationAsync(mapSectionWorkRequest, mapSectionGeneratorProcessor, ct);
					}

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

		// TODO: Use the CancellationToken in the MapSectionRequestProcesor's FetchOrQueueForGenerationAsync method.
		private async Task<MapSectionResponse?> FetchOrQueueForGenerationAsync(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			if (CheckForMatchingAndAddToPending(mapSectionWorkRequest))
			{
				// Don't have response now, but have added this request to another request that is in process.
				return null;
			}

			var mapSectionResponse = await FetchAsync(mapSectionWorkRequest);

			if (mapSectionResponse == null)
			{
				var request = mapSectionWorkRequest.Request;
				request.MapSectionId = null;
				QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
				return null;
			}

			var requestedIterations = mapSectionWorkRequest.Request.MapCalcSettings.TargetIterations;

			if (IsResponseComplete(mapSectionResponse, requestedIterations))
			{
				mapSectionWorkRequest.Request.FoundInRepo = true;
				mapSectionWorkRequest.Request.ProcessingEndTime = DateTime.UtcNow;

				mapSectionResponse.OwnerId = mapSectionWorkRequest.Request.OwnerId;
				mapSectionResponse.JobOwnerType = mapSectionWorkRequest.Request.JobOwnerType;
				_ = await _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse);

				return mapSectionResponse;
			}
			else
			{
				// Update the request with the values (in progress) retrieved from the repository.
				var request = mapSectionWorkRequest.Request;
				request.MapSectionId = mapSectionResponse.MapSectionId;
				request.IncreasingIterations = true;
				request.Counts = mapSectionResponse.Counts;
				request.EscapeVelocities = mapSectionResponse.EscapeVelocities;
				request.DoneFlags =  mapSectionResponse.DoneFlags;
				request.ZValues = null;

				QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
				return null;
			}
		}

		private bool IsResponseComplete(MapSectionResponse mapSectionResponse, int requestedIterations)
		{
			if (mapSectionResponse.Counts == null)
			{
				return false;
			}

			if (mapSectionResponse.MapCalcSettings.TargetIterations >= requestedIterations)
			{
				//The MapSection fetched from the repository is the result of a request to generate at or above the current request's target iterations.
				return true;
			}

			if (mapSectionResponse.DoneFlags.Length == 1)
			{
				// All are either done or not done
				var result = mapSectionResponse.DoneFlags[0];
				return result;
			}

			for (var i = 0; i < mapSectionResponse.Counts.Length; i++)
			{
				if (!mapSectionResponse.DoneFlags[i] && mapSectionResponse.Counts[i] < requestedIterations)
				{
					return false;
				}
			}

			return true;
		}

		private void QueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor)
		{
			if (mapSectionWorkRequest == null)
			{
				throw new ArgumentNullException(nameof(mapSectionWorkRequest), "The mapSectionWorkRequest must be non-null.");
			}

			DoWithWriteLock(() => { _pendingRequests.Add(mapSectionWorkRequest); });

			var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, HandleGeneratedResponse);
			mapSectionGeneratorProcessor.AddWork(mapSectionGenerateRequest);
		}

		private bool CheckForMatchingAndAddToPending(MapSectionWorkRequest mapSectionWorkRequest)
		{
			_requestsLock.EnterUpgradeableReadLock();

			try
			{
				var pendingRequests = GetMatchingRequests(mapSectionWorkRequest.Request);

				//Debug.WriteLine($"Checking for dups, the count is {pendingRequests.Count} for request: {mapSectionWorkItem.Request}.");

				if (pendingRequests.Count > 0)
				{
					//Debug.WriteLine($"Found a dup request, marking this one as pending.");

					_requestsLock.EnterWriteLock();
					try
					{
						// There is already a request made for this same block, add our request to the queue
						mapSectionWorkRequest.Request.Pending = true;
						_pendingRequests.Add(mapSectionWorkRequest);
					}
					finally
					{
						_requestsLock.ExitWriteLock();
					}

					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_requestsLock.ExitUpgradeableReadLock();
			}
		}

		private async Task<MapSectionResponse?> FetchAsync(MapSectionWorkRequest mapSectionWorkRequest)
		{
			var mapSectionRequest = mapSectionWorkRequest.Request;
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var blockPosition = mapSectionRequest.BlockPosition;
			var mapSectionResponse = await _mapSectionAdapter.GetMapSectionAsync(subdivisionId, blockPosition, _fetchZValues);

			return mapSectionResponse;
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse? mapSectionResponse)
		{
			if (mapSectionResponse == null || mapSectionResponse.IsEmpty)
			{
				Debug.WriteLine("The MapSectionResponse is empty in the HandleGeneratedResponse callback for the MapSectionRequestProcessor.");
			}

			var mapSectionRequest = mapSectionWorkRequest.Request;

			// if the mapSectionResponse is null, create a mapSectionResponse with null counts, null, escape velocities, etc., from the request.
			var response = mapSectionResponse ?? new MapSectionResponse(mapSectionRequest);


			// Set the original request's repsonse to the generated response.
			mapSectionWorkRequest.Response = response;

			// Send the original request to the response processor.
			_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);

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
						workItem.Response = response;
						_mapSectionResponseProcessor.AddWork(workItem);
					}
				}
			}
			finally
			{
				_requestsLock.ExitUpgradeableReadLock();
			}
		}

		// Exclude requests that were added to "piggy back" onto a "real" request.
		private List<MapSectionWorkRequest> GetMatchingRequests(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var result = _pendingRequests.Where(x => (!x.Request.Pending) && x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition).ToList();

			return result;
		}

		// Find all matching requests.
		private List<MapSectionWorkRequest> GetPendingRequests(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);

			var result = _pendingRequests.Where(x => x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition).ToList();

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
			var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);

			var result = workRequests.Any(x => x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition);

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
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
