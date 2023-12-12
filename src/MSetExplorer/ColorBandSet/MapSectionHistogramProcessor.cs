﻿using MSS.Types;
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
		private const int QUEUE_CAPACITY = 200;
		private const int WAIT_FOR_MAPSECTION_INTERVAL_MS = 500;

		private readonly IHistogram _histogram;
		private readonly ObservableCollection<MapSection> _mapSections;

		private bool _processingEnabled;
		private readonly object _processingEnabledLock;

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<HistogramBlockRequest> _workQueue;

		private readonly Task _workQueueProcessor;
		private readonly TimeSpan _waitDuration;

		//private readonly HistogramD _topValues;
		//private double _averageMapSectionTargetIteration;

		private bool disposedValue;

		#region Constructor

		public MapSectionHistogramProcessor(IHistogram histogram, ObservableCollection<MapSection> mapSections)
		{
			_histogram = histogram;
			_mapSections = mapSections;

			_processingEnabled = true;
			_processingEnabledLock = new object();
			_cts = new CancellationTokenSource();
			_workQueue = new BlockingCollection<HistogramBlockRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(ProcessTheQueue);
			_waitDuration = TimeSpan.FromMilliseconds(WAIT_FOR_MAPSECTION_INTERVAL_MS);

			//_topValues = new HistogramD();

			_mapSections.CollectionChanged += MapSections_CollectionChanged; 
		}

		#endregion

		#region Public Events

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

		//public void LoadHistogram(IEnumerable<IHistogram> histograms)
		//{
		//	foreach (var histogram in histograms)
		//	{
		//		if (histogram.IsEmpty)
		//			continue;	

		//		_histogram.Add(histogram);
		//	}

		//	HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
		//}

		public void Reset()
		{
			_histogram.Reset();
			//_topValues.Clear();

			HistogramUpdated?.Invoke(this, HistogramUpdateType.Clear);
		}

		public void Reset(int newSize)
		{
			_processingEnabled = false;

			_histogram.Reset(newSize);
			//_topValues.Clear();

			foreach(var mapsection in _mapSections)
			{
				if (!mapsection.Histogram.IsEmpty)
				{
					_histogram.Add(mapsection.Histogram);
				}
			}

			_processingEnabled = true;

			HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
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

		//public double GetAverageTopValue() => _histogram.GetAverageMaxIndex();

		// TODO: Have the MapSectionHistogramProcessor Cache the value of AverageMapSectionTargetIteration.
		public double GetAverageMapSectionTargetIteration()
		{
			// _topValues.Clear();
			var topValues = new HistogramD();

			foreach(var ms in _mapSections)
			{
				topValues.Increment(ms.TargetIterations);
			}

			var result = topValues.GetAverage();

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
					var haveWork = HandleRequest(workRequest);

					// Process the queue as long as new items are available.
					while (_workQueue.TryTake(out workRequest, _waitDuration.Milliseconds, ct))
					{
						haveWork |= HandleRequest(workRequest);
					}

					// No new items availble in the last _waitDuration.Milliseconds,

					if (haveWork)
					{
						// Raise the Refresh event to let our subscribers know that the Histogram has been updated.
						HistogramUpdated?.Invoke(this, HistogramUpdateType.Refresh);
						haveWork = false;
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

		private bool HandleRequest(HistogramBlockRequest histogramWorkRequest)
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
					//_topValues.Increment(mapSection.TargetIterations);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.OldItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					AddWork(new HistogramBlockRequest(HistogramBlockRequestType.Remove, mapSection.Histogram));
					//_topValues.Decrement(mapSection.TargetIterations);
				}
			}

			//Debug.WriteLine($"There are {Histogram[Histogram.UpperBound - 1]} points that reached the target iterations.");
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
