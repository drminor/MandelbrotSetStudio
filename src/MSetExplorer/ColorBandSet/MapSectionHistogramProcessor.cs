using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
		private readonly BlockingCollection<HistogramBlockRequest> _workQueue;

		private readonly Task _workQueueProcessor;

		private readonly TimeSpan _waitDuration;

		private readonly ObservableCollection<MapSection> _mapSections;

		private readonly HistogramD _topValues;
		//private double _averageMapSectionTargetIteration;



		private bool disposedValue;

		#region Constructor

		public MapSectionHistogramProcessor(IHistogram histogram, ObservableCollection<MapSection> mapSections)
		{
			_mapSections = mapSections;
			_histogram = histogram;
			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<HistogramBlockRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(ProcessTheQueue);
			_waitDuration = TimeSpan.FromMilliseconds(100);

			_topValues = new HistogramD();
			//_averageMapSectionTargetIteration = 0;

			_mapSections.CollectionChanged += MapSections_CollectionChanged; 
		}


		#endregion

		#region Public Events

		public event EventHandler<PercentageBand[]>? PercentageBandsUpdated;
		public event EventHandler<HistogramUpdateType>? HistogramUpdated;

		#endregion

		#region Public Properties

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

		public void AddWork(HistogramBlockRequest histogramWorkRequest)
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
			_topValues.Clear();

			HistogramUpdated?.Invoke(this, HistogramUpdateType.Clear);
		}

		public void Reset(int newSize)
		{
			_histogram.Reset(newSize);
			_topValues.Clear();

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

			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					// Block waiting for new work.
					HistogramBlockRequest? workRequest = _workQueue.Take(ct);
					DoWorkRequest(workRequest);

					// Process the queue as long as new items are available.
					while (_workQueue.TryTake(out workRequest, _waitDuration.Milliseconds, ct))
					{
						DoWorkRequest(workRequest);
					}

					// No new items availble in the last _waitDuration.Milliseconds,
					// raise the Refresh event to let our subscribers know that the Histogram has been updated.
					HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
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

		private bool DoWorkRequest(HistogramBlockRequest histogramWorkRequest)
		{
			bool result;

			lock (_processingEnabledLock)
			{
				if (_processingEnabled && histogramWorkRequest.Histogram != null)
				{
					result = true;

					if (histogramWorkRequest.RequestType == HistogramBlockRequestType.Add)
					{
						_histogram.Add(histogramWorkRequest.Histogram);
					}
					else if (histogramWorkRequest.RequestType == HistogramBlockRequestType.Remove)
					{
						_histogram.Remove(histogramWorkRequest.Histogram);
					}
					else
					{
						throw new InvalidOperationException($"The {histogramWorkRequest.RequestType} is not recognized or is not supported.");
					}
				}
				else
				{
					result = false;
				}
			}

			return result;
		}

		private void MapSections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			//if (_colorBandSet != null && _colorBandSet.Count == 0)
			//{
			//	return;
			//}

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				//_histogram.Reset();
				Reset();
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					AddWork(new HistogramBlockRequest(HistogramBlockRequestType.Add, mapSection.Histogram));
					_topValues.Increment(mapSection.TargetIterations);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.OldItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					AddWork(new HistogramBlockRequest(HistogramBlockRequestType.Remove, mapSection.Histogram));
					_topValues.Decrement(mapSection.TargetIterations);
				}
			}

			//Debug.WriteLine($"There are {Histogram[Histogram.UpperBound - 1]} points that reached the target iterations.");
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
