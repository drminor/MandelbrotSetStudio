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
					Tuple<MapSection, MapSection?>? mapSectionPair = FetchOrQueueForProcessing(mapSectionWorkRequest, ct);

					if (mapSectionPair != null)
					{
						if (!mapSectionRequest.IsCancelled)
						{
							mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
							result.Add(mapSectionPair.Item1);
							msrJob.SectionsFoundInRepo++;
						}

						if (mapSectionPair.Item2 != null)
						{
							var mirror = mapSectionRequest.Mirror;

							//if (mirror == null)
							//{
							//	throw new InvalidOperationException("MapSectionRequestProcessor:SubmitRequestsReceiving two MapSections, but the request has no mirror.");
							//}

							if (mirror != null && !mirror.IsCancelled)
							{
								mirror.ProcessingStartTime = DateTime.UtcNow;
								result.Add(mapSectionPair.Item2);
								msrJob.SectionsFoundInRepo++;
							}
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

		private Tuple<MapSection, MapSection?>? FetchOrQueueForProcessing(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
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
					var result = CreateTheMapSections(mapSectionBytes, request, ct);
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
				if (mapSectionWorkRequest.Request.NeitherRequestNorMirrorIsInPlay)
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

					if (jobIsCancelled || mapSectionRequest.NeitherRequestNorMirrorIsInPlay)
					{
						var msg = $"MapSectionRequestProcessor: QueueProcessor:{queueProcessorIndex} is skipping request with JobId/Request#: {mapSectionRequest.MapLoaderJobNumber}/{mapSectionRequest.RequestNumber}.";
						msg += jobIsCancelled ? " JobIsCancelled" : "MapSectionRequest's Cancellation Token is cancelled.";
						Debug.WriteLineIf(_useDetailedDebug, msg);

						//mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
						//SendToResponseQueue(mapSectionWorkRequest, ct);

						//var mirror = mapSectionWorkRequest.Request.Mirror;

						//if (mirror != null)
						//{
						//	var mapSectionForMirror = _mapSectionBuilder.CreateEmptyMapSection(mirror, isCancelled: true);
						//	var mapSectionRequestForMirror = new MapSectionWorkRequest(mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
						//	SendToResponseQueue(mapSectionRequestForMirror, ct);
						//}
					}
					else
					{
						//if (mapSectionWorkRequest.Request.Cancelled)
						//{
						//	mapSectionWorkRequest.Response = _mapSectionBuilder.CreateEmptyMapSection(mapSectionRequest, isCancelled: true);
						//	SendToResponseQueue(mapSectionWorkRequest, ct);
						//}
						//else
						//{
						//	var mirror = mapSectionWorkRequest.Request.Mirror;

						//	if (mirror != null && mirror.Cancelled)
						//	{
						//		var mapSectionForMirror = _mapSectionBuilder.CreateEmptyMapSection(mirror, isCancelled: true);
						//		var mapSectionRequestForMirror = new MapSectionWorkRequest(mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
						//		SendToResponseQueue(mapSectionRequestForMirror, ct);
						//	}
						//}

						if (!UseRepo)
						{
							QueueForGeneration(mapSectionWorkRequest, queueProcessorIndex);
						}
						else
						{
							Tuple<MapSection, MapSection?>? mapSectionPair = FetchOrQueueForGeneration(mapSectionWorkRequest, queueProcessorIndex, ct);

							if (mapSectionPair != null)
							{
								if (!mapSectionWorkRequest.Request.IsCancelled)
								{
									mapSectionWorkRequest.Response = mapSectionPair.Item1;
									SendToResponseQueue(mapSectionWorkRequest, ct);
								}

								if (mapSectionPair.Item2 != null)
								{
									var mirror = mapSectionWorkRequest.Request.Mirror;
									if (mirror == null)
									{
										throw new InvalidOperationException("Receiving two MapSections, but the request has no mirror.");
									}

									if (!mirror.IsCancelled)
									{
										var mapSectionRequestForMirror = new MapSectionWorkRequest(mirror, mapSectionWorkRequest.WorkAction, mapSectionPair.Item2);
										SendToResponseQueue(mapSectionRequestForMirror, ct);
									}
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

		private Tuple<MapSection, MapSection?>? FetchOrQueueForGeneration(MapSectionWorkRequest mapSectionWorkRequest, int queueProcessorIndex, CancellationToken ct)
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

						var result = CreateTheMapSections(mapSectionBytes, request, ct);
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

		private Tuple<MapSection, MapSection?>? CreateTheMapSections(MapSectionBytes mapSectionBytes, MapSectionRequest request, CancellationToken ct)
		{
			var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
			mapSectionVectors.Load(mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);

			var mapSection1 = CreateMapSection(request, mapSectionVectors, ct);

			if (request.Mirror != null) request.Mirror.MapSectionId = request.MapSectionId;

			var mapSection2 = request.Mirror == null ? null : CreateMapSection(request.Mirror, mapSectionVectors, ct);

			if (mapSection1 != null && mapSection2 != null)
			{
				// We're using the same MapSectionVectors as does the first mapSection
				mapSectionVectors.IncreaseRefCount();
				return new Tuple<MapSection, MapSection?>(mapSection1, mapSection2);
			}
			else
			{
				var combo = mapSection1 ?? mapSection2;
				if (combo != null)
				{
					return new Tuple<MapSection, MapSection?>(combo, null);
				}
				else
				{
					return null;
				}
			}
		}

		private MapSection? CreateMapSection(MapSectionRequest request, MapSectionVectors mapSectionVectors, CancellationToken ct)
		{
			MapSection? result;

			if (!request.IsCancelled)
			{
				request.FoundInRepo = true;
				request.ProcessingEndTime = DateTime.UtcNow;

				PersistJobMapSectionRecord(request, ct);
				result = _mapSectionBuilder.CreateMapSection(request, mapSectionVectors);
			}
			else
			{
				result = null;
			}

			return result;
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

				//var mapSectionVectors2 = new MapSectionVectors2(request.BlockSize, mapSectionBytes.Counts, mapSectionBytes.EscapeVelocities);
				//request.MapSectionVectors2 = mapSectionVectors2;
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

			if (mapSectionWorkRequest.Request.NeitherRequestNorMirrorIsInPlay)
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

				var mapSectionRequestForMirror = new MapSectionWorkRequest(mirror, mapSectionWorkRequest.WorkAction, mapSectionForMirror);
				SendToResponseQueue(mapSectionRequestForMirror, ct);
			}

			// Only send the first until we've had a chance to build the second 
			SendToResponseQueue(mapSectionWorkRequest, ct);

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

		private void SendToResponseQueue(MapSectionWorkRequest mapSectionWorkRequest, CancellationToken ct)
		{
			if (mapSectionWorkRequest.Request.NeitherRequestNorMirrorIsInPlay)
			{
				Debug.WriteLine("Sending to the Response Processor a Cancelled request.");
			}
			_mapSectionResponseProcessor.AddWork(mapSectionWorkRequest, ct);
		}

		private void SendToPersistQueue(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, CancellationToken ct)
		{
			CheckRequestResponseBeforePersist(mapSectionRequest, mapSectionResponse);

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
