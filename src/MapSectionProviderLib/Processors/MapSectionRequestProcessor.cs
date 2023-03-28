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

		// TODO: Add a property to the MapSectionRequest to specify whether or not to Save the ZValues.

		private readonly bool SAVE_THE_ZVALUES;

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
			SAVE_THE_ZVALUES = false;

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

		public void AddWork(int jobNumber, MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSection, int> responseHandler)
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
						mapSectionWorkRequest.Response = _mapSectionHelper.CreateEmptyMapSection(mapSectionWorkRequest.Request, mapSectionWorkRequest.JobId, isCancelled: true);
						_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);
					}
					else
					{
						if (!UseRepo)
						{
							//await Task.Delay(20);
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

					// TODO: Save the previous Request / Response pair
					// in case the next item on the queue is requesting the same block, just inverted.

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

			var mapSectionResponse = await FetchAsync(request, ct, mapSectionVectors);

			if (mapSectionResponse != null)
			{
				var requestedIterations = request.MapCalcSettings.TargetIterations;

				if (IsResponseComplete(mapSectionResponse, requestedIterations, out var reason))
				{
					//Debug.WriteLine($"Got {request.ScreenPosition} from repo.");

					request.FoundInRepo = true;
					request.ProcessingEndTime = DateTime.UtcNow;

					mapSectionResponse.OwnerId = request.OwnerId;
					mapSectionResponse.JobOwnerType = request.JobOwnerType;

					PersistJobMapSectionRecord(request, mapSectionResponse);

					return mapSectionResponse;
				}
				else
				{
					//Debug.WriteLine($"Requesting the iteration count to be increased for {request.ScreenPosition}. The response was incomplete for reason: {reason}.");
					request.IncreasingIterations = true;
					request.MapSectionVectors = mapSectionResponse.MapSectionVectors;

					if (UseRepo && SAVE_THE_ZVALUES)
					{
						var mapSectionId = ObjectId.Parse(mapSectionResponse.MapSectionId);
						var mapSectionZVectors = _mapSectionHelper.ObtainMapSectionZVectors(request.LimbCount);
						request.MapSectionZVectors = mapSectionZVectors;

						var zValues = await FetchTheZValuesAsync(mapSectionId, ct);
						if (zValues != null)
						{
							mapSectionZVectors.Load(zValues.Zrs, zValues.Zis, zValues.HasEscapedFlags, zValues.RowsHasEscaped);
						}
						else
						{
							request.MapSectionZVectors.ResetObject();
						}

						request.MapSectionId = mapSectionId.ToString();
					}
					else
					{
						request.MapSectionId = mapSectionResponse.MapSectionId;
					}

					//Debug.WriteLine($"Requesting the iteration count to be increased for {request.ScreenPosition}.");
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
			//request.IncreasingIterations = false;
			request.MapSectionVectors = mapSectionVectors;
			request.MapSectionVectors.ResetObject();

			if (UseRepo && SAVE_THE_ZVALUES)
			{
				request.MapSectionZVectors = _mapSectionHelper.ObtainMapSectionZVectors(request.LimbCount);
				request.MapSectionZVectors.ResetObject();
			}

			if (request.ScreenPosition.IsZero())
			{
				var note = request.MapSectionZVectors == null ? "Not Saving the ZValues." : "LimbCount = { request.MapSectionZVectors.LimbCount}.";
				Debug.WriteLine($"Requesting {request.ScreenPosition} to be generated. {note}.");
			}

			QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
		}

		private bool IsResponseComplete(MapSectionResponse mapSectionResponse, int requestedIterations, [MaybeNullWhen(true)] out string reason)
		{
			if (mapSectionResponse.MapSectionVectors == null)
			{
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

		private async Task<MapSectionResponse?> FetchAsync(MapSectionRequest mapSectionRequest, CancellationToken ct, MapSectionVectors mapSectionVectors)
		{
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

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse, int jobId)
		{
			Debug.Assert(jobId == mapSectionWorkRequest.JobId, "The jobId given to the HandleGeneratedResponse is not the same as the JobId of the MapSectionWorkRequest.");

			_requestsLock.EnterUpgradeableReadLock();

			if (UseRepo)
			{
				PersistResponse(mapSectionWorkRequest.Request, mapSectionResponse);
			}

			try
			{
				mapSectionWorkRequest.Response = BuildMapSection(mapSectionWorkRequest.Request, mapSectionResponse, jobId);
				_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest);

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

				WasPrimaryRequestFound(mapSectionWorkRequest, pendingRequests);

				foreach (var workItem in pendingRequests)
				{
					if (workItem != mapSectionWorkRequest)
					{
						workItem.Response = BuildMapSection(workItem.Request, mapSectionResponse, jobId);
						_mapSectionResponseProcessor.AddWork(workItem);
					}
				}
			}
			finally
			{
				if (mapSectionResponse != null)
				{
					_mapSectionHelper.ReturnMapSectionResponse(mapSectionResponse);
				}

				_requestsLock.ExitUpgradeableReadLock();
			}
		}

		private MapSection BuildMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, int jobId)
		{
			MapSection mapSection;

			if (mapSectionResponse.RequestCancelled)
			{
				mapSection = _mapSectionHelper.CreateEmptyMapSection(mapSectionRequest, jobId, isCancelled: true);
			}
			else
			{
				mapSection = CreateMapSection(mapSectionRequest, mapSectionResponse.MapSectionVectors, jobId);
				mapSectionResponse.MapSectionVectors?.IncreaseRefCount();
			}

			return mapSection;
		}

		private MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors? mapSectionVectors, int jobId)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors == null)
			{
				Debug.WriteLine($"WARNING: Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors is empty. The request's block position is {mapSectionRequest.BlockPosition}.");
				mapSectionResult = _mapSectionHelper.CreateEmptyMapSection(mapSectionRequest, jobId, isCancelled: false);
			}
			else
			{
				mapSectionResult = _mapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionVectors, jobId);
			}

			return mapSectionResult;
		}

		private void PersistJobMapSectionRecord(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var copyWithNoVectors = mapSectionResponse.CreateCopySansVectors();
			_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, copyWithNoVectors, onlyInsertJobMapSectionRecord: true));
		}

		private void PersistResponse(MapSectionRequest mapSectionRequest, MapSectionResponse? mapSectionResponse)
		{
			if (mapSectionResponse != null)
			{
				mapSectionResponse.MapSectionVectors?.IncreaseRefCount();
				_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, mapSectionResponse));
			}
		}

		// Returns true, if there is a "Primary" Request already in the queue
		private bool ThereIsAMatchingRequest(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var result = _pendingRequests.Any(x => (!x.Request.Pending) && x.Request.SubdivisionId == subdivisionId && x.Request.BlockPosition == mapSectionRequest.BlockPosition);

			return result;
		}

		// Find all matching requests.
		private List<MapSectionWorkRequest> GetPendingRequests(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
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

		private bool IsJobCancelled(int jobId)
		{
			bool result;
			lock(_cancelledJobsLock)
			{
				result = _cancelledJobIds.Contains(jobId);
			}

			return result;
		}

		//[Conditional("DEBUG")]
		//private void IsMapSectionResponseNull(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse? mapSectionResponse)
		//{
		//	if (mapSectionResponse == null)
		//	{
		//		Debug.WriteLine($"WARNING: The MapSectionResponse is null in the HandleGeneratedResponse callback for the MapSectionRequestProcessor. The request's block position is {mapSectionWorkRequest.Request.BlockPosition}.");
		//	}
		//}

		[Conditional("DEBUG")]
		private void WasPrimaryRequestFound(MapSectionWorkRequest mapSectionWorkRequest, IList<MapSectionWorkRequest> pendingRequests)
		{
			if (!RequestExists(mapSectionWorkRequest.Request, pendingRequests))
			{
				Debug.WriteLine("WARNING: The primary request was not included in the list of pending requests.");
			}
		}

		private bool RequestExists(MapSectionRequest mapSectionRequest, IEnumerable<MapSectionWorkRequest> workRequests)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var result = workRequests.Any(x => x.Request.SubdivisionId == subdivisionId && x.Request.BlockPosition == mapSectionRequest.BlockPosition);

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
