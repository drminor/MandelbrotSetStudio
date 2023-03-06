using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		#region Private Properties

		private const int PRECSION_PADDING = 4;

		private const int NUMBER_OF_CONSUMERS = 2;
		private const int QUEUE_CAPACITY = 10; //200;

		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionHelper _mapSectionHelper;

		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionGeneratorProcessor _mapSectionGeneratorProcessor;
		private readonly MapSectionResponseProcessor _mapSectionResponseProcessor;
		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

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

		#endregion

		#region Constructor

		public MapSectionRequestProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionHelper mapSectionHelper,
			MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			//_isStopped = false;

			UseRepo = true;

			_nextJobId = 0;
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionHelper = mapSectionHelper;
			_dtoMapper = new DtoMapper();

			_mapSectionGeneratorProcessor = mapSectionGeneratorProcessor;
			_mapSectionResponseProcessor = mapSectionResponseProcessor;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

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

		#region Public Properties

		public bool UseRepo { get; set; }

		#endregion

		#region Public Methods

		public void AddWork(int jobNumber, MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSection?, int> responseHandler)
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

		public void MarkJobAsComplete(int jobId)
		{
			lock (_cancelledJobsLock)
			{
				if (_cancelledJobIds.Contains(jobId))
				{
					_cancelledJobIds.Remove(jobId);
				}

				_mapSectionGeneratorProcessor.MarkJobAsComplete(jobId);
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
			lock (_cancelledJobsLock)
			{
				var nextJobId = _nextJobId++;
			}

			return _nextJobId;
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _workQueue.Take(ct);

					if (IsJobCancelled(mapSectionWorkRequest.JobId))
					{
						mapSectionWorkRequest.Response = new MapSection(mapSectionWorkRequest.Request, isCancelled: true);
						_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);
					}
					else
					{
						if (!UseRepo)
						{
							await Task.Delay(20);
							var mapSectionVectors = _mapSectionHelper.ObtainMapSectionVectors();
							PrepareRequestAndQueue(mapSectionWorkRequest, mapSectionGeneratorProcessor, mapSectionVectors);
						}
						else
						{
							var mapSectionResponse = await FetchOrQueueForGenerationAsync(mapSectionWorkRequest, mapSectionGeneratorProcessor, ct);

							if (mapSectionResponse != null)
							{
								var mapSection = CreateMapSection(mapSectionWorkRequest.Request, mapSectionResponse.MapSectionVectors, mapSectionWorkRequest.JobId);

								mapSectionWorkRequest.Response = mapSection;
								_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);
							}
						}
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

		private async Task<MapSectionResponse?> FetchOrQueueForGenerationAsync(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;

			var mapSectionVectors = _mapSectionHelper.ObtainMapSectionVectors();

			var mapSectionResponse = await FetchAsync(mapSectionWorkRequest, ct, mapSectionVectors);

			if (mapSectionResponse != null)
			{
				var requestedIterations = mapSectionWorkRequest.Request.MapCalcSettings.TargetIterations;

				if (IsResponseComplete(mapSectionResponse, requestedIterations, out var reason))
				{
					//Debug.WriteLine($"Got {request.ScreenPosition} from repo.");

					request.FoundInRepo = true;
					request.ProcessingEndTime = DateTime.UtcNow;

					mapSectionResponse.OwnerId = request.OwnerId;
					mapSectionResponse.JobOwnerType = request.JobOwnerType;
					_ = await _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse);

					return mapSectionResponse;
				}
				else
				{
					Debug.WriteLine($"Requesting the iteration count to be increased for {request.ScreenPosition}. The response was incomplete for reason: {reason}.");

					request.MapSectionVectors = mapSectionResponse.MapSectionVectors;

					var mapSectionId = ObjectId.Parse(mapSectionResponse.MapSectionId);
					var zValues = await FetchTheZValuesAsync(mapSectionId, ct);

					if (zValues != null)
					{
						var mapSectionZVectors = _mapSectionHelper.ObtainMapSectionZVectors(zValues.LimbCount);
						mapSectionZVectors.Load(zValues.Zrs, zValues.Zis, zValues.HasEscapedFlags, zValues.RowsHasEscaped);
						request.MapSectionZVectors = mapSectionZVectors;
					}
					else
					{
						request.MapSectionZVectors = _mapSectionHelper.ObtainMapSectionZVectorsByPrecision(request.Precision + PRECSION_PADDING);
						request.MapSectionZVectors.ResetObject();
					}

					request.MapSectionId = mapSectionId.ToString();
					request.IncreasingIterations = true;

					Debug.WriteLine($"Requesting the iteration count to be increased for {request.ScreenPosition}.");
					QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);

					//Debug.WriteLine($"Response found, but is incomplete. Creating brand new request -- not updating for {request.ScreenPosition}.");
					//PrepareRequestAndQueue(mapSectionWorkRequest, mapSectionGeneratorProcessor, mapSectionVectors);
					return null;
				}
			}
			else
			{
				PrepareRequestAndQueue(mapSectionWorkRequest, mapSectionGeneratorProcessor, mapSectionVectors);
				return null;
			}
		}

		private void PrepareRequestAndQueue(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionVectors mapSectionVectors)
		{
			var request = mapSectionWorkRequest.Request;

			request.MapSectionId = null;

			request.MapSectionVectors = mapSectionVectors;
			request.MapSectionVectors.ResetObject();
			request.MapSectionZVectors = _mapSectionHelper.ObtainMapSectionZVectorsByPrecision(request.Precision + PRECSION_PADDING);
			request.MapSectionZVectors.ResetObject();

			if (request.ScreenPosition.IsZero())
			{
				Debug.WriteLine($"Requesting {request.ScreenPosition} to be generated. LimbCount = {request.MapSectionZVectors.LimbCount}.");
			}

			QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
		}

		private bool IsResponseComplete(MapSectionResponse mapSectionResponse, int requestedIterations, [MaybeNullWhen(true)] out string reason)
		{
			if (mapSectionResponse.MapSectionVectors == null)
			{
				//reason = "MapSectionVectors is null.";
				//return false;

				throw new InvalidOperationException("A MapSectionRecords was found in the repo with a null MapSectionVectors.");
			}

			// TODO: Update the mapSectionResponse to include details about which rows are complete. This is required for those cases where the Generator was given a CancellationToken that got cancelled.

			var fetchedTargetIterations = mapSectionResponse.MapCalcSettings?.TargetIterations ?? 0;

			if (fetchedTargetIterations >= requestedIterations)
			{
				//The MapSection fetched from the repository is the result of a request to generate at or above the current request's target iterations.
				reason = null;
				return true;
			}

			if (mapSectionResponse.AllRowsHaveEscaped)
			{
				reason = null;
				return true;
			}

			reason = $"IterationCountOnFile: {fetchedTargetIterations} is < requested {requestedIterations} and AllRowsHaveEscaped = false.";
			return false;
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

		private async Task<MapSectionResponse?> FetchAsync(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct, MapSectionVectors mapSectionVectors)
		{
			var mapSectionRequest = mapSectionWorkRequest.Request;
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var blockPosition = _dtoMapper.MapTo(mapSectionRequest.BlockPosition);

			var mapSectionResponse = await _mapSectionAdapter.GetMapSectionAsync(subdivisionId, blockPosition, ct, mapSectionVectors);

			return mapSectionResponse;
		}

		private async Task<ZValues?> FetchTheZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionAdapter.GetMapSectionZValuesAsync(mapSectionId, ct);

			return result;
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse? mapSectionResponse, int jobId)
		{
			var workList = new List<MapSectionWorkRequest>();

			MapSection mapSection;

			if (mapSectionResponse != null)
			{
				mapSection = BuildMapSection(mapSectionWorkRequest.Request, mapSectionResponse, jobId);
				if (!mapSectionResponse.RequestCancelled)
				{
					PersistResponse(mapSectionWorkRequest.Request, mapSectionResponse);
				}
			}
			else
			{
				Debug.WriteLine("The MapSectionResponse is null in the HandleGeneratedResponse callback for the MapSectionRequestProcessor.");
				mapSection = new MapSection(mapSectionWorkRequest.Request, isCancelled: false);
			}

			_requestsLock.EnterUpgradeableReadLock();

			try
			{
				mapSectionWorkRequest.Response = mapSection;
				workList.Add(mapSectionWorkRequest);

				var pendingRequests = GetPendingRequests(mapSectionWorkRequest.Request);
				//Debug.WriteLine($"Handling generated response, the count is {pendingRequests.Count} for request: {mapSectionWorkRequest.Request}");

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
						MapSection newMapSectionCopy;
						if (mapSectionResponse != null)
						{
							newMapSectionCopy = BuildMapSection(mapSectionWorkRequest.Request, mapSectionResponse, jobId);
							if (newMapSectionCopy.MapSectionVectors != null)
							{
								Debug.WriteLine($"Creating a copy of the MapSectionVectors for {mapSectionWorkRequest.Request.ScreenPosition}.");
								newMapSectionCopy.MapSectionVectors = _mapSectionHelper.Duplicate(newMapSectionCopy.MapSectionVectors);
							}
						}
						else
						{
							newMapSectionCopy = new MapSection(mapSectionWorkRequest.Request, isCancelled: false);
						}

						workItem.Response = newMapSectionCopy;
						workList.Add(workItem);
					}
				}
			}
			finally
			{
				_requestsLock.ExitUpgradeableReadLock();
			}

			foreach (var workItem in workList)
			{
				_mapSectionResponseProcessor.AddWork(workItem);
			}
		}

		private MapSection BuildMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, int jobId)
		{
			MapSection mapSection;

			if (mapSectionResponse.RequestCancelled)
			{
				mapSection = new MapSection(mapSectionRequest, isCancelled: true);
			}
			else
			{
				mapSection = CreateMapSection(mapSectionRequest, mapSectionResponse.MapSectionVectors, jobId);
			}

			return mapSection;
		}

		private void PersistResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			if (!mapSectionResponse.RecordOnFile || mapSectionRequest.IncreasingIterations)
			{
				if (mapSectionResponse.MapSectionVectors == null)
				{
					throw new InvalidOperationException("The MapSectionVectors is null.");
				}

				mapSectionResponse.MapSectionVectors = _mapSectionHelper.Duplicate(mapSectionResponse.MapSectionVectors);


				if (!mapSectionResponse.RequestCompleted && !mapSectionResponse.RecordOnFile)
				{
					mapSectionResponse.MapCalcSettings.TargetIterations = 0;
				}

				_mapSectionPersistProcessor.AddWork(mapSectionResponse);
			}
			else
			{
				Debug.WriteLine("The MapSectionRequestProcessor is handling a response that is already OnFile and it's IncreasingIterations is false.");
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

		private MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors? mapSectionVectors, int jobId)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors == null)
			{
				Debug.WriteLine($"Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors is empty. The request's block position is {mapSectionRequest.BlockPosition}.");
				mapSectionResult = new MapSection(mapSectionRequest, isCancelled: false);
			}
			else
			{
				var mapBlockOffset = mapSectionRequest.MapBlockOffset;
				mapSectionResult = _mapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionVectors, jobId, mapBlockOffset);
			}

			return mapSectionResult;
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
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
