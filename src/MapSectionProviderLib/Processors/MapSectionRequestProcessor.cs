using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		#region Private Properties

		private const int NUMBER_OF_REQUEST_CONSUMERS = 1;
		private const int REQUEST_QUEUE_CAPACITY = 200;

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private readonly MapSectionGeneratorProcessor _mapSectionGeneratorProcessor;
		private readonly MapSectionResponseProcessor _mapSectionResponseProcessor;
		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private readonly CancellationTokenSource _requestQueueCts;
		private readonly BlockingCollection<MapSectionWorkRequest> _requestQueue;

		private readonly Action<MapSectionWorkRequest, MapSectionResponse> _generatorWorkRequestWorkAction;

		private readonly Task[] _requestQueueProcessors;
		private readonly int[] _requestCounters;

		private readonly object _cancelledJobsLock;
		private readonly List<int> _cancelledJobIds;

		private int _nextJobId;
		private bool disposedValue;

		private bool _isStopped;

		//private const int RETURN_QUEUE_CAPACITY = 200;
		//private readonly bool _useTheReturnQueue = false;
		//private readonly CancellationTokenSource? _returnQueueCts;
		//private readonly BlockingCollection<MapSectionGenerateRequest>? _returnQueue;
		//private readonly Task? _returnQueueProcess;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionRequestProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider,
			MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			UseRepo = true;

			_nextJobId = 0;
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionBuilder = new MapSectionBuilder();

			_mapSectionGeneratorProcessor = mapSectionGeneratorProcessor;
			_mapSectionResponseProcessor = mapSectionResponseProcessor;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

			_cancelledJobsLock = new object();
			_cancelledJobIds = new List<int>();

			_requestQueueCts = new CancellationTokenSource();
			_requestQueue = new BlockingCollection<MapSectionWorkRequest>(REQUEST_QUEUE_CAPACITY);

			_generatorWorkRequestWorkAction = HandleGeneratedResponse;
			(_requestQueueProcessors, _requestCounters) =  CreateTheRequestQueueProcessors();

			_isStopped = false;
		}


		private (Task[] processors, int[] counters) CreateTheRequestQueueProcessors()
		{
			var requestQueueProcessors = new Task[NUMBER_OF_REQUEST_CONSUMERS];
			var requestCounters = new int[NUMBER_OF_REQUEST_CONSUMERS];

			//for (var processorIndex = 0; processorIndex < _requestQueueProcessors.Length; processorIndex++)
			//{
			//	_requestCounters[processorIndex] = 0;
			//	_requestQueueProcessors[processorIndex] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, processorIndex, _requestQueueCts.Token));
			//}

			requestCounters[0] = 0;
			requestQueueProcessors[0] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, 0, _requestQueueCts.Token));
			//_requestCounters[1] = 0;
			//_requestQueueProcessors[1] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, 1, _requestQueueCts.Token));

			//requestCounters[0] = 0;
			//requestQueueProcessors[0] = Task.Run(() => ProcessTheRequestQueue(_mapSectionGeneratorProcessor, 0, _requestQueueCts.Token));

			return (requestQueueProcessors, requestCounters);
		}

		private void CreateTheReturnQueues()
		{
			//if (_useTheReturnQueue)
			//{
			//	_generatorWorkRequestWorkAction = QueueGeneratedResponse;

			//	_returnQueueCts = new CancellationTokenSource();
			//	_returnQueue = new BlockingCollection<MapSectionGenerateRequest>(RETURN_QUEUE_CAPACITY);
			//	_returnQueueProcess = Task.Run(() => ProcessTheReturnQueue(_returnQueue, _returnQueueCts.Token));
			//}
			//else
			//{
			//	_generatorWorkRequestWorkAction = HandleGeneratedResponse;

			//	_returnQueueCts = null;
			//	_returnQueue = null;
			//	_returnQueueProcess = null;
			//}
		}

		#endregion

		#region Public Properties

		public bool UseRepo { get; set; }

		public int NumberOfRequestsPending => _requestQueue.Count;
		//public int NumberOfReturnsPending => _returnQueue?.Count ?? 0;

		#endregion

		#region Public Methods

		public List<Tuple<MapSectionRequest, MapSectionResponse>> FetchResponses(List<MapSectionRequest> mapSectionRequests)
		{
			var cts = new CancellationTokenSource();
			var ct = cts.Token;

			var result = new List<Tuple<MapSectionRequest, MapSectionResponse>>();

			foreach (var request in mapSectionRequests)
			{
				var mapSectionBytes = Fetch(request);

				if (mapSectionBytes != null)
				{
					var requestedIterations = request.MapCalcSettings.TargetIterations;

					if (DoesTheResponseSatisfyTheRequest(mapSectionBytes, requestedIterations, out _))
					{
						request.FoundInRepo = true;
						request.ProcessingEndTime = DateTime.UtcNow;

						var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
						var mapSectionResponse = MapFrom(mapSectionBytes, mapSectionVectors);

						result.Add(new Tuple<MapSectionRequest, MapSectionResponse>(request, mapSectionResponse));

						PersistJobMapSectionRecord(request, mapSectionResponse, ct);

						var mirror = request.Mirror;
						if (mirror != null)
						{
							mirror.FoundInRepo = true;
							mirror.ProcessingEndTime = DateTime.UtcNow;

							mapSectionResponse.MapSectionVectors?.IncreaseRefCount();
							mapSectionResponse.MapSectionZVectors?.IncreaseRefCount();
							result.Add(new Tuple<MapSectionRequest, MapSectionResponse>(mirror, mapSectionResponse));

							PersistJobMapSectionRecord(mirror, mapSectionResponse, ct);
						}
					}
				}
			}

			return result;
		}

		public void AddWork(MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSection> responseHandler)
		{
			var mapSectionWorkItem = new MapSectionWorkRequest(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest, responseHandler);

			if (!_requestQueue.IsAddingCompleted)
			{
				_requestQueue.Add(mapSectionWorkItem);
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor. Not adding: {mapSectionWorkItem.Request}, The MapSectionRequestProcessor's RequestQueue IsAddingComplete has been set.");
			}
		}

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

					//if (_returnQueueCts != null) 
					//	_returnQueueCts.Cancel();
				}
				else
				{
					if (!_requestQueue.IsCompleted && !_requestQueue.IsAddingCompleted)
					{
						_requestQueue.CompleteAdding();
					}

					//if (_returnQueue != null)
					//{
					//	if (!_returnQueue.IsCompleted && !_returnQueue.IsAddingCompleted)
					//	{
					//		_returnQueue.CompleteAdding();
					//	}
					//}
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
			}
			catch { }

			//if (_returnQueueProcess != null)
			//{
			//	try
			//	{
			//		if (_returnQueueProcess.Wait(RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS * 1000))
			//		{
			//			Debug.WriteLine($"The MapSectionRequestProcesssor's ReturnQueueProcessor Task has completed.");
			//		}
			//		else
			//		{
			//			Debug.WriteLine($"WARNING: The MapSectionRequestProcesssor's ReturnQueueProcessor Task did not complete after waiting for {RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS} seconds.");
			//		}
			//	}
			//	catch { }
			//}
		}

		public int GetNextJobNumber()
		{
			lock (_cancelledJobsLock)
			{
				var nextJobId = _nextJobId++;
			}

			return _nextJobId;
		}

		#endregion

		#region Private Methods Asychronous

		private async Task ProcessTheRequestQueueAsync(MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_requestQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _requestQueue.Take(ct);
					var mapSectionRequest = mapSectionWorkRequest.Request;

					var jobIsCancelled = IsJobCancelled(mapSectionWorkRequest.JobId);

					if (jobIsCancelled || mapSectionRequest.NeitherRequestNorMirrorIsInPlay)
					{
						var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
						Debug.WriteLineIf(_useDetailedDebug, msg);

						mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
						AddToResponseProcessorQueue(mapSectionWorkRequest, ct);

						var mirror = mapSectionWorkRequest.Request.Mirror;

						if (mirror != null)
						{
							var mapSectionForMirror = _mapSectionBuilder.CreateEmptyMapSection(mirror, isCancelled: true);
							var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
							AddToResponseProcessorQueue(mapSectionRequestForMirror, ct);
						}
					}
					else
					{
						if (mapSectionWorkRequest.Request.Cancelled)
						{
							mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
							AddToResponseProcessorQueue(mapSectionWorkRequest, ct);
						}
						else
						{
							var mirror = mapSectionWorkRequest.Request.Mirror;

							if (mirror != null && mirror.Cancelled)
							{
								var mapSectionForMirror = _mapSectionBuilder.CreateEmptyMapSection(mirror, isCancelled: true);
								var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
								AddToResponseProcessorQueue(mapSectionRequestForMirror, ct);
							}
						}

						if (!UseRepo)
						{
							QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex);
						}
						else
						{
							Tuple<MapSection, MapSection?>? mapSectionPair = await FetchOrQueueForGenerationAsync(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex, ct);

							if (mapSectionPair != null)
							{
								mapSectionWorkRequest.Response = mapSectionPair.Item1;
								AddToResponseProcessorQueue(mapSectionWorkRequest, ct);

								if (mapSectionPair.Item2 != null)
								{
									var mirror = mapSectionWorkRequest.Request.Mirror;
									if (mirror == null)
									{
										throw new InvalidOperationException("Receiving two MapSections, but the request has no mirror.");
									}

									var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionPair.Item2);
									AddToResponseProcessorQueue(mapSectionRequestForMirror, ct);
								}
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

		private async Task<Tuple<MapSection, MapSection?>?> FetchOrQueueForGenerationAsync(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
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

					var mapSection1 = CreateMapSection(request, mapSectionVectors);

					MapSection? mapSection2;

					var mirror = request.Mirror;
					if (mirror != null)
					{
						mirror.FoundInRepo = true;
						mirror.ProcessingEndTime = DateTime.UtcNow;
						
						PersistJobMapSectionRecord(mirror, mapSectionResponse, ct);

						// Use the same MapSectionVectors as does the first mapSection
						mapSectionVectors.IncreaseRefCount();
						mapSection2 = CreateMapSection(mirror, mapSectionVectors);
					}
					else
					{
						mapSection2 = null;
					}

					return new Tuple<MapSection, MapSection?>(mapSection1, mapSection2);
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

		private async Task<MapSectionBytes?> FetchAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var subdivisionId = mapSectionRequest.Subdivision.Id;
			var mapSectionBytes = await _mapSectionAdapter.GetMapSectionBytesAsync(subdivisionId, mapSectionRequest.SectionBlockOffset, ct);

			return mapSectionBytes;
		}

		private async Task<ZValues?> FetchTheZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionAdapter.GetMapSectionZValuesAsync(mapSectionId, ct);

			return result;
		}

		#endregion

		#region Private Methods Syncrhonous

		private void ProcessTheRequestQueue(MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_requestQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _requestQueue.Take(ct);
					var mapSectionRequest = mapSectionWorkRequest.Request;

					var jobIsCancelled = IsJobCancelled(mapSectionWorkRequest.JobId);

					if (jobIsCancelled || mapSectionRequest.NeitherRequestNorMirrorIsInPlay)
					{
						var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
						Debug.WriteLineIf(_useDetailedDebug, msg);

						mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
						AddToResponseProcessorQueue(mapSectionWorkRequest, ct);

						var mirror = mapSectionWorkRequest.Request.Mirror;

						if (mirror != null)
						{
							var mapSectionForMirror = _mapSectionBuilder.CreateEmptyMapSection(mirror, isCancelled: true);
							var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
							AddToResponseProcessorQueue(mapSectionRequestForMirror, ct);
						}
					}
					else
					{
						if (mapSectionWorkRequest.Request.Cancelled)
						{
							mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
							AddToResponseProcessorQueue(mapSectionWorkRequest, ct);
						}
						else
						{
							var mirror = mapSectionWorkRequest.Request.Mirror;

							if (mirror != null && mirror.Cancelled)
							{
								var mapSectionForMirror = _mapSectionBuilder.CreateEmptyMapSection(mirror, isCancelled: true);
								var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
								AddToResponseProcessorQueue(mapSectionRequestForMirror, ct);
							}
						}

						if (!UseRepo)
						{
							QueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex);
						}
						else
						{
							Tuple<MapSection, MapSection?>? mapSectionPair = FetchOrQueueForGeneration(mapSectionWorkRequest, mapSectionGeneratorProcessor, queueProcessorIndex, ct);

							if (mapSectionPair != null)
							{
								mapSectionWorkRequest.Response = mapSectionPair.Item1;
								AddToResponseProcessorQueue(mapSectionWorkRequest, ct);

								if (mapSectionPair.Item2 != null)
								{
									var mirror = mapSectionWorkRequest.Request.Mirror;
									if (mirror == null)
									{
										throw new InvalidOperationException("Receiving two MapSections, but the request has no mirror.");
									}

									var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionPair.Item2);
									AddToResponseProcessorQueue(mapSectionRequestForMirror, ct);
								}
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

		private Tuple<MapSection, MapSection?>? FetchOrQueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, int queueProcessorIndex, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;
			var persistZValues = request.MapCalcSettings.SaveTheZValues;

			var mapSectionBytes = Fetch(request);

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

					var mapSection1 = CreateMapSection(request, mapSectionVectors);

					MapSection? mapSection2;

					var mirror = request.Mirror;
					if (mirror != null)
					{
						mirror.FoundInRepo = true;
						mirror.ProcessingEndTime = DateTime.UtcNow;

						PersistJobMapSectionRecord(mirror, mapSectionResponse, ct);

						// Use the same MapSectionVectors as does the first mapSection
						mapSectionVectors.IncreaseRefCount();
						mapSection2 = CreateMapSection(mirror, mapSectionVectors);
					}
					else
					{
						mapSection2 = null;
					}

					return new Tuple<MapSection, MapSection?>(mapSection1, mapSection2);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The response was not satisfactory because: {reason}. ({request.ScreenPosition}).");

					// TODO: Add a property to the MapSectionVectors class that tracks whether or not a MapSectionZValuesRecord exists on file. 
					if (persistZValues)
					{
						var zValues = FetchTheZValues(mapSectionId);
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

		private MapSectionBytes? Fetch(MapSectionRequest mapSectionRequest)
		{
			var subdivisionId = mapSectionRequest.Subdivision.Id;
			var mapSectionBytes = _mapSectionAdapter.GetMapSectionBytes(subdivisionId, mapSectionRequest.SectionBlockOffset);

			return mapSectionBytes;
		}

		private ZValues? FetchTheZValues(ObjectId mapSectionId)
		{
			var result = _mapSectionAdapter.GetMapSectionZValues(mapSectionId);

			return result;
		}

		#endregion

		#region Private Methods

		private MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors? mapSectionVectors)
		{
			MapSection mapSectionResult;

			if (mapSectionVectors == null)
			{
				Debug.WriteLine($"WARNING: MapSectionRequestProcessor. Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors is empty. The request's block position is {mapSectionRequest.SectionBlockOffset}.");
				mapSectionResult = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: false);
			}
			else
			{
				mapSectionResult = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors);
			}

			return mapSectionResult;
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

			var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, _generatorWorkRequestWorkAction);
			mapSectionGeneratorProcessor.AddWork(mapSectionGenerateRequest, ct);

			if (Interlocked.Increment(ref _requestCounters[queueProcessorIndex]) % 10 == 0)
			{
				var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} has processed {_requestCounters[queueProcessorIndex]} requests.";
				Debug.WriteLineIf(_useDetailedDebug, msg);
				Console.WriteLine(msg);
			}
		}

		private void AddToResponseProcessorQueue(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
		{
			_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse)
		{
			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

			Debug.Assert(mapSectionWorkRequest.Request.MapLoaderJobNumber == mapSectionWorkRequest.JobId, "mm1");
			Debug.Assert(!mapSectionWorkRequest.Request.Pending, "Pending Items should not be InProcess.");

			if (UseRepo && !mapSectionResponse.RequestCancelled)
			{
				mapSectionResponse.MapSectionZVectors?.IncreaseRefCount();
				SendToPersistQueue(mapSectionWorkRequest.Request, mapSectionResponse, ct);
			}

			var mapSection = BuildMapSection(mapSectionWorkRequest.Request, mapSectionResponse);
			mapSectionWorkRequest.Response = mapSection;

			var mirror = mapSectionWorkRequest.Request.Mirror;

			if (mirror != null)
			{
				Debug.Assert(mirror.MapLoaderJobNumber == mapSectionWorkRequest.Request.MapLoaderJobNumber, "mm2");

				MapSection mapSectionForMirror;

				if (mapSection.MapSectionVectors != null)
				{
					// Use the same MapSectionVectors as does the first mapSection
					mapSection.MapSectionVectors.IncreaseRefCount();
					mapSectionForMirror = _mapSectionBuilder.CreateMapSection(mirror, mapSection.MapSectionVectors);
				}
				else
				{
					mapSectionForMirror = BuildMapSection(mirror, mapSectionResponse);
				}

				var mapSectionRequestForMirror = new MapSectionWorkRequest(mapSectionWorkRequest.JobId, mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
				_mapSectionResponseProcessor.AddWork(mapSectionRequestForMirror, ct);
			}

			// Only send the first until we've had a chance to build the second 
			_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);

			_mapSectionVectorProvider.ReturnToPool(mapSectionResponse);
		}

		private MapSection BuildMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			MapSection mapSection;

			if (mapSectionResponse.RequestCancelled)
			{
				mapSection = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
			}
			else
			{
				mapSection = CreateMapSectionFromBytes(mapSectionRequest, mapSectionResponse.MapSectionVectors2);
				mapSection.MathOpCounts = mapSectionResponse.MathOpCounts;
			}

			return mapSection;
		}

		private MapSection CreateMapSectionFromBytes(MapSectionRequest mapSectionRequest, MapSectionVectors2? mapSectionVectors2)
		{
			MapSection mapSection;

			if (mapSectionVectors2 == null)
			{
				Debug.WriteLine($"WARNING: MapSectionRequestProcessor. Cannot create a mapSectionResult from the mapSectionResponse, the MapSectionVectors2 is empty. The request's block position is {mapSectionRequest.SectionBlockOffset}.");
				mapSection = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: false);
			}
			else
			{
				var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
				mapSectionVectors.Load(mapSectionVectors2.Counts, mapSectionVectors2.EscapeVelocities);

				mapSection = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors);
			}

			return mapSection;
		}

		private void PersistJobMapSectionRecord(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, CancellationToken ct)
		{
			var copyWithNoVectors = mapSectionResponse.CreateCopySansVectors();
			_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, copyWithNoVectors, onlyInsertJobMapSectionRecord: true), ct);
		}

		private void SendToPersistQueue(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, CancellationToken ct)
		{
			CheckRequestResponseBeforePersist(mapSectionRequest, mapSectionResponse);

			// Send work to the Persist processor
			_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, mapSectionResponse), ct);
		}

		[Conditional("DEBUG")]
		private void CheckRequestResponseBeforePersist(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			Debug.Assert(mapSectionResponse.MapSectionVectors == null, "The MapSectionVectors should be null.");

			if (mapSectionRequest.MapCalcSettings.SaveTheZValues && !mapSectionResponse.AllRowsHaveEscaped)
			{
				if (mapSectionResponse.MapSectionZVectors == null)
				{
					Debug.WriteLine("WARNING:MapSectionRequestProcessor.The MapSectionZValues is null, but the SaveTheZValues setting is true.");
				}
			}

			if (mapSectionResponse.MapSectionZVectors != null)
			{
				Debug.Assert(mapSectionResponse.MapSectionZVectors.ReferenceCount == 1, "The MapSectionZVectors Reference Count should be 1.");
			}
			//else
			//{
			//	Debug.WriteLine("MapSectionRequestProcessor.The MapSectionZValues are not null.");
			//}
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

		//[Conditional("DEBUG")]
		//private void ReportQueueForGeneration(PointInt screenPosition, bool saveTheZValues, int limbCount)
		//{
		//	if (screenPosition.IsZero())
		//	{
		//		var note = saveTheZValues ? $"Saving the ZValues. LimbCount = {limbCount}." : "Not Saving the ZValues.";
		//		Debug.WriteLineIf(_useDetailedDebug, $"Requesting {screenPosition} to be generated. {note}.");
		//	}
		//}

		#endregion

		//#region Private Methods - Return Queue

		//private void ProcessTheReturnQueue(BlockingCollection<MapSectionGenerateRequest> returnQueue, CancellationToken ct)
		//{
		//	while (!ct.IsCancellationRequested && !returnQueue.IsCompleted)
		//	{
		//		try
		//		{
		//			var mapSectionGenerateRequest = returnQueue.Take(ct);

		//			var mapSectionResponse = mapSectionGenerateRequest.Response;

		//			if (mapSectionResponse != null)
		//			{
		//				mapSectionGenerateRequest.RunWorkAction(mapSectionResponse);
		//			}
		//			else
		//			{
		//				Debug.WriteLine($"WARNING: MapSectionRequestProcessor. The return processor found a MapSectionGenerateRequest with a null Response value.");
		//			}
		//		}
		//		catch (OperationCanceledException)
		//		{
		//			//Debug.WriteLine("The response queue got a OCE.");
		//		}
		//		catch (Exception e)
		//		{
		//			Debug.WriteLine($"MapSectionRequestProcessor. The return queue got an exception: {e}.");
		//			throw;
		//		}
		//	}
		//}

		//private void QueueGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse)
		//{
		//	if (_returnQueue != null && !_returnQueue.IsAddingCompleted)
		//	{
		//		var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest.JobId, mapSectionWorkRequest, HandleGeneratedResponse, mapSectionResponse);
		//		_returnQueue.Add(mapSectionGenerateRequest);
		//	}
		//	else
		//	{
		//		Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor. Not adding: {mapSectionWorkRequest.Request}, The MapSectionRequestProcessor's ReturnQueue IsAddingComplete has been set.");
		//	}
		//}

		//#endregion

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

					//if (_returnQueueCts != null)
					//{
					//	_returnQueueCts.Dispose();
					//}

					//if (_returnQueue != null)
					//{
					//	_returnQueue.Dispose();
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
