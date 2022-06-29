using MEngineDataContracts;
using MSS.Common.MSetRepo;
using MSS.Common.DataTransferObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MapSecWorkReqType = MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>;
using MapSecWorkGenType = MapSectionProviderLib.WorkItem<MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>, MEngineDataContracts.MapSectionResponse>;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		private const int NUMBER_OF_CONSUMERS = 2;
		private const int QUEUE_CAPACITY = 10; //200;

		private readonly IMapSectionAdapter? _mapSectionAdapter;
		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionGeneratorProcessor _mapSectionGeneratorProcessor;
		private readonly MapSectionResponseProcessor _mapSectionResponseProcessor;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSecWorkReqType> _workQueue;

		private readonly Task[] _workQueueProcessors;

		private readonly object _cancelledJobsLock = new();
		private readonly object _pendingRequestsLock = new();
		private readonly List<int> _cancelledJobIds;
		private readonly List<MapSecWorkReqType> _pendingRequests;

		private int _nextJobId;
		private bool disposedValue;

		private int _recordsUpdated;

		#region Constructor

		public MapSectionRequestProcessor(IMapSectionAdapter? mapSectionAdapter, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, MapSectionResponseProcessor mapSectionResponseProcessor)
		{
			_nextJobId = 0;
			_mapSectionAdapter = mapSectionAdapter;
			_dtoMapper = new DtoMapper();
			_mapSectionGeneratorProcessor = mapSectionGeneratorProcessor;
			_mapSectionResponseProcessor = mapSectionResponseProcessor;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSecWorkReqType>(QUEUE_CAPACITY);
			_pendingRequests = new List<MapSecWorkReqType>();
			_cancelledJobIds = new List<int>();

			_workQueueProcessors = new Task[NUMBER_OF_CONSUMERS];

			for (var i = 0; i < _workQueueProcessors.Length; i++)
			{
				_workQueueProcessors[i] = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionGeneratorProcessor, _cts.Token));
			}
		}

		#endregion

		#region Public Properties

		public int NumberOfRecordsUpdated => _recordsUpdated;

		#endregion

		#region Public Methods

		public void AddWork(int jobNumber, MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSectionResponse?> responseHandler) 
		{
			var mapSectionWorkItem = new MapSecWorkReqType(jobNumber, mapSectionRequest, responseHandler);

			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionWorkItem.Request}, Adding has been completed.");
			}
		}

		public IList<MapSectionRequest> GetPendingRequests(int jobNumber)
		{
			IList<MapSectionRequest> pendingRequestsCopy;

			lock (_pendingRequestsLock)
			{
				pendingRequestsCopy = new List<MapSectionRequest>(_pendingRequests.Where(x => x.JobId == jobNumber).Select(x => x.Request));
			}

			return pendingRequestsCopy;
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
				_mapSectionResponseProcessor.CancelJob(jobId);
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
					var mapSectionWorkItem = _workQueue.Take(ct);

					MapSectionResponse? mapSectionResponse;
					if (IsJobCancelled(mapSectionWorkItem.JobId))
					{
						mapSectionResponse = BuildEmptyResponse(mapSectionWorkItem.Request);
						mapSectionResponse.RequestCancelled = true;
					}
					else
					{
						mapSectionResponse = await FetchOrQueueForGenerationAsync(mapSectionWorkItem, mapSectionGeneratorProcessor, ct);
					}

					if (mapSectionResponse != null)
					{
						mapSectionWorkItem.Response = mapSectionResponse;
						_mapSectionResponseProcessor.AddWork(mapSectionWorkItem);
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

		private async Task<MapSectionResponse?> FetchOrQueueForGenerationAsync(MapSecWorkReqType mapSectionWorkItem, MapSectionGeneratorProcessor mapSectionGeneratorProcessor, CancellationToken ct)
		{
			if (CheckForMatchingAndAddToPending(mapSectionWorkItem))
			{
				// Don't have response now, but have added this request to another request that is in process.
				return null;
			}

			var mapSectionResponse = await FetchAsync(mapSectionWorkItem);

			if (mapSectionResponse == null)
			{
				QueueForGeneration(mapSectionWorkItem, mapSectionGeneratorProcessor);
				return null;
			}

			var requestedIterations = mapSectionWorkItem.Request.MapCalcSettings.TargetIterations;
			if (mapSectionResponse.MapCalcSettings.TargetIterations >= requestedIterations)
			{
				mapSectionWorkItem.Request.FoundInRepo = true;
				mapSectionWorkItem.Request.ProcessingEndTime = DateTime.UtcNow;
				return mapSectionResponse;
			}

			if (mapSectionResponse.Counts != null)
			{
				if (IsResponseComplete(mapSectionResponse, requestedIterations))
				{
					mapSectionWorkItem.Request.FoundInRepo = true;
					mapSectionWorkItem.Request.ProcessingEndTime = DateTime.UtcNow;
					return mapSectionResponse;
				}
				else
				{
					// Update the request with the values (in progress) retreived from the repository.
					var request = mapSectionWorkItem.Request;
					request.MapSectionId = mapSectionResponse.MapSectionId;
					request.IncreasingIterations = true;
					request.Counts = mapSectionResponse.Counts;
					request.EscapeVelocities = mapSectionResponse.EscapeVelocities;
					request.DoneFlags = mapSectionResponse.DoneFlags;
					request.ZValues = mapSectionResponse.ZValues;

					QueueForGeneration(mapSectionWorkItem, mapSectionGeneratorProcessor);
					return null;
				}
			}
			else
			{
				QueueForGeneration(mapSectionWorkItem, mapSectionGeneratorProcessor);
				return null;
			}
		}

		private bool IsResponseComplete(MapSectionResponse mapSectionResponse, int targetIterations)
		{
			for (var i = 0; i < mapSectionResponse.Counts.Length; i++)
			{
				if (!mapSectionResponse.DoneFlags[i] && mapSectionResponse.Counts[i] < targetIterations)
				{
					return false;
				}
			}

			return true;
		}

		private void QueueForGeneration(MapSecWorkReqType mapSectionWorkItem, MapSectionGeneratorProcessor mapSectionGeneratorProcessor)
		{
			lock (_pendingRequestsLock)
			{
				_pendingRequests.Add(mapSectionWorkItem);
			}

			var generatorWorkItem = new MapSecWorkGenType(mapSectionWorkItem.JobId, mapSectionWorkItem, HandleGeneratedResponse);
			mapSectionGeneratorProcessor.AddWork(generatorWorkItem);
		}

		private bool CheckForMatchingAndAddToPending(MapSecWorkReqType mapSectionWorkItem)
		{
			var pendingRequests = GetMatchingRequests(mapSectionWorkItem.Request);

			//Debug.WriteLine($"Checking for dups, the count is {pendingRequests.Count} for request: {mapSectionWorkItem.Request}.");

			if (pendingRequests.Count > 0)
			{
				//Debug.WriteLine($"Found a dup request, marking this one as pending.");

				lock (_pendingRequestsLock)
				{
					// There is already a request made for this same block, add our request to the queue
					mapSectionWorkItem.Request.Pending = true;
					_pendingRequests.Add(mapSectionWorkItem);
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		private async Task<MapSectionResponse?> FetchAsync(MapSecWorkReqType mapSectionWorkItem)
		{
			if (_mapSectionAdapter != null)
			{
				var mapSectionRequest = mapSectionWorkItem.Request;
				var mapSectionResponse = await _mapSectionAdapter.GetMapSectionAsync(mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition, mapSectionRequest.DontFetchZValuesFromRepo);

				if (mapSectionResponse?.JustNowUpdated == true)
				{
					_recordsUpdated++;

					//if (_recordsUpdated % 10 == 0)
					//{
					//	Debug.WriteLine($"{_recordsUpdated} records have been updated.");
					//}
				}

				return mapSectionResponse;
			}
			else
			{
				await Task.Delay(100);
				return null;
			}
		}

		private void HandleGeneratedResponse(MapSecWorkReqType mapSectionWorkItem, MapSectionResponse? mapSectionResponse)
		{
			// Set the original request's repsonse to the generated response.
			mapSectionWorkItem.Response = mapSectionResponse ?? BuildEmptyResponse(mapSectionWorkItem.Request);

			// Send the original request to the response processor.
			_mapSectionResponseProcessor.AddWork(mapSectionWorkItem);

			//if (IsJobCancelled(mapSectionWorkItem.JobId))
			//{
			//	Thread.Sleep(2 * 1000);
			//}

			var pendingRequests = GetPendingRequests(mapSectionWorkItem.Request);
			//Debug.WriteLine($"Handling generated response, the count is {pendingRequests.Count} for request: {mapSectionWorkItem.Request}");

			//Debug.Assert(RequestExists(mapSectionWorkItem.Request, pendingRequests), "The primary request was not included in the list of pending requests.");
			if (!RequestExists(mapSectionWorkItem.Request, pendingRequests))
			{
				Debug.WriteLine("The primary request was not included in the list of pending requests.");
			}

			foreach (var workItem in pendingRequests)
			{
				if (workItem != mapSectionWorkItem && !IsJobCancelled(workItem.JobId))
				{
					workItem.Response = mapSectionWorkItem.Response;
					_mapSectionResponseProcessor.AddWork(workItem);
				}
			}
		}

		// Exclude requests that were added to "piggy back" onto a "real" request.
		private List<MapSecWorkReqType> GetMatchingRequests(MapSectionRequest mapSectionRequest)
		{
			List<MapSecWorkReqType> result; // = new List<MapSecWorkReqType>();

			var subdivisionId = mapSectionRequest.SubdivisionId;
			var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);

			lock (_pendingRequestsLock)
			{
				result = _pendingRequests.Where(x => (!x.Request.Pending) && x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition).ToList();
			}

			return result;
		}

		// Find and remove all matching requests.
		private List<MapSecWorkReqType> GetPendingRequests(MapSectionRequest mapSectionRequest)
		{
			List<MapSecWorkReqType> result; // = new List<MapSecWorkReqType>();

			var subdivisionId = mapSectionRequest.SubdivisionId;
			var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			lock (_pendingRequestsLock)
			{
				result = _pendingRequests.Where(x => x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition).ToList();

				foreach (var workItem in result)
				{
					_pendingRequests.Remove(workItem);
				}
			}

			return result;
		}

		private bool RequestExists(MapSectionRequest mapSectionRequest, IEnumerable<MapSecWorkReqType> workItems)
		{
			var subdivisionId = mapSectionRequest.SubdivisionId;
			var blockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);

			var result = workItems.Any(x => x.Request.SubdivisionId == subdivisionId && _dtoMapper.MapFrom(x.Request.BlockPosition) == blockPosition);

			if (!result)
			{
				Debug.WriteLine("Here.");
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

		private MapSectionResponse BuildEmptyResponse(MapSectionRequest mapSectionRequest)
		{
			var result = new MapSectionResponse
			{
				MapSectionId = mapSectionRequest.MapSectionId,
				SubdivisionId = mapSectionRequest.SubdivisionId,
				BlockPosition = mapSectionRequest.BlockPosition,
				Counts = null,
				EscapeVelocities = null,
				DoneFlags = null,
				ZValues = null
			};

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
