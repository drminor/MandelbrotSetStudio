using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionResponseProcessor : IDisposable
	{
		private const int QUEUE_CAPACITY = 200;
		private readonly object _cancelledJobsLock = new();

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionWorkRequest> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;
		private bool _isStopped;

		#region Constructor

		public MapSectionResponseProcessor()
		{
			_isStopped = false;
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSectionWorkRequest>(QUEUE_CAPACITY);
			//_cancelledJobIds = new List<int>();

			_workQueueProcessor = Task.Run(() => { ProcessTheQueue(_cts.Token); });
		}

		#endregion

		#region Public Methods

		internal void AddWork(MapSectionWorkRequest mapSectionWorkItem)
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

		public void Stop(bool immediately)
		{
			lock (_cancelledJobsLock)
			{
				if (_isStopped)
				{
					return;
				}

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

				_isStopped = true;
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

		private void ProcessTheQueue(CancellationToken ct)
		{
			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkRequest = _workQueue.Take(ct);

					mapSectionWorkRequest.Request.Completed = true;
					mapSectionWorkRequest.RunWorkAction();
					if (!mapSectionWorkRequest.Request.ProcessingEndTime.HasValue)
					{
						mapSectionWorkRequest.Request.ProcessingEndTime = DateTime.UtcNow;
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
