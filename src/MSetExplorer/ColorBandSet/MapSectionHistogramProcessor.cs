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

		private bool disposedValue;

		#region Constructor

		public MapSectionHistogramProcessor(IHistogram histogram)
		{
			_histogram = histogram;
			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<HistogramWorkRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(ProcessTheQueue);
			_waitDuration = TimeSpan.FromMilliseconds(100);
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

			HistogramWorkRequest? lastWorkRequest = null;

			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					while(_workQueue.TryTake(out var currentWorkRequest, _waitDuration.Milliseconds, ct))
					{
						lastWorkRequest = DoWorkRequest(currentWorkRequest);
					}

					if (lastWorkRequest != null)
					{
						CalculateAndPostPercentages(lastWorkRequest);
					}

					var currentWorkRequest1 = _workQueue.Take(ct);
					lastWorkRequest = DoWorkRequest(currentWorkRequest1);
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
				if (_processingEnabled)
				{
					if (histogramWorkRequest.Histogram != null)
					{
						if (histogramWorkRequest.RequestType == HistogramWorkRequestType.Add)
						{
							_histogram.Add(histogramWorkRequest.Histogram);
						}
						else if (histogramWorkRequest.RequestType == HistogramWorkRequestType.Remove)
						{
							_histogram.Remove(histogramWorkRequest.Histogram);
						}
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
					var newPercentages = BuildNewPercentages(histogramWorkRequest.Cutoffs, _histogram);
					histogramWorkRequest.RunWorkAction(newPercentages);
				}
			}
		}

		private PercentageBand[] BuildNewPercentages(int[] cutOffs, IHistogram histogram)
		{
			var pbList = cutOffs.Select(x => new PercentageBand(x)).ToList();
			pbList.Add(new PercentageBand(int.MaxValue));

			var bucketCnts = pbList.ToArray();

			var curBucketPtr = 0;
			var curBucketCut = cutOffs[curBucketPtr];

			long runningSum = 0;

			var kvps = histogram.GetKeyValuePairs();

			var i = 0;

			for (; i < kvps.Length && curBucketPtr < bucketCnts.Length; i++)
			{
				var idx = kvps[i].Key;
				var amount = kvps[i].Value;

				while (curBucketPtr < bucketCnts.Length && idx > curBucketCut)
				{
					curBucketPtr++;
					curBucketCut = bucketCnts[curBucketPtr].Cutoff;
				}

				runningSum += amount;

				if (idx == curBucketCut)
				{
					bucketCnts[curBucketPtr].ExactCount = amount;
				}

				bucketCnts[curBucketPtr].Count += amount;
				bucketCnts[curBucketPtr].RunningSum = runningSum;
			}

			for(; i < kvps.Length; i++)
			{
				var amount = kvps[i].Value;
				runningSum += amount;

				bucketCnts[^1].Count += amount;
				bucketCnts[^1].RunningSum = runningSum;
			}

			runningSum += histogram.UpperCatchAllValue;
			bucketCnts[^1].Count += histogram.UpperCatchAllValue;
			bucketCnts[^1].RunningSum = runningSum;

			// For now, include all of the cnts above the target in the last bucket.
			bucketCnts[^2].Count += bucketCnts[^1].Count;

			//var total = (double)histogram.Values.Select(x => Convert.ToInt64(x)).Sum();
			var total = (double)runningSum;

			foreach(var pb in bucketCnts)
			{
				pb.Percentage = Math.Round(100 * (pb.Count / total), 2);
			}

			return bucketCnts;
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
