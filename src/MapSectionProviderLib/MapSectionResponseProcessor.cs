using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using MapSecWorkReqType = MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>;

namespace MapSectionProviderLib
{
	public class MapSectionResponseProcessor : IDisposable
	{
		private const int QUEUE_CAPACITY = 200;
		private readonly object _cancelledJobsLock = new();

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSecWorkReqType> _workQueue;

		private readonly List<int> _cancelledJobIds;

		private Task _workQueueProcessor;
		private bool disposedValue;

		#region Constructor

		public MapSectionResponseProcessor()
		{
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSecWorkReqType>(QUEUE_CAPACITY);
			_cancelledJobIds = new List<int>();

			_workQueueProcessor = Task.Run(ProcessTheQueue);
		}

		#endregion

		#region Public Methods

		public void AddWork(MapSecWorkReqType mapSectionWorkItem)
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

			// Don't block the UI thread, since the task may be waiting for the UI thread to complete its work as it executes the RunWorkAction.
			_ = Task.Run(WaitForTheQueueProcessorToComplete);
		}

		private void WaitForTheQueueProcessorToComplete()
		{
			try
			{
				_ = _workQueueProcessor.Wait(10 * 1000);
				Debug.WriteLine("The MapSectionReponseProcesssor's WorkQueueProcessor Task has completed.");
			}
			catch (Exception e) 
			{
				Debug.WriteLine($"While Waiting for the processor to complete, the MapSectionResponseProcessor received exception: {e}.");
			}
		}

		#endregion

		#region Private Methods

		private void ProcessTheQueue()
		{
			var ct = _cts.Token;

			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkItem = _workQueue.Take(ct);

					if (mapSectionWorkItem.Response != null)
					{
						if (!mapSectionWorkItem.Response.RequestCancelled && IsJobCancelled(mapSectionWorkItem.JobId))
						{
							mapSectionWorkItem.Response.RequestCancelled = true;
						}

						mapSectionWorkItem.Request.Completed = true;
						mapSectionWorkItem.RunWorkAction();
					}
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

		#endregion

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					Stop(true);

					if (_cts != null)
					{
						_cts.Dispose();
					}

					if (_workQueue != null)
					{
						_workQueue.Dispose();
					}

					//if (_workQueueProcessor != null)
					//{
					//	_workQueueProcessor.Dispose();
					//}
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
