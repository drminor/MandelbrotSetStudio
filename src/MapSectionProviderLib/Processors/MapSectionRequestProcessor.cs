﻿using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		#region Private Properties

		private const int NUMBER_OF_REQUEST_CONSUMERS = 2;
		private const int REQUEST_QUEUE_CAPACITY = 50;
		private const int RETURN_QUEUE_CAPACITY = 200;

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private readonly MapSectionGeneratorProcessor _mapSectionGeneratorProcessor;
		private readonly MapSectionResponseProcessor _mapSectionResponseProcessor;
		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private readonly CancellationTokenSource _requestQueueCts;
		private readonly BlockingCollection<MapSectionWorkRequest> _requestQueue;
		private readonly Task[] _requestQueueProcessors;

		private readonly object _cancelledJobsLock = new();

		private readonly ReaderWriterLockSlim _requestsLock;

		private readonly List<int> _cancelledJobIds;
		private readonly List<MapSectionWorkRequest> _pendingRequests;

		private int _nextJobId;
		private bool disposedValue;

		private bool _isStopped;

		private readonly CancellationTokenSource _returnQueueCts;
		private readonly BlockingCollection<MapSectionGenerateRequest> _returnQueue;
		private readonly Task _returnQueueProcess;

		private readonly bool _useDetailedDebug;

		#endregion

		#region Constructor

		public MapSectionRequestProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider,
			MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_isStopped = false;
			_useDetailedDebug = false;

			UseRepo = true;

			_nextJobId = 0;
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionBuilder = new MapSectionBuilder();

			_mapSectionGeneratorProcessor = mapSectionGeneratorProcessor;
			_mapSectionResponseProcessor = mapSectionResponseProcessor;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

			_requestQueueCts = new CancellationTokenSource();
			_requestQueue = new BlockingCollection<MapSectionWorkRequest>(REQUEST_QUEUE_CAPACITY);
			_pendingRequests = new List<MapSectionWorkRequest>();
			_requestsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			_cancelledJobIds = new List<int>();

			_requestQueueProcessors = new Task[NUMBER_OF_REQUEST_CONSUMERS];

			for (var i = 0; i < _requestQueueProcessors.Length; i++)
			{
				_requestQueueProcessors[i] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, _requestQueueCts.Token));
			}

			_returnQueueCts = new CancellationTokenSource();
			_returnQueue = new BlockingCollection<MapSectionGenerateRequest>(RETURN_QUEUE_CAPACITY);

			_returnQueueProcess = Task.Run(() => ProcessTheReturnQueue(_returnQueueCts.Token));
		}

		#endregion

		#region Public Properties

		public bool UseRepo { get; set; }

		#endregion

		#region Public Methods

		public List<Tuple<MapSectionRequest, MapSectionResponse>> FetchResponses(List<MapSectionRequest> mapSectionRequests, out int jobNumber)
		{
			var cts = new CancellationTokenSource();
			var ct = cts.Token;

			jobNumber = GetNextRequestId();
			var result = new List<Tuple<MapSectionRequest, MapSectionResponse>>();

			//MapSectionVectors? mapSectionVectors = null;

			foreach (var request in mapSectionRequests)
			{
				//if (mapSectionVectors == null)
				//{
				//	mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
				//}

				var mapSectionBytes = Fetch(request);

				if (mapSectionBytes != null)
				{
					var requestedIterations = request.MapCalcSettings.TargetIterations;

					if (IsResponseComplete(mapSectionBytes, requestedIterations, out var reason))
					{
						request.FoundInRepo = true;
						request.ProcessingEndTime = DateTime.UtcNow;


						var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
						mapSectionVectors.IncreaseRefCount();
						var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors);

						//var backBuffer = new byte[request.BlockSize.NumberOfCells * 4];
						//var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities, backBuffer);
						//mapSectionResponse.MapSectionVectors2 = mapSectionVectors2;

						result.Add(new Tuple<MapSectionRequest, MapSectionResponse>(request, mapSectionResponse));
						//mapSectionVectors = null;

						PersistJobMapSectionRecord(request, mapSectionResponse, ct);
					}
				}
			}

			return result;
		}

		public void AddWork(int jobNumber, MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSection> responseHandler)
		{
			var mapSectionWorkItem = new MapSectionWorkRequest(jobNumber, mapSectionRequest, responseHandler);

			if (!_requestQueue.IsAddingCompleted)
			{
				_requestQueue.Add(mapSectionWorkItem);
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Not adding: {mapSectionWorkItem.Request}, The MapSectionRequestProcessor's RequestQueue IsAddingComplete has been set.");
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
			_mapSectionGeneratorProcessor?.Stop(immediately);
			_mapSectionResponseProcessor?.Stop(immediately);

			lock (_cancelledJobsLock)
			{
				if (_isStopped)
				{
					return;
				}

				if (immediately)
				{
					_requestQueueCts.Cancel();
					_returnQueueCts.Cancel();
				}
				else
				{
					if (!_requestQueue.IsCompleted && !_requestQueue.IsAddingCompleted)
					{
						_requestQueue.CompleteAdding();
					}

					if (!_returnQueue.IsCompleted && !_returnQueue.IsAddingCompleted)
					{
						_returnQueue.CompleteAdding();
					}


				}

				_isStopped = true;
			}

			try
			{
				for (var i = 0; i < _requestQueueProcessors.Length; i++)
				{
					if (_requestQueueProcessors[i].Wait(RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS * 1000))
					{
						Debug.WriteLine($"The MapSectionRequestProcesssor's RequestQueueProcessor Task #{i} has completed.");
					}
					else
					{
						Debug.WriteLine($"WARNING: The MapSectionRequestProcesssor's RequestQueueProcessor Task #{i} did not complete after waiting for {RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS} seconds.");
					}
				}

				if (_returnQueueProcess.Wait(RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS * 1000))
				{
					Debug.WriteLine($"The MapSectionRequestProcesssor's ReturnQueueProcessor Task has completed.");
				}
				else
				{
					Debug.WriteLine($"WARNING: The MapSectionRequestProcesssor's ReturnQueueProcessor Task did not complete after waiting for {RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS} seconds.");
				}
			}
			catch { }
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

		private async Task ProcessTheRequestQueueAsync(MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_requestQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _requestQueue.Take(ct);
					var mapSectionRequest = mapSectionWorkRequest.Request;

					var jobIsCancelled = IsJobCancelled(mapSectionWorkRequest.JobId);
					if (jobIsCancelled || mapSectionRequest.CancellationTokenSource.IsCancellationRequested)
					{
						var msg = $"The MapSectionRequestProcessor is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";

						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";

						mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, mapSectionWorkRequest.JobId, isCancelled: true);
						_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);
					}
					else
					{
						if (!UseRepo)
						{
							//var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
							PrepareRequestAndQueue(mapSectionWorkRequest, mapSectionGeneratorProcessor/*, mapSectionVectors*/);
						}
						else
						{
							var mapSectionResponse = await FetchOrQueueForGenerationAsync(mapSectionWorkRequest, mapSectionGeneratorProcessor, ct);

							var mapSectionVectors = mapSectionResponse?.MapSectionVectors;

							if (mapSectionVectors != null)
							{
								//MapSection mapSection; 
								//if (mapSectionResponse.MapSectionVectors != null)
								//{
								//	mapSection = CreateMapSection(mapSectionRequest, mapSectionResponse.MapSectionVectors, mapSectionWorkRequest.JobId);
								//}
								//else
								//{
								//	mapSection = CreateMapSection(mapSectionRequest, mapSectionResponse.MapSectionVectors2, mapSectionWorkRequest.JobId);
								//}

								var mapSection = CreateMapSection(mapSectionRequest, mapSectionVectors, mapSectionWorkRequest.JobId);

								mapSectionWorkRequest.Response = mapSection;
								_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);

							}
						}
					}
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLineIf(_useDetailedDebug, "The work queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The work queue got an exception: {e}.");
					throw;
				}
			}
		}

		private async Task<MapSectionResponse?> FetchOrQueueForGenerationAsync(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;
			var persistZValues = request.MapCalcSettings.SaveTheZValues;

			var mapSectionBytes = await FetchAsync(request, ct);

			if (mapSectionBytes != null)
			{
				var requestedIterations = request.MapCalcSettings.TargetIterations;

				if (IsResponseComplete(mapSectionBytes, requestedIterations, out var reason))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Got {request.ScreenPosition} from repo.");

					request.FoundInRepo = true;
					request.ProcessingEndTime = DateTime.UtcNow;

					var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
					mapSectionVectors.IncreaseRefCount();
					var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors);

					//var backBuffer = new byte[request.BlockSize.NumberOfCells * 4];
					//var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities, backBuffer);
					//var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors2);

					//var mapSectionVectors2 = _mapSectionVectorProvider.ObtainMapSectionVectors2();
					//var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors2);

					request.MapSectionId = mapSectionResponse.MapSectionId;

					PersistJobMapSectionRecord(request, mapSectionResponse, ct);

					return mapSectionResponse;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Requesting the iteration count to be increased for {request.ScreenPosition}. The response was incomplete for reason: {reason}.");
					request.IncreasingIterations = true;

					//request.MapSectionVectors = mapSectionResponse.MapSectionVectors;

					// TODO: Add a property to the MapSectionVectors class that tracks whether or not a MapSectionZValuesRecord exists on file. 
					if (UseRepo && persistZValues)
					{
						var mapSectionId = mapSectionBytes.Id;
						request.MapSectionId = mapSectionId.ToString();

						var zValues = await FetchTheZValuesAsync(mapSectionId, ct);
						if (zValues != null)
						{
							var mapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(request.LimbCount);
							mapSectionZVectors.Load(zValues.Zrs, zValues.Zis, zValues.HasEscapedFlags, zValues.RowsHasEscaped);

							request.MapSectionZVectors = mapSectionZVectors;

							//var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
							//mapSectionVectors.Load(mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
							//request.MapSectionVectors = mapSectionVectors;

							var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
							request.MapSectionVectors2 = mapSectionVectors2;

							//var mapSectionVectors2 = _mapSectionVectorProvider.ObtainMapSectionVectors2();

							//mapSectionVectors2.Counts = mapSectionBytes.Counts;
							//mapSectionVectors2.EscapeVelocities = mapSectionBytes.EscapeVelocities;

							//Array.Copy(mapSectionBytes.Counts, mapSectionVectors2.Counts, mapSectionBytes.Counts.Length);
							//Array.Copy(mapSectionBytes.EscapeVelocities, mapSectionVectors2.EscapeVelocities, mapSectionBytes.EscapeVelocities.Length);
						}
						//else
						//{
						//	request.MapSectionZVectors.ResetObject();
						//}
					}

					Debug.WriteLineIf(_useDetailedDebug, $"Requesting the iteration count to be increased for {request.ScreenPosition}.");
					QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);

					return null;
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Request for {request.ScreenPosition} not found in the repo: Queuing for generation.");
				PrepareRequestAndQueue(mapSectionWorkRequest, mapSectionGeneratorProcessor/*, mapSectionVectors*/);
				return null;
			}
		}

		private void PrepareRequestAndQueue(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor/*, MapSectionVectors mapSectionVectors*/)
		{
			var request = mapSectionWorkRequest.Request;
			//var persistZValues = request.MapCalcSettings.SaveTheZValues;

			request.MapSectionId = null;
			//request.IncreasingIterations = false;
			//request.MapSectionVectors = mapSectionVectors;
			//request.MapSectionVectors.ResetObject();

			//if (UseRepo && persistZValues)
			//{
			//	request.MapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(request.LimbCount);
			//	request.MapSectionZVectors.ResetObject();
			//}

			ReportQueueForGeneration(request.ScreenPosition, request.MapCalcSettings.SaveTheZValues, request.LimbCount);

			QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor);
		}

		private bool IsResponseComplete(MapSectionBytes mapSectionBytes, int requestedIterations, [NotNullWhen(false)] out string? reason)
		{
			//if (mapSectionResponse.MapSectionVectors == null)
			//{
			//	throw new InvalidOperationException("A MapSectionRecords was found in the repo with a null MapSectionVectors.");
			//}

			// TODO: Update the mapSectionResponse to include details about which rows are complete. This is required for those cases where the Generator was given a CancellationToken that got cancelled.

			if (!mapSectionBytes.Complete)
			{
				reason = "The response is not complete. Processing was interrupted during generation or during the last target iteration increase.";
				return false;
			}

			var fetchedTargetIterations = mapSectionBytes.MapCalcSettings?.TargetIterations ?? 0;

			if (fetchedTargetIterations >= requestedIterations)
			{
				//The MapSection fetched from the repository is the result of a request to generate at or above the current request's target iterations.
				reason = null;
				return true;
			}

			if (mapSectionBytes.AllRowsHaveEscaped)
			{
				reason = null;
				return true;
			}

			reason = $"IterationCountOnFile: {fetchedTargetIterations} is < requested {requestedIterations} and AllRowsHaveEscaped = false.";
			return false;
		}

		private int _requestCounter = 0;

		private void QueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor)
		{
			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

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

					var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, QueueGeneratedResponse);
					mapSectionGeneratorProcessor.AddWork(mapSectionGenerateRequest, ct);

					if (Interlocked.Increment(ref _requestCounter) % 10 == 0)
					{
						//Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
						Console.WriteLine($"The MapSectionRequestProcessor has processed {_requestCounter} requests.");
					}
				}
			}
			finally
			{
				_requestsLock.ExitWriteLock();
			}
		}

		//private async Task<MapSectionResponse?> FetchAsyncOld(MapSectionRequest mapSectionRequest, CancellationToken ct, MapSectionVectors mapSectionVectors)
		//{
		//	var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);

		//	// TODO: Add property: OriginalSourceSubdivisionId to the MapSectionResponse class.
		//	var mapSectionResponse = await _mapSectionAdapter.GetMapSectionAsync(subdivisionId, mapSectionRequest.BlockPosition, mapSectionVectors, ct);
		//	//mapSectionResponse.OriginalSourceSubdivisionId = mapSectionRequest.OriginalSourceSubdivisionId;

		//	return mapSectionResponse;
		//}

		private async Task<MapSectionBytes?> FetchAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);

			// TODO: Add property: OriginalSourceSubdivisionId to the MapSectionBytes class.
			var mapSectionBytes = await _mapSectionAdapter.GetMapSectionBytesAsync(subdivisionId, mapSectionRequest.BlockPosition, ct);
			//mapSectionBytes.OriginalSourceSubdivisionId = mapSectionRequest.OriginalSourceSubdivisionId;

			return mapSectionBytes;
		}

		//private MapSectionResponse? FetchOld(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors)
		//{
		//	var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
		//	var mapSectionResponse = _mapSectionAdapter.GetMapSection(subdivisionId, mapSectionRequest.BlockPosition, mapSectionVectors);

		//	return mapSectionResponse;
		//}

		private MapSectionBytes? Fetch(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var mapSectionBytes = _mapSectionAdapter.GetMapSectionBytes(subdivisionId, mapSectionRequest.BlockPosition);

			return mapSectionBytes;
		}

		private async Task<ZValues?> FetchTheZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionAdapter.GetMapSectionZValuesAsync(mapSectionId, ct);

			return result;
		}

		private void QueueGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse)
		{
			if (!_returnQueue.IsAddingCompleted)
			{
				var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, HandleGeneratedResponse, mapSectionResponse);
				_returnQueue.Add(mapSectionGenerateRequest);
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Not adding: {mapSectionWorkRequest.Request}, The MapSectionRequestProcessor's ReturnQueue IsAddingComplete has been set.");
			}
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse)
		{
			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

			_requestsLock.EnterUpgradeableReadLock();

			if (UseRepo)
			{
				PersistResponse(mapSectionWorkRequest.Request, mapSectionResponse, ct);
			}

			try
			{
				mapSectionWorkRequest.Response = BuildMapSection(mapSectionWorkRequest.Request, mapSectionResponse, mapSectionWorkRequest.JobId);
				_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);

				var pendingRequests = GetPendingRequests(mapSectionWorkRequest.Request);
				Debug.WriteLineIf(_useDetailedDebug, $"Handling generated response, the count is {pendingRequests.Count} for request: {mapSectionWorkRequest.Request}");

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
						workItem.Response = BuildMapSection(workItem.Request, mapSectionResponse, workItem.JobId);
						_mapSectionResponseProcessor.AddWork(workItem, ct);
					}
				}
			}
			finally
			{
				if (mapSectionResponse != null)
				{
					_mapSectionVectorProvider.ReturnMapSectionResponse(mapSectionResponse);
				}

				_requestsLock.ExitUpgradeableReadLock();
			}
		}

		private MapSection BuildMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, int jobId)
		{
			MapSection mapSection;

			if (mapSectionResponse.RequestCancelled)
			{
				mapSection = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, jobId, isCancelled: true);
			}
			else
			{
				mapSectionResponse.MapSectionVectors2?.IncreaseRefCount();
				mapSection = CreateMapSection(mapSectionRequest, mapSectionResponse.MapSectionVectors2, jobId);
				mapSection.MathOpCounts = mapSectionResponse.MathOpCounts;
			}

			return mapSection;
		}

		private MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors? mapSectionVectors, int jobId)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors == null)
			{
				Debug.WriteLine($"WARNING: Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors is empty. The request's block position is {mapSectionRequest.BlockPosition}.");
				mapSectionResult = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, jobId, isCancelled: false);
			}
			else
			{
				mapSectionResult = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors, jobId);
			}

			return mapSectionResult;
		}

		private MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors2? mapSectionVectors2, int jobId)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors2 == null)
			{
				Debug.WriteLine($"WARNING: Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors2 is empty. The request's block position is {mapSectionRequest.BlockPosition}.");
				mapSectionResult = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, jobId, isCancelled: false);
			}
			else
			{
				//var mapSectionVectors = new MapSectionVectors(RMapConstants.BLOCK_SIZE);
				var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
				mapSectionVectors.IncreaseRefCount();

				mapSectionVectors.Load(mapSectionVectors2.Counts, mapSectionVectors2.EscapeVelocities);

				mapSectionResult = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors, jobId);
			}

			return mapSectionResult;
		}

		private void PersistJobMapSectionRecord(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, CancellationToken ct)
		{
			var copyWithNoVectors = mapSectionResponse.CreateCopySansVectors();
			_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, copyWithNoVectors, onlyInsertJobMapSectionRecord: true), ct);
		}

		private void PersistResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, CancellationToken ct)
		{
			// Send work to the Persist processor
			// If the request is not cancelled -- OR -- if SaveTheZValues is 'On'.
			if (!mapSectionResponse.RequestCancelled || mapSectionRequest.MapCalcSettings.SaveTheZValues)
			{
				var zRefCnt = mapSectionResponse.MapSectionZVectors?.ReferenceCount ?? 0;
				Debug.Assert(zRefCnt == 0 || zRefCnt == 1, "PersistResponse: The MapSectionZVectors Reference Count should be either 0 or 1.");

				Debug.Assert(mapSectionResponse.MapSectionVectors == null, "PersistResponse: The MapSectionVectors should be null.");

				mapSectionResponse.MapSectionVectors2?.IncreaseRefCount();
				mapSectionResponse.MapSectionZVectors?.IncreaseRefCount();
				_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, mapSectionResponse), ct);
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

		private MapSectionResponse MapFrom(MapSectionBytes target, MapSectionVectors mapSectionVectors)
		{
			mapSectionVectors.Load(target.Counts, target.EscapeVelocities);

			var result = new MapSectionResponse
			(
				mapSectionId: target.Id.ToString(),
				subdivisionId: target.SubdivisionId.ToString(),
				blockPosition: target.BlockPosition,
				mapCalcSettings: target.MapCalcSettings,
				requestCompleted: target.Complete,
				allRowsHaveEscaped: target.AllRowsHaveEscaped,
				mapSectionVectors: mapSectionVectors
			);

			return result;
		}

		[Conditional("DEBUG2")]
		private void WasPrimaryRequestFound(MapSectionWorkRequest mapSectionWorkRequest, IList<MapSectionWorkRequest> pendingRequests)
		{
			if (!RequestExists(mapSectionWorkRequest.Request, pendingRequests))
			{
				Debug.WriteLine("WARNING: The primary request was not included in the list of pending requests.");
			}
		}

		[Conditional("DEBUG")]
		private void ReportQueueForGeneration(PointInt screenPosition, bool saveTheZValues, int limbCount)
		{
			if (screenPosition.IsZero())
			{
				var note = saveTheZValues ? $"Saving the ZValues. LimbCount = {limbCount}." : "Not Saving the ZValues.";
				Debug.WriteLineIf(_useDetailedDebug, $"Requesting {screenPosition} to be generated. {note}.");
			}
		}

		private bool RequestExists(MapSectionRequest mapSectionRequest, IEnumerable<MapSectionWorkRequest> workRequests)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var result = workRequests.Any(x => x.Request.SubdivisionId == subdivisionId && x.Request.BlockPosition == mapSectionRequest.BlockPosition);

			return result;
		}

		#endregion

		#region Private Methods - Return Queue

		private void ProcessTheReturnQueue(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_returnQueue.IsCompleted)
			{
				try
				{
					var mapSectionGenerateRequest = _returnQueue.Take(ct);

					var mapSectionResponse = mapSectionGenerateRequest.Response;

					if (mapSectionResponse != null)
					{
						mapSectionGenerateRequest.RunWorkAction(mapSectionResponse);
					}
					else
					{
						Debug.WriteLine($"WARNING: The return processor found a MapSectionGenerateRequest with a null Response value.");
					}
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The response queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The return queue got an exception: {e}.");
					throw;
				}
			}
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
					if (_requestQueueCts != null)
					{
						_requestQueueCts.Dispose();
					}

					if (_requestQueue != null)
					{
						_requestQueue.Dispose();
					}

					if (_returnQueueCts != null)
					{
						_returnQueueCts.Dispose();
					}

					if (_returnQueue != null)
					{
						_returnQueue.Dispose();
					}


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
