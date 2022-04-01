using MSS.Types;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapSectionHistogramProcessor : IDisposable
	{
		private readonly IHistogram _histogram;

		private const int QUEUE_CAPACITY = 200;

		private bool _processingEnabled;
		private readonly object _processingEnabledLock = new();

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<HistogramWorkRequest> _workQueue;

		private readonly Task _workQueueProcessor;

		private readonly TimeSpan _waitDuration;
		private HistogramWorkRequest? _lastWorkRequest;

		private bool disposedValue;

		#region Constructor

		public MapSectionHistogramProcessor(IHistogram histogram)
		{
			_histogram = histogram;
			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<HistogramWorkRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(ProcessTheQueue);
			_waitDuration = TimeSpan.FromMilliseconds(100);
			_lastWorkRequest = null;
		}

		#endregion

		#region Public Properties

		public bool ProcessingEnabled
		{
			get
			{
				lock(_processingEnabledLock)
				{
					return _processingEnabled;
				}
			}

			set
			{
				lock (_processingEnabledLock)
				{
					_processingEnabled = value;
				}

			}
		}

		#endregion

		#region Public Methods

		public void AddWork(HistogramWorkRequest histogramWorkRequest)
		{
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(histogramWorkRequest);
			}
			else
			{
				Debug.WriteLine($"Not adding: {histogramWorkRequest}, Adding has been completed.");
			}
		}

		public void Stop(bool immediately)
		{
			lock (_processingEnabledLock)
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
					HistogramWorkRequest? currentWorkRequest = null;
					HistogramWorkRequest? lastWorkRequest = null;

					while(_workQueue.TryTake(out currentWorkRequest, _waitDuration.Milliseconds, ct))
					{
						lastWorkRequest = DoWorkRequest(currentWorkRequest);
					}

					if (lastWorkRequest != null)
					{
						CalculateAndPostPercentages(lastWorkRequest);
					}

					currentWorkRequest = _workQueue.Take(ct);
					lastWorkRequest = DoWorkRequest(currentWorkRequest);
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

		private HistogramWorkRequest? DoWorkRequest(HistogramWorkRequest histogramWorkRequest)
		{
			HistogramWorkRequest? result;

			lock (_processingEnabledLock)
			{
				if (_processingEnabled && histogramWorkRequest.Histogram != null)
				{
					if (histogramWorkRequest.RequestType == HistogramWorkRequestType.Add)
					{
						_histogram.Add(histogramWorkRequest.Histogram);
					}
					else if (histogramWorkRequest.RequestType == HistogramWorkRequestType.Remove)
					{
						_histogram.Remove(histogramWorkRequest.Histogram);
					}

					result = histogramWorkRequest;
				}
				else
				{
					result = null;
				}
			}

			return result;
		}

		private void CalculateAndPostPercentages(HistogramWorkRequest histogramWorkRequest)
		{
			lock (_processingEnabledLock)
			{
				if (_processingEnabled)
				{
					var newPercentages = BuildNewPercentages(histogramWorkRequest.CutOffs, _histogram);
					histogramWorkRequest.RunWorkAction(newPercentages);
				}
			}
		}

		private ValueTuple<int, double>[] BuildNewPercentages(int[] cutOffs, IHistogram histogram)
		{
			var bucketCnts = new long[cutOffs.Length];
			var curBucketPtr = 0;
			var curBucketCut = cutOffs[curBucketPtr];

			var kvps = histogram.GetKeyValuePairs();
			for (var i = 0; i < kvps.Length; i++)
			{
				var idx = kvps[i].Key;
				var amount = kvps[i].Value;

				while (curBucketPtr < cutOffs.Length - 1 && idx >= curBucketCut)
				{
					curBucketPtr++;
					curBucketCut = cutOffs[curBucketPtr];
				}

				bucketCnts[curBucketPtr] += amount;
			}

			var total = (double)histogram.Values.Select(x => Convert.ToInt64(x)).Sum();
			var newPercentages = bucketCnts.Select((x, i) => new ValueTuple<int, double>(cutOffs[i], Math.Round(100 * (x / total), 2))).ToArray();

			return newPercentages;
		}

		//private ValueTuple<int, double>[] BuildNewPercentagesOld(int[] cutOffs, IHistogram histogram)
		//{
		//	int[] cuts = new int[cutOffs.Length + 1];

		//	Array.Copy(cutOffs, cuts, cutOffs.Length);
		//	cuts[cuts.Length - 1] = int.MaxValue;

		//	var bucketCnts = new long[cutOffs.Length + 1];
		//	var curBucketPtr = 0;
		//	var curBucketCut = cuts[curBucketPtr];

		//	var kvps = histogram.GetKeyValuePairs();
		//	for (var i = 0; i < kvps.Length; i++)
		//	{
		//		var idx = kvps[i].Key;
		//		var amount = kvps[i].Value;

		//		while (curBucketPtr < cuts.Length && idx >= curBucketCut)
		//		{
		//			curBucketPtr++;
		//			curBucketCut = cuts[curBucketPtr];
		//		}

		//		bucketCnts[curBucketPtr] += amount;
		//	}

		//	var total = (double)histogram.Values.Select(x => Convert.ToInt64(x)).Sum();
		//	var newPercentages = bucketCnts.Select((x, i) => new ValueTuple<int, double>(cuts[i], Math.Round(100 * (x / total), 2))).ToArray();

		//	return newPercentages;
		//}

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
