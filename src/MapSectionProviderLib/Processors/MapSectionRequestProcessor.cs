using MapSectionProviderLib.Support;
using MongoDB.Bson;
using MSS.Common;
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

		private const int NUMBER_OF_REQUEST_CONSUMERS = 1;
		private const int REQUEST_QUEUE_CAPACITY = 5;
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
		private readonly int[] _requestCounters;

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

		private readonly bool _useDetailedDebug = true;

		private readonly bool _combineRequests = true;

		#endregion

		#region Constructor

		public MapSectionRequestProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider,
			MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_isStopped = false;

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
			_requestCounters = new int[NUMBER_OF_REQUEST_CONSUMERS];

			//for (var processorIndex = 0; processorIndex < _requestQueueProcessors.Length; processorIndex++)
			//{
			//	_requestCounters[processorIndex] = 0;
			//	_requestQueueProcessors[processorIndex] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, processorIndex, _requestQueueCts.Token));
			//}

			_requestCounters[0] = 0;
			_requestQueueProcessors[0] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, 0, _requestQueueCts.Token));

			//_requestCounters[1] = 0;
			//_requestQueueProcessors[1] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, 1, _requestQueueCts.Token));


			_returnQueueCts = new CancellationTokenSource();
			_returnQueue = new BlockingCollection<MapSectionGenerateRequest>(RETURN_QUEUE_CAPACITY);

			_returnQueueProcess = Task.Run(() => ProcessTheReturnQueue(_returnQueueCts.Token));
		}

		#endregion

		#region Public Properties

		public bool UseRepo { get; set; }

		public int NumberOfRequestsPending => _requestQueue.Count;
		public int NumberOfReturnsPending => _returnQueue.Count;

		#endregion

		#region Public Methods

		public List<Tuple<MapSectionRequest, MapSectionResponse>> FetchResponses(List<MapSectionRequest> mapSectionRequests, out int jobNumber)
		{
			var cts = new CancellationTokenSource();
			var ct = cts.Token;

			jobNumber = GetNextRequestId();
			var result = new List<Tuple<MapSectionRequest, MapSectionResponse>>();

			foreach (var request in mapSectionRequests)
			{
				request.MapLoaderJobNumber = jobNumber;
				var mapSectionBytes = Fetch(request);

				if (mapSectionBytes != null)
				{
					var requestedIterations = request.MapCalcSettings.TargetIterations;

					if (DoesTheResponseSatisfyTheRequest(mapSectionBytes, requestedIterations, out var reason))
					{
						request.FoundInRepo = true;
						request.ProcessingEndTime = DateTime.UtcNow;

						var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
						var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors);

						result.Add(new Tuple<MapSectionRequest, MapSectionResponse>(request, mapSectionResponse));

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
				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor. Not adding: {mapSectionWorkItem.Request}, The MapSectionRequestProcessor's RequestQueue IsAddingComplete has been set.");
			}
		}

		public int GetNumberOfPendingRequests(int jobNumber)
		{
			_requestsLock.EnterReadLock();

			try
			{
				var result = _pendingRequests.Count(x => x.JobId == jobNumber);
				return result;
			}
			finally
			{
				_requestsLock.ExitReadLock();
			}
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

		private async Task ProcessTheRequestQueueAsync(MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
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
						var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
						Debug.WriteLineIf(_useDetailedDebug, msg);

						mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, mapSectionWorkRequest.JobId, isCancelled: true);
						AddToResponseProcessorQueue(mapSectionWorkRequest, ct);
					}
					else
					{
						if (!UseRepo)
						{
							QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex);
						}
						else
						{
							var mapSection = await FetchOrQueueForGenerationAsync(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex, ct);

							if (mapSection != null)
							{
								mapSectionWorkRequest.Response = mapSection;
								AddToResponseProcessorQueue(mapSectionWorkRequest, ct);
							}
							else
							{
								// A request has been sent which will result in the HandleGeneratedResponse callback being called.
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
					Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} got an exception: {e}.");
					throw;
				}
			}
		}

		private void AddToResponseProcessorQueue(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
		{
			//_requestsLock.EnterReadLock();

			//try
			//{
			//	_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);
			//}
			//finally
			//{
			//	_requestsLock.ExitReadLock();
			//}

			_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);

		}

		private MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors? mapSectionVectors, int jobNumber)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors == null)
			{
				Debug.WriteLine($"WARNING: MapSectionRequestProcessor. Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors is empty. The request's block position is {mapSectionRequest.SectionBlockOffset}.");
				mapSectionResult = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, jobNumber, isCancelled: false);
			}
			else
			{
				mapSectionResult = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors, jobNumber);
			}

			return mapSectionResult;
		}

		private async Task<MapSection?> FetchOrQueueForGenerationAsync(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;
			var persistZValues = request.MapCalcSettings.SaveTheZValues;

			var mapSectionBytes = await FetchAsync(request, ct);

			if (mapSectionBytes != null)
			{
				var mapSectionId = mapSectionBytes.Id;
				request.MapSectionId = mapSectionId.ToString();
				
				var requestedIterations = request.MapCalcSettings.TargetIterations;

				if (DoesTheResponseSatisfyTheRequest(mapSectionBytes, requestedIterations, out var reason))
				{
					//Debug.WriteLineIf(_useDetailedDebug, $"Got {request.ScreenPosition} from repo.");

					request.FoundInRepo = true;
					request.ProcessingEndTime = DateTime.UtcNow;

					var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
					var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors);

					PersistJobMapSectionRecord(request, mapSectionResponse, ct);

					var mapSection = CreateMapSection(request, mapSectionVectors, mapSectionWorkRequest.JobId);

					return mapSection;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The response was not satisfactory because: {reason}. ({request.ScreenPosition}).");

					// TODO: Add a property to the MapSectionVectors class that tracks whether or not a MapSectionZValuesRecord exists on file. 
					if (persistZValues)
					{
						var zValues = await FetchTheZValuesAsync(mapSectionId, ct);
						if (zValues != null)
						{
							Debug.WriteLineIf(_useDetailedDebug, $"Requesting the iteration count to be increased for {request.ScreenPosition}.");
							request.IncreasingIterations = true;

							var mapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(request.LimbCount);
							mapSectionZVectors.Load(zValues.Zrs, zValues.Zis, zValues.HasEscapedFlags, zValues.RowsHasEscaped);
							request.MapSectionZVectors = mapSectionZVectors;

							var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
							request.MapSectionVectors2 = mapSectionVectors2;
						}
						else
						{
							Debug.WriteLineIf(_useDetailedDebug, $"Requesting the MapSection to be generated again for {request.ScreenPosition}.");
						}
					}

					QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex);

					return null;
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Request for {request.ScreenPosition} not found in the repo: Queuing for generation.");

				QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex);
				return null;
			}
		}

		private bool DoesTheResponseSatisfyTheRequest(MapSectionBytes mapSectionBytes, int requestedIterations, [NotNullWhen(false)] out string? reason)
		{
			// TODO: Update the mapSectionResponse to include details about which rows are complete. This is required for those cases where
			// the Generator was given a CancellationToken that got cancelled.

			if (!mapSectionBytes.RequestWasCompleted)
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

		private void QueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex)
		{
			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

			if (mapSectionWorkRequest == null)
			{
				throw new ArgumentNullException(nameof(mapSectionWorkRequest), "The mapSectionWorkRequest must be non-null.");
			}

			if (_combineRequests)
			{
				_requestsLock.EnterWriteLock();

				try
				{
					if (ThereIsAMatchingRequest(mapSectionWorkRequest.Request))
					{
						// There is already a request made for this same block, add our request to the queue
						mapSectionWorkRequest.Request.Pending = true;
						_pendingRequests.Add(mapSectionWorkRequest);
						return;
					}
					else
					{
						// Let other's know about our request.
						_pendingRequests.Add(mapSectionWorkRequest);
					}
				}
				finally
				{
					_requestsLock.ExitWriteLock();
				}
			}

			SendToGenerator(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex, ct);
		}

		private void SendToGenerator(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
		{
			var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, QueueGeneratedResponse);
			mapSectionGeneratorProcessor.AddWork(mapSectionGenerateRequest, ct);

			if (Interlocked.Increment(ref _requestCounters[queueProcessorIndex]) % 10 == 0)
			{
				var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} has processed {_requestCounters[queueProcessorIndex]} requests.";
				Debug.WriteLineIf(_useDetailedDebug, msg);
				Console.WriteLine(msg);
			}
		}

		private async Task<MapSectionBytes?> FetchAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var mapSectionBytes = await _mapSectionAdapter.GetMapSectionBytesAsync(subdivisionId, mapSectionRequest.SectionBlockOffset, ct);

			return mapSectionBytes;
		}

		private MapSectionBytes? Fetch(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var mapSectionBytes = _mapSectionAdapter.GetMapSectionBytes(subdivisionId, mapSectionRequest.SectionBlockOffset);

			return mapSectionBytes;
		}

		private async Task<ZValues?> FetchTheZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionAdapter.GetMapSectionZValuesAsync(mapSectionId, ct);

			return result;
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse)
		{
			_requestsLock.EnterUpgradeableReadLock();

			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

			var workRequestsToSend = new List<MapSectionWorkRequest>();

			try
			{
				_requestsLock.EnterWriteLock();

				try
				{
					Debug.Assert(!mapSectionWorkRequest.Request.Pending, "Pending Items should not be InProcess.");

					if (UseRepo)
					{
						PersistResponse(mapSectionWorkRequest.Request, mapSectionResponse, ct);
					}

					mapSectionWorkRequest.Response = BuildMapSection(mapSectionWorkRequest.Request, mapSectionResponse, mapSectionWorkRequest.JobId);
					workRequestsToSend.Add(mapSectionWorkRequest);

					if (_combineRequests)
					{
						ConfirmPrimaryRequestFound(mapSectionWorkRequest, _pendingRequests);
						var requestsForSameSection = GetMatchingRequests(mapSectionWorkRequest, _pendingRequests);


						//Debug.WriteLineIf(_useDetailedDebug, $"Handling generated response, the count is {pendingRequests.Count} for request: {mapSectionWorkRequest.Request}");

						if (requestsForSameSection.Count > 0)
						{
							Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor. Handling generated response, the count is {requestsForSameSection.Count} for request: {mapSectionWorkRequest.Request}");

							if (mapSectionWorkRequest.Response.RequestCancelled)
							{
								// The 'primary' request that these Pending requests were waiting for was cancelled.
								// Update one of the pending items to not pending, and add this request to the list of items to be generated. 
								var pendingItemPromotedToPrimary = false;

								foreach (var workItem in requestsForSameSection)
								{
									if (workItem.Request.Cancelled || workItem.Request.CancellationTokenSource.IsCancellationRequested)
									{
										Debug.WriteLine($"The Primary Request was cancelled and this pending request was also cancelled: {workItem}.");

										workItem.Response = BuildMapSection(workItem.Request, mapSectionResponse, workItem.JobId);
										workRequestsToSend.Add(workItem);
										_pendingRequests.Remove(workItem);
									}
									else
									{
										if (!pendingItemPromotedToPrimary)
										{
											Debug.WriteLine($"The Primary Request was cancelled, promoting workItem: {workItem} from Pending to Primary.");

											Debug.Assert(workItem.Request.Pending, "Each WorkItem in the list of PendingRequests (other than the original request) should have Pending = true.");
											workItem.Request.Pending = false;

											SendToGenerator(workItem, _mapSectionGeneratorProcessor, -1, ct);
											pendingItemPromotedToPrimary = true;
										}
										else
										{

										}
									}
								}

								if (!pendingItemPromotedToPrimary)
								{
									Debug.WriteLine($"The Primary Request was cancelled and no workItem was promoted to primary.");
								}
							}
							else
							{
								foreach (var workItem in requestsForSameSection)
								{
									workItem.Response = BuildMapSection(workItem.Request, mapSectionResponse, workItem.JobId);
									workRequestsToSend.Add(workItem);
									_pendingRequests.Remove(workItem);
								}
							}

							//// Process each pending item, regardless of whether the original request is cancelled.
							//foreach (var workItem in requestsForSameSection)
							//{
							//	workItem.Response = BuildMapSection(workItem.Request, mapSectionResponse, workItem.JobId);
							//	workRequestsToSend.Add(workItem);
							//	_pendingRequests.Remove(workItem);
							//}


						}
					}
				}
				finally
				{
					_requestsLock.ExitWriteLock();
				}
			}
			finally
			{
				foreach (var workRequestToSend in workRequestsToSend)
				{
					_mapSectionResponseProcessor.AddWork(workRequestToSend, ct);
				}

				if (mapSectionResponse != null)
				{
					_mapSectionVectorProvider.ReturnMapSectionResponse(mapSectionResponse);
				}

				_requestsLock.ExitUpgradeableReadLock();
			}
		}

		private MapSection BuildMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, int jobNumber)
		{
			MapSection mapSection;

			if (mapSectionResponse.RequestCancelled)
			{
				mapSection = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, jobNumber, isCancelled: true);
			}
			else
			{
				mapSection = CreateMapSectionFromBytes(mapSectionRequest, mapSectionResponse.MapSectionVectors2, jobNumber);
				mapSection.MathOpCounts = mapSectionResponse.MathOpCounts;
			}

			return mapSection;
		}

		private MapSection CreateMapSectionFromBytes(MapSectionRequest mapSectionRequest, MapSectionVectors2? mapSectionVectors2, int jobNumber)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors2 == null)
			{
				Debug.WriteLine($"WARNING: MapSectionRequestProcessor. Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors2 is empty. The request's block position is {mapSectionRequest.SectionBlockOffset}.");
				mapSectionResult = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, jobNumber, isCancelled: false);
			}
			else
			{
				var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
				mapSectionVectors.Load(mapSectionVectors2.Counts, mapSectionVectors2.EscapeVelocities);

				mapSectionResult = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors, jobNumber);
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

			// Update on 9/17/2023 -- not saving if request is cancelled and SaveTheZValues is true.
			if (!mapSectionResponse.RequestCancelled/* || mapSectionRequest.MapCalcSettings.SaveTheZValues*/)
			{
				var zRefCnt = mapSectionResponse.MapSectionZVectors?.ReferenceCount ?? 0;
				Debug.Assert(zRefCnt == 0 || zRefCnt == 1, "PersistResponse: The MapSectionZVectors Reference Count should be either 0 or 1.");

				Debug.Assert(mapSectionResponse.MapSectionVectors == null, "PersistResponse: The MapSectionVectors should be null.");

				if (mapSectionRequest.MapCalcSettings.SaveTheZValues && !mapSectionResponse.AllRowsHaveEscaped)
				{
					if (mapSectionResponse.MapSectionZVectors == null)
					{
						Debug.WriteLine("WARNING:MapSectionRequestProcessor.The MapSectionZValues is null, but the SaveTheZValues setting is true.");
					}
					//else
					//{
					//	Debug.WriteLine("MapSectionRequestProcessor.The MapSectionZValues are not null.");
					//}
				}

				// TODO: What is causing the MapSectionVectors to be returned early? The below is a hack until we can determine root cause.
				// Create a copy including the MapSectionVectors2 and ZVectors.
				var cpy = mapSectionResponse.CreateCopySansVectors();
				cpy.MapSectionVectors2 = mapSectionResponse.MapSectionVectors2;
				cpy.MapSectionZVectors = mapSectionResponse.MapSectionZVectors;

				// Remove the ZVectors from the main response to prevent it from being returned to the pool before it can be saved to disk.
				mapSectionResponse.MapSectionZVectors = null;

				//mapSectionResponse.MapSectionVectors2?.IncreaseRefCount();
				//mapSectionResponse.MapSectionZVectors?.IncreaseRefCount();

				_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, cpy), ct);
			}
		}

		// Returns true, if there is a "Primary" Request already in the queue
		private bool ThereIsAMatchingRequest(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var result = _pendingRequests.Any(x => (!x.Request.Pending) && x.Request.SubdivisionId == subdivisionId && x.Request.SectionBlockOffset == mapSectionRequest.SectionBlockOffset);

			return result;
		}

		// Find all matching requests.
		private List<MapSectionWorkRequest> GetMatchingRequests(MapSectionWorkRequest mapSectionWorkRequest, List<MapSectionWorkRequest> workRequests)
		{
			var subdivisionId = mapSectionWorkRequest.Request.SubdivisionId;
			var sectionBlockOffset = mapSectionWorkRequest.Request.SectionBlockOffset;
			var result = workRequests.Where(x => x.Request.SubdivisionId == subdivisionId && x.Request.SectionBlockOffset == sectionBlockOffset && x != mapSectionWorkRequest).ToList();

			return result;
		}

		//private bool RemoveFoundRequests(IList<MapSectionWorkRequest> requests)
		//{
		//	var result = true;

		//	foreach (var workItem in requests)
		//	{
		//		if (!_pendingRequests.Remove(workItem))
		//		{
		//			result = false;
		//		}
		//	}

		//	return result;
		//}

		private bool IsJobCancelled(int jobId)
		{
			bool result;
			lock(_cancelledJobsLock)
			{
				result = _cancelledJobIds.Contains(jobId);
			}

			return result;
		}

		// Copied from MSetRecordMapper
		private MapSectionResponse MapFrom(MapSectionBytes target, MapSectionVectors mapSectionVectors)
		{
			mapSectionVectors.Load(target.Counts, target.EscapeVelocities);

			var result = new MapSectionResponse
			(
				mapSectionId: target.Id.ToString(),
				subdivisionId: target.SubdivisionId.ToString(),
				blockPosition: target.BlockPosition,
				mapCalcSettings: target.MapCalcSettings,
				requestCompleted: target.RequestWasCompleted,
				allRowsHaveEscaped: target.AllRowsHaveEscaped,
				mapSectionVectors: mapSectionVectors
			);

			return result;
		}

		[Conditional("DEBUG")]
		private void ConfirmPrimaryRequestFoundLoose(MapSectionWorkRequest mapSectionWorkRequest, IList<MapSectionWorkRequest> workRequests)
		{
			var mapSectionRequest = mapSectionWorkRequest.Request;
			var matchingRequest = workRequests.FirstOrDefault(x => (!x.Request.Pending) && x.Request.SubdivisionId == mapSectionRequest.SubdivisionId && x.Request.SectionBlockOffset == mapSectionRequest.SectionBlockOffset);

			if (matchingRequest == null)
			{
				var subdivisionId = mapSectionWorkRequest.Request.SubdivisionId;
				var sectionBlockOffset = mapSectionWorkRequest.Request.SectionBlockOffset;
				Debug.WriteLine($"WARNING: MapSectionRequestProcessor: The primary request: {subdivisionId}/{sectionBlockOffset} was not included in the list of pending requests.");
			}
			else
			{
				if (_useDetailedDebug)
				{
					var hasReferenceEquality = matchingRequest.Request == mapSectionRequest;

					if (hasReferenceEquality)
					{
						Debug.WriteLine($"Primary Request found and it is the same object.");
					}
					else
					{
						Debug.WriteLine($"Primary Request found with same SubdivisionId and SectionOffset but it is not the same object.");
					}

				}
			}

			//Debug.Assert(primaryRequestIsFound, "The primary request was not included in the list of pending requests.");
		}

		[Conditional("DEBUG")]
		private void ConfirmPrimaryRequestFound(MapSectionWorkRequest mapSectionWorkRequest, IList<MapSectionWorkRequest> workRequests)
		{
			var doesContain = workRequests.Any(x => x.Request == mapSectionWorkRequest.Request);

			Debug.Assert(doesContain, "does contain should be true here.");
		}

		//[Conditional("DEBUG")]
		//private void ReportQueueForGeneration(PointInt screenPosition, bool saveTheZValues, int limbCount)
		//{
		//	if (screenPosition.IsZero())
		//	{
		//		var note = saveTheZValues ? $"Saving the ZValues. LimbCount = {limbCount}." : "Not Saving the ZValues.";
		//		Debug.WriteLineIf(_useDetailedDebug, $"Requesting {screenPosition} to be generated. {note}.");
		//	}
		//}

		//private bool RequestExists(MapSectionRequest mapSectionRequest, IEnumerable<MapSectionWorkRequest> workRequests)
		//{
		//	var result = workRequests.Any(x => (!x.Request.Pending) && x.Request.SubdivisionId == mapSectionRequest.SubdivisionId && x.Request.SectionBlockOffset == mapSectionRequest.SectionBlockOffset);

		//	return result;
		//}

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
						Debug.WriteLine($"WARNING: MapSectionRequestProcessor. The return processor found a MapSectionGenerateRequest with a null Response value.");
					}
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The response queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"MapSectionRequestProcessor. The return queue got an exception: {e}.");
					throw;
				}
			}
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
				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor. Not adding: {mapSectionWorkRequest.Request}, The MapSectionRequestProcessor's ReturnQueue IsAddingComplete has been set.");
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
