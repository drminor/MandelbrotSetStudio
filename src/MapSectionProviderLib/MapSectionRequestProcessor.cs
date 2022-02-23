using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionRequestProcessor : IDisposable
	{
		private const int MAX_WORK_ITEMS = 4;

		private readonly object _lock = new();
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;
		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> _workQueue;

		private readonly Task _workQueueProcessor1;
		private readonly Task _workQueueProcessor2;
		private readonly Task _workQueueProcessor3;
		private readonly Task _workQueueProcessor4;

		private readonly List<int> _cancelledJobIds;

		private int _nextJobId;
		private bool disposedValue;

		#region Constructor

		public MapSectionRequestProcessor(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_nextJobId = 0;
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_dtoMapper = new DtoMapper();
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<WorkItem<MapSectionRequest, MapSectionResponse>> (MAX_WORK_ITEMS);
			_cancelledJobIds = new List<int>();

			if (mapSectionPersistProcessor != null)
			{
				_workQueueProcessor1 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
				_workQueueProcessor2 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
				_workQueueProcessor3 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
				_workQueueProcessor4 = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
			}
			else
			{
				_workQueueProcessor1 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
				_workQueueProcessor2 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
				_workQueueProcessor3 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
				_workQueueProcessor4 = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
			}
		}

		#endregion

		#region Public Methods

		public void AddWork(int jobNumber, MapSectionRequest mapSectionRequest, Action<MapSectionRequest, MapSectionResponse> workAction)
		{
			// TODO: Use a Lock to prevent update of IsAddingCompleted while we are in this method.
			if (!_workQueue.IsAddingCompleted)
			{
				var mapSectionWorkItem = new WorkItem<MapSectionRequest, MapSectionResponse>(jobNumber, mapSectionRequest, workAction);
				_workQueue.Add(mapSectionWorkItem);
			}
		}

		public void CancelJob(int jobId)
		{
			lock(_lock)
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
				_ = _workQueueProcessor1.Wait(120 * 1000);
				_ = _workQueueProcessor2.Wait(120 * 1000);
				_ = _workQueueProcessor3.Wait(120 * 1000);
				_ = _workQueueProcessor4.Wait(120 * 1000);
			}
			catch { }

			_mapSectionPersistProcessor?.Stop(immediately);
		}

		public int GetNextRequestId()
		{
			lock(_lock)
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
					MapSectionResponse mapSectionResponse;
					var workItem = _workQueue.Take(ct);

					if (IsJobCancelled(workItem.JobId))
					{
						mapSectionResponse = BuildEmptyResponse(workItem.Request);
					}
					else
					{
						var blockPositionDto = workItem.Request.BlockPosition;
 						var blockPosition = _dtoMapper.MapFrom(blockPositionDto);
						mapSectionResponse = await _mapSectionRepo.GetMapSectionAsync(workItem.Request.SubdivisionId, blockPosition);

						if (mapSectionResponse is null)
						{
							//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
							mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(workItem.Request);

							mapSectionPersistProcessor.AddWork(mapSectionResponse);
						}
					}

					workItem.WorkAction(workItem.Request, mapSectionResponse);
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

		// Without using the repository.
		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
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
			lock(_lock)
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

					if (_workQueueProcessor1 != null)
					{
						_workQueueProcessor1.Dispose();
					}

					if (_workQueueProcessor2 != null)
					{
						_workQueueProcessor2.Dispose();
					}

					if (_workQueueProcessor3 != null)
					{
						_workQueueProcessor3.Dispose();
					}

					if (_workQueueProcessor4 != null)
					{
						_workQueueProcessor4.Dispose();
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
