using MSS.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using HistogramWorkReqType = MapSectionProviderLib.WorkItem<MSS.Types.MapSection, System.Collections.Generic.IList<double>>;

namespace MSetExplorer
{
	public class MapSectionHistogramProcessor : IDisposable
	{
		private readonly IHistogram _histogram;

		private const int QUEUE_CAPACITY = 200;
		private readonly object _cancelledJobsLock = new();

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<HistogramWorkReqType> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		#region Constructor

		public MapSectionHistogramProcessor(IHistogram histogram)
		{
			_histogram = histogram;
			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<HistogramWorkReqType>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(ProcessTheQueue);
		}

		#endregion

		#region Public Methods

		public void AddWork(bool isAddOperation, MapSection mapSection, Action<MapSection, IList<double>> responseHandler)
		{
			var jb = isAddOperation ? 1 : 0;
			var mapSectionWorkItem = new HistogramWorkReqType(jb, mapSection, responseHandler);
			
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
				_workQueueProcessor.Wait(120 * 1000);
			}
			catch
			{ }
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

					if (mapSectionWorkItem.JobId == 1)
					{
						_histogram.Add(mapSectionWorkItem.Request.Histogram);
					}
					else
					{
						_histogram.Remove(mapSectionWorkItem.Request.Histogram);
					}

					var response = new List<double> { 0.1, 0.2 };

					mapSectionWorkItem.RunWorkAction(response);
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

					if (_workQueueProcessor != null)
					{
						_workQueueProcessor.Dispose();
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
