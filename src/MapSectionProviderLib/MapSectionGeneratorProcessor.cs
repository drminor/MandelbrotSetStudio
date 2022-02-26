using MEngineClient;
using MEngineDataContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using MapSecWorkReqType = MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>;

using MapSecWorkGenType = MapSectionProviderLib.WorkItem<MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>, MEngineDataContracts.MapSectionResponse>;


namespace MapSectionProviderLib
{
	public class MapSectionGeneratorProcessor : IDisposable
	{
		private const int NUMBER_OF_CONSUMERS = 4;
		private const int QUEUE_CAPACITY = 200;

		private readonly IMEngineClient _mEngineClient;

		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSecWorkGenType> _workQueue;

		private readonly Task[] _workQueueProcessors;

		private readonly object _cancelledJobsLock = new();
		private readonly List<int> _cancelledJobIds;

		private bool disposedValue;

		#region Constructor

		public MapSectionGeneratorProcessor(IMEngineClient mEngineClient, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_mEngineClient = mEngineClient;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;

			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<MapSecWorkGenType>(QUEUE_CAPACITY);
			_cancelledJobIds = new List<int>();

			_workQueueProcessors = new Task[NUMBER_OF_CONSUMERS];

			for (var i = 0; i < _workQueueProcessors.Length; i++)
			{
				_workQueueProcessors[i] = Task.Run(async () => await ProcessTheQueueAsync(_mapSectionPersistProcessor, _cts.Token));
			}
		}

		#endregion

		#region Public Methods

		public void AddWork(MapSecWorkGenType mapSectionWorkItem)
		{
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
			lock (_cancelledJobsLock)
			{
				if (!_cancelledJobIds.Contains(jobId))
				{
					_cancelledJobIds.Add(jobId);
				}
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
				}
			}
			catch { }

			_mapSectionPersistProcessor?.Stop(immediately);

		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(MapSectionPersistProcessor mapSectionPersistProcessor, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkItem = _workQueue.Take(ct);

					// The original request is in the Request's Request property.
					var mapSectionRequest = mapSectionWorkItem.Request.Request;

					MapSectionResponse mapSectionResponse;

					if (IsJobCancelled(mapSectionWorkItem.JobId))
					{
						mapSectionResponse = BuildEmptyResponse(mapSectionRequest);
					}
					else
					{
						//Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
						mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(mapSectionRequest);

						if (mapSectionPersistProcessor != null)
						{
							mapSectionPersistProcessor.AddWork(mapSectionResponse);
						}
					}

					mapSectionWorkItem.RunWorkAction(mapSectionResponse);
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The response queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The response queue got an exception: {e}.");
					throw;
				}
			}
		}

		private bool IsJobCancelled(int jobId)
		{
			bool result;
			lock (_cancelledJobsLock)
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
					// Dispose managed state (managed objects)
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

					for (var i = 0; i < _workQueueProcessors.Length; i++)
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
