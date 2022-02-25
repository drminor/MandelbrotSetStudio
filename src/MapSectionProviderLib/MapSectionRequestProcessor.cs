using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types.DataTransferObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MapSecWorkReqType = MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		private const int NUMBER_OF_CONSUMERS = 4;
		private const int QUEUE_CAPACITY = 15;

		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;
		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;
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

		#region Constructor

		public MapSectionRequestProcessor(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo, MapSectionPersistProcessor mapSectionPersistProcessor, 
			MapSectionResponseProcessor mapSectionResponseProcessor)
		{
			_nextJobId = 0;
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_dtoMapper = new DtoMapper();
			_mapSectionPersistProcessor = mapSectionPersistProcessor;
			_mapSectionResponseProcessor = mapSectionResponseProcessor;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSecWorkReqType>(QUEUE_CAPACITY);
			_pendingRequests = new List<MapSecWorkReqType>();
			_cancelledJobIds = new List<int>();

			_workQueueProcessors = new Task[NUMBER_OF_CONSUMERS];

			if (mapSectionPersistProcessor != null)
			{
				for (var i = 0; i < _workQueueProcessors.Length; i++)
				{
					_workQueueProcessors[i] = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
				}
			}
			else
			{
				for (var i = 0; i < _workQueueProcessors.Length; i++)
				{
					_workQueueProcessors[i] = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
				}
			}
		}

		#endregion

		#region Public Methods

		public IList<MapSecWorkReqType> GetPendingRequests()
		{
			IList<MapSecWorkReqType> pendingRequestsCopy;

			lock (_pendingRequestsLock)
			{
				pendingRequestsCopy = new List<MapSecWorkReqType>(_pendingRequests);
			}

			return pendingRequestsCopy;
		}

		public void AddWork(MapSecWorkReqType mapSectionWorkItem)
		{
			// TODO: Use a Lock to prevent update of IsAddingCompleted while we are in this method.
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionWorkItem.Request}, Adding has been completed.");
			}
		}

		public void CancelJob(int jobId)
		{
			lock(_cancelledJobsLock)
			{
				if (!_cancelledJobIds.Contains(jobId))
				{
					_cancelledJobIds.Add(jobId);
				}
			}
		}

		public void Stop(bool immediately)
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

			try
			{
				for (var i = 0; i < _workQueueProcessors.Length; i++)
				{
					_ = _workQueueProcessors[i].Wait(120 * 1000);
				}
			}
			catch { }

			_mapSectionPersistProcessor?.Stop(immediately);
		}

		public int GetNextRequestId()
		{
			// TODO: Consider using a lock object just dedicated to the GetNextRequestId method of the MapSectionRequestProcessor class.
			lock(_cancelledJobsLock)
			{
				var nextJobId = _nextJobId++;
			}

			return _nextJobId;
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(MapSectionPersistProcessor mapSectionPersistProcessor, CancellationToken ct)
		{
			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkItem = _workQueue.Take(ct);
					var mapSectionResponse = await FetchOrGenerateAsync(mapSectionWorkItem, mapSectionPersistProcessor, ct);

					if (mapSectionResponse != null)
					{
						mapSectionWorkItem.Response = mapSectionResponse;
						HandleFoundResponse(mapSectionWorkItem);
					}
					else
					{
						Debug.WriteLine($"FetchOrGenerateAsync returned null.");
					}
				}
				catch (OperationCanceledException)
				{
					Debug.WriteLine("The work queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The work queue got an exception: {e}.");
					throw;
				}
			}
		}
		private async Task<MapSectionResponse> FetchOrGenerateAsync(MapSecWorkReqType mapSectionWorkItem, MapSectionPersistProcessor mapSectionPersistProcessor, CancellationToken ct)
		{
			if (IsJobCancelled(mapSectionWorkItem.JobId))
			{
				return BuildEmptyResponse(mapSectionWorkItem.Request);
			}

			var mapSectionResponse = await FetchAsync(mapSectionWorkItem);
			if (mapSectionResponse != null)
			{
				// Response was found in the repository.
				mapSectionWorkItem.Request.FoundInRepo = true;
				return mapSectionResponse;
			}

			if (mapSectionWorkItem.Request.Pending)
			{
				// Don't have response now, but have added this request to another request that is in process.
				return mapSectionResponse;
			}

			//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
			mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(mapSectionWorkItem.Request);

			//var saveWorkItem = new WorkItem<MapSectionResponse, string>(mapSectionWorkItem.JobId, mapSectionResponse, HandleResponseSaved);
			mapSectionPersistProcessor.AddWork(mapSectionResponse);

			return mapSectionResponse;
		}

		private async Task<MapSectionResponse> FetchAsync(MapSecWorkReqType mapSectionWorkItem)
		{
			var mapSectionRequest = mapSectionWorkItem.Request;
			var pendingRequests = GetPendingRequests(mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition, removeFoundItems: false);

			//Debug.WriteLine($"Checking for dups, the count is {pendingRequests.Count} for request: {mapSectionWorkItem.Request}.");

			if (pendingRequests.Count > 0)
			{
				Debug.WriteLine($"Found a dup request, marking this one as pending.");

				// There is already a request made for this same block, add our request to the queue
				mapSectionRequest.Pending = true;
				_pendingRequests.Add(mapSectionWorkItem);

				return null;
			}
			else
			{
				var mapSectionResponse = await _mapSectionRepo.GetMapSectionAsync(mapSectionRequest.SubdivisionId, _dtoMapper.MapFrom(mapSectionRequest.BlockPosition));

				//_pendingRequests.Add(mapSectionWorkItem);
				if (mapSectionResponse is null)
				{
					_pendingRequests.Add(mapSectionWorkItem);
				}

				return mapSectionResponse;
			}
		}

		private void HandleFoundResponse(MapSecWorkReqType mapSectionWorkItem)
		{
			var mapSectionRequest = mapSectionWorkItem.Request;
			var pendingRequests = GetPendingRequests(mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition, removeFoundItems: true);

			//Debug.WriteLine($"Handling response, the count is {pendingRequests.Count} for request: {mapSectionWorkItem.Request}");

			if (!IsJobCancelled(mapSectionWorkItem.JobId))
			{
				mapSectionWorkItem.Request.Completed = true;
				_mapSectionResponseProcessor.AddWork(mapSectionWorkItem);
			}

			foreach (var workItem in pendingRequests)
			{
				if (workItem != mapSectionWorkItem && !IsJobCancelled(workItem.JobId))
				{
					workItem.Response = mapSectionWorkItem.Response;
					workItem.Request.Completed = true;
					_mapSectionResponseProcessor.AddWork(workItem);
				}
			}
		}

		//private void HandleResponseSaved(MapSectionResponse mapSectionResponse, string mapSectionId)
		//{
		//	//lock (_pendingRequestsLock)
		//	//{
		//	//	var pendingRequests = GetPendingRequests(mapSectionResponse.SubdivisionId, mapSectionResponse.BlockPosition);
		//	//	Debug.WriteLine($"Handling Response Saved, found {pendingRequests.Count}.");

		//	//	foreach (var workItem in pendingRequests)
		//	//	{
		//	//		if (!_pendingRequests.Remove(workItem))
		//	//		{
		//	//			Debug.WriteLine("Could not remove a pending request -- Handling Response Saved.");
		//	//		}
		//	//	}
		//	//}
		//}

		private List<MapSecWorkReqType> GetPendingRequests(string subdivisionId, BigVectorDto blockPositionDto, bool removeFoundItems)
		{
			List<MapSecWorkReqType> result = new List<MapSecWorkReqType>();

			var blockPosition = _dtoMapper.MapFrom(blockPositionDto);
			lock (_pendingRequestsLock)
			{
				foreach (var workItem in _pendingRequests)
				{
					var bp = _dtoMapper.MapFrom(workItem.Request.BlockPosition);
					if (bp.X == blockPosition.X && bp.Y == blockPosition.Y && workItem.Request.SubdivisionId == subdivisionId)
					{
						result.Add(workItem);
					}
				}

				if (removeFoundItems)
				{
					foreach(var workItem in result)
					{
						_pendingRequests.Remove(workItem);
					}
				}
			}

			return result;
		}

		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			// Does not use a Persist Processor

			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					MapSectionResponse mapSectionResponse;
					var workItem = _workQueue.Take(ct);

					if (IsJobCancelled(workItem.JobId))
					{
						mapSectionResponse = BuildEmptyResponse(workItem.Request);
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {workItem.Request.BlockPosition}.");
						mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(workItem.Request);
					}

					workItem.WorkAction(workItem.Request, mapSectionResponse);
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("The work queue got a TCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The work queue got an exception: {e}.");
					throw;
				}
			}
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
				Counts = null
			};

			return result;
		}

		#endregion

		#region IDispoable Support

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

					for(var i = 0; i < _workQueueProcessors.Length; i++)
					{
						if (_workQueueProcessors[i] != null)
						{
							_workQueueProcessors[i].Dispose();
						}
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
