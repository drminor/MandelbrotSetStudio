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

		private int _nextJobId;
		private bool disposedValue;

		private bool _isStopped;

		private readonly bool _useDetailedDebug = true;

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

			//requestCounters[0] = 0;
			//requestQueueProcessors[0] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, 0, _requestQueueCts.Token));
			//_requestCounters[1] = 0;
			//_requestQueueProcessors[1] = Task.Run(async () => await ProcessTheRequestQueueAsync(_mapSectionGeneratorProcessor, 1, _requestQueueCts.Token));

			requestCounters[0] = 0;
			requestQueueProcessors[0] = Task.Run(() => ProcessTheRequestQueue(queueProcessorIndex: 0, _requestQueueCts.Token));

			return (requestQueueProcessors, requestCounters);
		}

		#endregion

		#region Public Properties

		public bool UseRepo { get; set; }

		public int NumberOfRequestsPending => _requestQueue.Count;

		public int NumberOfSectionsPendingGeneration => _mapSectionGeneratorProcessor.NumberOfRequestsPending;

		//public int NumberOfReturnsPending => _returnQueue?.Count ?? 0;

		#endregion

		#region Public Methods

		public List<MapSection> SubmitRequests(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSectionRequest, MapSection> callback, CancellationToken ct, out List<MapSectionRequest> pendingGeneration)
		{
			pendingGeneration = new List<MapSectionRequest>();
			var result = new List<MapSection>();

			foreach(var mapSectionRequest in mapSectionRequests)
			{
				var mapSectionWorkRequest = new MapSectionWorkRequest(mapSectionRequest, callback);

				if (!UseRepo)
				{
					QueueForProcessing(mapSectionWorkRequest);
					pendingGeneration.Add(mapSectionRequest);
				}
				else
				{
					Tuple<MapSection?, MapSection?>? mapSectionPair = FetchOrQueueForProcessing(mapSectionWorkRequest, ct);

					if (mapSectionPair != null)
					{
						if (mapSectionPair.Item1 != null && !mapSectionPair.Item1.RequestCancelled) 
						{
							mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
							result.Add(mapSectionPair.Item1);
						}

						if (mapSectionPair.Item2 != null && !mapSectionPair.Item2.RequestCancelled)
						{
							mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
							result.Add(mapSectionPair.Item2);
						}
					}
					else
					{
						pendingGeneration.Add(mapSectionRequest);
					}
				}
			}

			return result;
		}

		private Tuple<MapSection?, MapSection?>? FetchOrQueueForProcessing(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;

			var mapSectionBytes = Fetch(request);

			if (mapSectionBytes != null)
			{
				var mapSectionId = mapSectionBytes.Id;
				request.MapSectionId = mapSectionId.ToString();

				var requestedIterations = request.MapCalcSettings.TargetIterations;

				// TODO: Update the Logic in FetchOrQueueForProcessing to only get Metadata instead of the actual bytes to determine if the record satifyies the request.
				if (DoesTheResponseSatisfyTheRequest(mapSectionBytes, requestedIterations, out var reason))
				{
					//Debug.WriteLineIf(_useDetailedDebug, $"Got {request.ScreenPosition} from repo.");

					var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
					mapSectionVectors.Load(mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
					var result = CreateTheMapSections(mapSectionVectors, request, ct);
					PersistJobMapSectionRecord(request, ct);

					_mapSectionVectorProvider.ReturnMapSectionVectors(mapSectionVectors);

					request.FoundInRepo = true;
					request.ProcessingEndTime = DateTime.UtcNow;

					return result;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"FetchOrQueueForProcessing: The response was not satisfactory because: {reason}. ({request.ScreenPosition}).");

					var persistZValues = request.MapCalcSettings.SaveTheZValues;

					if (persistZValues)
					{
						var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
						request.MapSectionVectors2 = mapSectionVectors2;
					}
					else
					{
						Debug.WriteLine("WARNING: FetchOrQueueForProcessing fetched, but did not use the 64Kb MapSectionBytes object.");
					}

					QueueForProcessing(mapSectionWorkRequest);

					return null;
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"FetchOrQueueForProcessing Request for {request.ScreenPosition} not found in the repo: Queuing for generation.");

				QueueForProcessing(mapSectionWorkRequest);
				return null;
			}
		}

		private void QueueForProcessing(MapSectionWorkRequest mapSectionWorkRequest)
		{
			if (!_requestQueue.IsAddingCompleted)
			{
				if (mapSectionWorkRequest.Request.NeitherRegularOrInvertedRequestIsInPlay)
				{
					Debug.WriteLine("Queuing for Procesing a cancelled request.");
				}
				_requestQueue.Add(mapSectionWorkRequest);
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor:QueueForProcessing: Not adding: {mapSectionWorkRequest.Request}, The MapSectionRequestProcessor's RequestQueue IsAddingComplete has been set.");
			}
		}

		public void Stop(bool immediately)
		{
			_mapSectionGeneratorProcessor?.Stop(immediately);
			_mapSectionResponseProcessor?.Stop(immediately);

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
			return Interlocked.Increment(ref _nextJobId);
		}

		#endregion

		#region Private Methods

		private void ProcessTheRequestQueue(int queueProcessorIndex, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_requestQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _requestQueue.Take(ct);
					var mapSectionRequest = mapSectionWorkRequest.Request;

					var jobIsCancelled = mapSectionWorkRequest.JobIsCancelled;

					if (jobIsCancelled || mapSectionRequest.NeitherRegularOrInvertedRequestIsInPlay)
					{
						var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} is skipping request with JobId/Request#: {mapSectionRequest.MapLoaderJobNumber}/{mapSectionRequest.RequestNumber}.";
						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
						Debug.WriteLineIf(_useDetailedDebug, msg);
					}
					else
					{
						if (!UseRepo)
						{
							QueueForGeneration(mapSectionWorkRequest, queueProcessorIndex);
						}
						else
						{
							Tuple<MapSection?, MapSection?>? mapSectionPair = FetchOrQueueForGeneration(mapSectionWorkRequest, queueProcessorIndex, ct);

							if (mapSectionPair != null)
							{
								if (mapSectionPair.Item1 != null && !mapSectionPair.Item1.RequestCancelled)
								{
									mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
									mapSectionWorkRequest.Response = mapSectionPair.Item1;
									QueueTheResponse(mapSectionWorkRequest, ct);
								}

								if (mapSectionPair.Item2 != null && !mapSectionPair.Item2.RequestCancelled)
								{
									mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
									var mapSectionRequestForInverted = new MapSectionWorkRequest(mapSectionRequest, mapSectionWorkRequest.WorkAction, mapSectionPair.Item2);
									QueueTheResponse(mapSectionRequestForInverted, ct);
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

		private Tuple<MapSection?, MapSection?>? FetchOrQueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, int queueProcessorIndex, CancellationToken ct)
		{
			var request = mapSectionWorkRequest.Request;
			var persistZValues = request.MapCalcSettings.SaveTheZValues;

			if (request.MapSectionVectors2 != null)
			{
				if (persistZValues)
				{
					UpdateWithZValues(request);
				}

				QueueForGeneration(mapSectionWorkRequest, queueProcessorIndex);

				return null;
			}
			else
			{
				var mapSectionBytes = Fetch(request);

				if (mapSectionBytes != null)
				{
					var mapSectionId = mapSectionBytes.Id;
					request.MapSectionId = mapSectionId.ToString();

					var requestedIterations = request.MapCalcSettings.TargetIterations;

					if (DoesTheResponseSatisfyTheRequest(mapSectionBytes, requestedIterations, out var reason))
					{
						//Debug.WriteLineIf(_useDetailedDebug, $"Got {request.ScreenPosition} from repo.");
						var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
						mapSectionVectors.Load(mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);

						var result = CreateTheMapSections(mapSectionVectors, request, ct);

						UpdateMsrJobWithResultCounts(result, request);

						PersistJobMapSectionRecord(request, ct);
						_mapSectionVectorProvider.ReturnMapSectionVectors(mapSectionVectors);

						request.FoundInRepo = true;
						request.ProcessingEndTime = DateTime.UtcNow;

						return result;
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"The response was not satisfactory because: {reason}. ({request.ScreenPosition}).");

						// TODO: Add a property to the MapSectionVectors class that tracks whether or not a MapSectionZValuesRecord exists on file. 
						if (persistZValues)
						{
							UpdateWithZValues(request);
						}

						QueueForGeneration(mapSectionWorkRequest, queueProcessorIndex);
						return null;
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Request for {request.ScreenPosition} not found in the repo: Queuing for generation.");

					QueueForGeneration(mapSectionWorkRequest, queueProcessorIndex);
					return null;
				}
			}
		}

		private void UpdateMsrJobWithResultCounts(Tuple<MapSection?, MapSection?>? result, MapSectionRequest request)
		{
			var sectionsFoundInRepo = 0;
			var sectionsCancelled = 0;

			if (result != null)
			{
				if (result.Item1 != null)
				{
					sectionsFoundInRepo++;
				}
				else
				{
					if (request.RegularPosition != null)
						sectionsCancelled++;
				}

				if (result.Item2 != null)
				{
					sectionsFoundInRepo++;
				}
				else
				{
					if (request.InvertedPosition != null)
						sectionsCancelled++;
				}
			}
			else
			{
				if (request.IsPaired)
				{
					sectionsCancelled += 2;
				}
				else
				{
					sectionsCancelled++;
				}
			}

			request.MsrJob.SectionsFoundInRepo += sectionsFoundInRepo;
			request.MsrJob.SectionsCancelled += sectionsCancelled;
		}

		private Tuple<MapSection?, MapSection?>? CreateTheMapSections(MapSectionVectors mapSectionVectors, MapSectionRequest request, CancellationToken ct)
		{
			MapSection? mapSection1;
			MapSection? mapSection2;

			if (request.RegularPosition != null && !request.RegularPosition.IsCancelled)
			{
				mapSectionVectors.IncreaseRefCount();
				mapSection1 = _mapSectionBuilder.CreateMapSection(request, isInverted: false, mapSectionVectors);

			}
			else
			{
				mapSection1 = null;
			}

			if (request.InvertedPosition != null && !request.InvertedPosition.IsCancelled)
			{
				mapSectionVectors.IncreaseRefCount();
				mapSection2 = _mapSectionBuilder.CreateMapSection(request, isInverted: true, mapSectionVectors);
			}
			else
			{
				mapSection2 = null;
			}

			return new Tuple<MapSection?, MapSection?>(mapSection1, mapSection2);
		}

		private void UpdateWithZValues(MapSectionRequest request)
		{
			var mapSectionId = ObjectId.Parse(request.MapSectionId);
			var zValues = FetchTheZValues(mapSectionId);
			if (zValues != null)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Requesting the iteration count to be increased for {request.ScreenPosition}.");
				request.IncreasingIterations = true;

				var mapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(request.LimbCount);
				mapSectionZVectors.Load(zValues.Zrs, zValues.Zis, zValues.HasEscapedFlags, zValues.RowsHasEscaped);
				request.MapSectionZVectors = mapSectionZVectors;
			}
			else
			{
				request.MapSectionVectors2 = null;
				Debug.WriteLine("WARNING: Because the ZValues were not available, we fetched, but did not use the 64Kb MapSectionBytes object.");

				Debug.WriteLineIf(_useDetailedDebug, $"Requesting the MapSection to be generated again for {request.ScreenPosition}.");
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

		private void QueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, int queueProcessorIndex)
		{
			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

			if (mapSectionWorkRequest == null)
			{
				throw new ArgumentNullException(nameof(mapSectionWorkRequest), "The mapSectionWorkRequest must be non-null.");
			}

			if (mapSectionWorkRequest.Request.NeitherRegularOrInvertedRequestIsInPlay)
			{
				Debug.WriteLine("Queuing for Generation a Cancelled request.");
			}

			var mapSectionGenerateRequest = new MapSectionGenerateRequest(mapSectionWorkRequest, _generatorWorkRequestWorkAction);
			_mapSectionGeneratorProcessor.AddWork(mapSectionGenerateRequest, ct);

			if (Interlocked.Increment(ref _requestCounters[queueProcessorIndex]) % 10 == 0)
			{
				var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} has processed {_requestCounters[queueProcessorIndex]} requests.";
				Debug.WriteLineIf(_useDetailedDebug, msg);
				Console.WriteLine(msg);
			}
		}

		private void HandleGeneratedResponse(MapSectionWorkRequest mapSectionWorkRequest, MapSectionResponse mapSectionResponse)
		{
			// Use our CancellationSource when adding work
			var ct = _requestQueueCts.Token;

			Debug.Assert(mapSectionWorkRequest.Request.MapLoaderJobNumber == mapSectionWorkRequest.JobNumber, "mm1");
			Debug.Assert(!mapSectionWorkRequest.Request.Pending, "Pending Items should not be InProcess.");

			var mapSectionRequest = mapSectionWorkRequest.Request;

			if (mapSectionResponse.MapSectionVectors2 == null)
			{
				Debug.WriteLine($"CHECK THIS: MapSectionRequestProcessor is not Handling the Generated Response, the MapSectionVectors2 is null. ResponseIsCancelled = {mapSectionResponse.RequestCancelled}, RequestIsCancelled = {mapSectionRequest.IsCancelled} Request = {mapSectionRequest}.");
				return;
			}

			if (UseRepo && !mapSectionResponse.RequestCancelled)
			{
				CheckRequestResponseBeforePersist(mapSectionRequest, mapSectionResponse);

				mapSectionResponse.MapSectionZVectors?.IncreaseRefCount();
				QueueForPersistence(mapSectionRequest, mapSectionResponse, ct);
			}

			var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
			mapSectionVectors.Load(mapSectionResponse.MapSectionVectors2.Counts, mapSectionResponse.MapSectionVectors2.EscapeVelocities);

			var mapSections = CreateTheMapSections(mapSectionVectors, mapSectionRequest, ct);

			if (mapSections != null)
			{
				if (mapSections.Item1 != null)
				{
					mapSectionWorkRequest.Response = mapSections.Item1;
					QueueTheResponse(mapSectionWorkRequest, ct);
				}

				if (mapSections.Item2 != null)
				{
					var mapSectionRequestForInverted = new MapSectionWorkRequest(mapSectionRequest, mapSectionWorkRequest.WorkAction, mapSections.Item2);
					QueueTheResponse(mapSectionRequestForInverted, ct);
				}
			}

			_mapSectionVectorProvider.ReturnMapSectionVectors(mapSectionVectors);
			_mapSectionVectorProvider.ReturnToPool(mapSectionResponse);
			mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;
		}

		private void QueueTheResponse(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
		{
			if (mapSectionWorkRequest.Request.NeitherRegularOrInvertedRequestIsInPlay)
			{
				Debug.WriteLine("Sending to the Response Processor a Cancelled request.");
			}
			_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);
		}

		private void QueueForPersistence(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, CancellationToken ct)
		{
			// Send work to the Persist processor
			_mapSectionPersistProcessor.AddWork(new MapSectionPersistRequest(mapSectionRequest, mapSectionResponse), ct);
		}

		private void PersistJobMapSectionRecord(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var persistRequest = new MapSectionPersistRequest(mapSectionRequest, response: null, onlyInsertJobMapSectionRecord: true);
			_mapSectionPersistProcessor.AddWork(persistRequest, ct);
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void CheckRequestResponseBeforePersist(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			Debug.Assert(mapSectionResponse.MapSectionVectors2 != null, "The MapSectionVectors2 should not be null.");
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

		#region Not Used

		//public List<MapSection> FetchResponses(List<MapSectionRequest> mapSectionRequests, out List<MapSectionRequest> notFoundInRepo)
		//{
		//	notFoundInRepo = new List<MapSectionRequest>();

		//	var cts = new CancellationTokenSource();
		//	var ct = cts.Token;

		//	var result = new List<MapSection>();

		//	foreach (var request in mapSectionRequests)
		//	{
		//		var mapSectionBytes = Fetch(request);

		//		if (mapSectionBytes != null)
		//		{
		//			var mapSectionId = mapSectionBytes.Id;
		//			var requestedIterations = request.MapCalcSettings.TargetIterations;

		//			if (DoesTheResponseSatisfyTheRequest(mapSectionBytes, requestedIterations, out var reason))
		//			{
		//				//Debug.WriteLineIf(_useDetailedDebug, $"Got {request.ScreenPosition} from repo.");

		//				var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
		//				mapSectionVectors.Load(mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
		//				request.MapSectionId = mapSectionId.ToString();

		//				var mapSection = CreateMapSection(request, mapSectionVectors, ct);
		//				if (mapSection != null)
		//				{
		//					result.Add(mapSection);
		//				}

		//				if (request.Mirror != null)
		//				{
		//					request.Mirror.MapSectionId = mapSectionId.ToString();
		//					mapSection = CreateMapSection(request.Mirror, mapSectionVectors, ct);
		//					if (mapSection != null)
		//					{
		//						result.Add(mapSection);
		//					}

		//				}
		//			}
		//			else
		//			{
		//				Debug.WriteLineIf(_useDetailedDebug, $"The response was not satisfactory because: {reason}. ({request.ScreenPosition}).");

		//				var persistZValues = request.MapCalcSettings.SaveTheZValues;

		//				if (persistZValues)
		//				{
		//					var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
		//					request.MapSectionVectors2 = mapSectionVectors2;
		//				}
		//				else
		//				{
		//					Debug.WriteLine("WARNING: We fetched, but did not use the 64Kb MapSectionBytes object.");
		//				}

		//				notFoundInRepo.Add(request);
		//			}
		//		}
		//		else
		//		{
		//			notFoundInRepo.Add(request);
		//		}
		//	}

		//	return result;
		//}

		//public void AddWork(MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSection> responseHandler)
		//{
		//	var mapSectionWorkItem = new MapSectionWorkRequest(mapSectionRequest, responseHandler);

		//	if (!_requestQueue.IsAddingCompleted)
		//	{
		//		_requestQueue.Add(mapSectionWorkItem);
		//	}
		//	else
		//	{
		//		Debug.WriteLineIf(_useDetailedDebug, $"MapSectionRequestProcessor. Not adding: {mapSectionWorkItem.Request}, The MapSectionRequestProcessor's RequestQueue IsAddingComplete has been set.");
		//	}
		//}

		#endregion
	}
}
