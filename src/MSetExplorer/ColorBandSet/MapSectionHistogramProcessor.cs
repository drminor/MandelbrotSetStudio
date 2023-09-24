using MSS.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapSectionHistogramProcessor : IDisposable, IMapSectionHistogramProcessor
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

		public event EventHandler<PercentageBand[]>? PercentageBandsUpdated;
		public event EventHandler<HistogramUpdateType>? HistogramUpdated;

		public IHistogram Histogram => _histogram;

		public bool ProcessingEnabled
		{
			get
			{
				lock (_processingEnabledLock)
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

		//public double GetAverageTopValue() => _histogram.GetAverageMaxIndex();

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

		public void LoadHistogram(IEnumerable<IHistogram> histograms)
		{
			foreach (var histogram in histograms)
			{
				if (histogram.IsEmpty)
					continue;	

				_histogram.Add(histogram);
			}

			HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
		}

		public void Reset()
		{
			_histogram.Reset();
			HistogramUpdated?.Invoke(this, HistogramUpdateType.Clear);
		}

		public void Reset(int newSize)
		{
			_histogram.Reset(newSize);
			HistogramUpdated?.Invoke(this, HistogramUpdateType.Clear);
		}

		// TODO: Handle Long to Int conversion for GetKeyValuePairsForBand.
		public KeyValuePair<int, int>[] GetKeyValuePairsForBand(int previousCutoff, int cutoff, bool includeCatchAll)
		{
			var result = _histogram.GetKeyValuePairs().Where(x => x.Key >= previousCutoff && x.Key < cutoff).ToList();

			if (includeCatchAll && cutoff == _histogram.Length)
			{
				result.Add(new KeyValuePair<int, int>(cutoff + 1, (int)_histogram.UpperCatchAllValue));
			}

			return result.ToArray();
		}

		public IEnumerable<KeyValuePair<int, int>> GetKeyValuePairsForBand(int previousCutoff, int cutoff)
		{
			var result = _histogram.GetKeyValuePairs().Where(x => x.Key >= previousCutoff && x.Key < cutoff);

			return result;
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
					while (_workQueue.TryTake(out var currentWorkRequest, _waitDuration.Milliseconds, ct))
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
						switch (histogramWorkRequest.RequestType)
						{
							case HistogramWorkRequestType.Add:
								_histogram.Add(histogramWorkRequest.Histogram);
								HistogramUpdated?.Invoke(this, HistogramUpdateType.BlockAdded);
								break;
							case HistogramWorkRequestType.Remove:
								_histogram.Remove(histogramWorkRequest.Histogram);
								HistogramUpdated?.Invoke(this, HistogramUpdateType.BlockRemoved);
								break;
							case HistogramWorkRequestType.Refresh:
								HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
								break;
							default:
								Debug.WriteLine("WARNING: Unrecognized HistogramRequestType, using HistogramRequestType.Refresh.");
								HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
								break;
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
					PercentageBandsUpdated?.Invoke(this, newPercentages);
				}
			}
		}

		private PercentageBand[] BuildNewPercentages(int[] cutoffs, IHistogram histogram)
		{
			var pbList = cutoffs.Select(x => new PercentageBand(x)).ToList();
			pbList.Add(new PercentageBand(int.MaxValue));

			var bucketCnts = pbList.ToArray();

			var curBucketPtr = 0;
			var curBucketCut = cutoffs[curBucketPtr];

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

			for (; i < kvps.Length; i++)
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

			foreach (var pb in bucketCnts)
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
