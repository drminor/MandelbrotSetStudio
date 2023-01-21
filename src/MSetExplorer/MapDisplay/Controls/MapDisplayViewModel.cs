using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private const double SECTION_DRAW_DELAY = 200;

		private static readonly SizeInt INITIAL_SCREEN_SECTION_ALLOCATION = new(100);

		private static bool _keepDisplaySquare;

		private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private int? _currentMapLoaderJobNumber;

		private readonly DrawingGroup _drawingGroup;
		private readonly ScaleTransform _scaleTransform;
		private readonly IScreenSectionCollection _screenSectionCollection;

		private SizeDbl _canvasSize;
		private VectorInt _canvasControlOffset;
		private double _displayZoom;
		private SizeDbl _logicalDisplaySize;

		private AreaColorAndCalcSettings? _currentJobAreaAndCalcSettings;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		private SizeDbl _containerSize;

		//private bool _cmLoadedButNotHandled;

		private object _paintLocker;

		#region Constructor

		public MapDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_useEscapeVelocities = true;
			_keepDisplaySquare = true;

			_synchronizationContext = SynchronizationContext.Current;
			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;
			_currentMapLoaderJobNumber = null;
			//_mapLoaderManager.MapSectionReady += MapSectionReady;

			BlockSize = blockSize;

			_drawingGroup = new DrawingGroup();
			_scaleTransform = new ScaleTransform();
			_drawingGroup.Transform = _scaleTransform;
			_screenSectionCollection = new ScreenSectionCollection(_drawingGroup, BlockSize, INITIAL_SCREEN_SECTION_ALLOCATION);
			ImageSource = new DrawingImage(_drawingGroup);

			_currentJobAreaAndCalcSettings = null;

			_colorBandSet = new ColorBandSet();
			_colorMap = null;

			_logicalDisplaySize = new SizeDbl();

			CanvasControlOffset = new VectorInt();

			_paintLocker = new object();
			MapSections = new ObservableCollection<MapSection>();

			HandleContainerSizeUpdates = true;

			DisplayZoom = 1.0;
			ContainerSize = new SizeDbl(600);
		}

		#endregion

		#region Public Properties

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		public VectorInt ScreenCollectionIndex => _screenSectionCollection.SectionIndex;

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; }

		public ImageSource ImageSource { get; init; }

		public AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings
		{
			get => _currentJobAreaAndCalcSettings;
			set
			{
				if (value != _currentJobAreaAndCalcSettings)
				{
					var previousValue = _currentJobAreaAndCalcSettings;
					_currentJobAreaAndCalcSettings = value?.Clone();
					HandleCurrentJobChanged(previousValue, _currentJobAreaAndCalcSettings);
					OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings));
				}
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");

					_colorMap = LoadColorMap(value);

					RedrawSections(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
				}
			}
		}

		private ColorMap LoadColorMap(ColorBandSet colorBandSet)
		{
			_colorBandSet = colorBandSet;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = _useEscapeVelocities,
				HighlightSelectedColorBand = _highlightSelectedColorBand
			};

			return colorMap;
		}

		public bool UseEscapeVelocities
		{
			get => _useEscapeVelocities;
			set
			{
				if (value != _useEscapeVelocities)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLine($"The MapDisplay is turning {strState} the use of EscapeVelocities.");
					_useEscapeVelocities = value;

					if (_colorMap != null)
					{
						_colorMap.UseEscapeVelocities = value;
						RedrawSections(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
					}
				}
			}
		}

		public bool HighlightSelectedColorBand
		{
			get => _highlightSelectedColorBand;
			set
			{
				if (value != _highlightSelectedColorBand)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLine($"The MapDisplay is turning {strState} the Highlighting the selected ColorBand.");
					_highlightSelectedColorBand = value;

					if (_colorMap != null)
					{
						_colorMap.HighlightSelectedColorBand = value;
						RedrawSections(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
					}
				}
			}
		}

		public bool HandleContainerSizeUpdates { get; set; }

		// TODO: Prevent the ContainerSize from being set to a value that would require more than 100 x 100 blocks.
		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				if (HandleContainerSizeUpdates)
				{
					_containerSize = value;

					var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(ContainerSize, BlockSize, _keepDisplaySquare);
					var desiredCanvasSize = sizeInWholeBlocks.Scale(BlockSize);

					//Debug.WriteLine($"The Container size is now {value}, updating the CanvasSize from {CanvasSize} to {desiredCanvasSize}.");

					CanvasSize = new SizeDbl(desiredCanvasSize);
				}
				else
				{
					//Debug.WriteLine($"Not handling the ContainerSize update. The value is {value}.");
				}
			}
		}

		public SizeDbl CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					Debug.WriteLine($"The MapDisplay Canvas Size is now {value}.");
					_canvasSize = value;
					LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
				}
			}
		}

		// TODO: Prevent the DisplayZoom from being set to a value that would require more than 100 x 100 blocks.
		/// <summary>
		/// 1 = LogicalDisplay Size = PosterSize
		/// 2 = LogicalDisplay Size Width is 1/2 PosterSize Width (1 Screen Pixel = 2 * (CanvasSize / PosterSize)
		/// 4 = 1/4 PosterSize
		/// Maximum is PosterSize / Actual CanvasSize 
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value  -_displayZoom) > 0.01)
				{
					ClearDisplay();

					//Debug.WriteLine($"The DrawingGroup has {_screenSectionCollection.CurrentDrawingGroupCnt} item.");

					_displayZoom = value;

					_scaleTransform.ScaleX = 1 / _displayZoom;
					_scaleTransform.ScaleY = 1 / _displayZoom;

					LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);

					OnPropertyChanged();
				}
			}
		}

		public SizeDbl LogicalDisplaySize
		{
			get => _logicalDisplaySize;
			set
			{
				if (_logicalDisplaySize != value)
				{
					_logicalDisplaySize = value;

					Debug.WriteLine($"MapDisplay's Logical DisplaySize is now {value}.");

					UpdateScreenCollectionSize(LogicalDisplaySize.Round(), CanvasControlOffset);
					OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
				}
			}
		}

		public VectorInt CanvasControlOffset
		{
			get => _canvasControlOffset;
			set
			{
				if (value != _canvasControlOffset)
				{
					_canvasControlOffset = value;

					UpdateScreenCollectionSize(LogicalDisplaySize.Round(), CanvasControlOffset);
					OnPropertyChanged();
				}
			}
		}

		public ObservableCollection<MapSection> MapSections { get; init; }

		private void UpdateScreenCollectionSize(SizeInt logicalContainerSize, VectorInt canvasControlOffset)
		{
			// Calculate the number of Block-Sized screen sections needed to fill the display at the current Zoom.
			var sizeInBlocks = RMapHelper.GetMapExtentInBlocks(logicalContainerSize, canvasControlOffset, BlockSize);

			_screenSectionCollection.CanvasSizeInBlocks = sizeInBlocks;
		}

		#endregion

		#region Public Methods

		public void SubmitJob(AreaColorAndCalcSettings job)
		{
			if (job.IsEmpty)
			{
				throw new ArgumentException("The job cannot be empty.");
			}

			CancelJob();

			CanvasControlOffset = job.MapAreaInfo.CanvasControlOffset;
			_currentMapLoaderJobNumber = _mapLoaderManager.Push(job.OwnerId, job.OwnerType, job.MapAreaInfo, job.MapCalcSettings, MapSectionReady);
		}

		public void CancelJob()
		{
			if (_currentMapLoaderJobNumber != null)
			{
				_mapLoaderManager.StopJob(_currentMapLoaderJobNumber.Value);
				_currentMapLoaderJobNumber = null;
			}
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				_screenSectionCollection.HideScreenSections();
				MapSections.Clear();
			}
		}

		public void RestartLastJob()
		{
			var currentJob = CurrentAreaColorAndCalcSettings;

			if (currentJob != null && !currentJob.IsEmpty)
			{
				SubmitJob(currentJob);
			}
		}

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var screenArea = e.Area;
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, screenArea, e.IsPreview));
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var screenArea = new RectangleInt(new PointInt(invOffset), CanvasSize.Round());
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, screenArea));
		}

		#endregion

		#region Event Handlers

		private int _callCounter;
		private List<Tuple<MapSection, int>> _mapSectionsPendingUiUpdate = new List<Tuple<MapSection, int>>();
		private Stopwatch? _stopwatch;

		private void MapSectionReady(MapSection mapSection, int jobNumber, bool isLastSection)
		{
			var shouldUpdateUi = IsMapSectionForCurJob(mapSection, jobNumber, isLastSection, _mapSectionsPendingUiUpdate);

			if (shouldUpdateUi)
			{
				//_synchronizationContext?.Send(async (o) => await UpdateUi(mapSectionsPendingUiUpdate), null);
				_synchronizationContext?.Send(o => UpdateUi(_mapSectionsPendingUiUpdate), null);

				if (isLastSection)
				{
					_synchronizationContext?.Post(o =>
					{
						_screenSectionCollection.Finish();
						DisplayJobCompleted?.Invoke(this, jobNumber);
					}
					, null);
				}
			}
		}

		private bool IsMapSectionForCurJob(MapSection mapSection, int jobNumber, bool isLastSection, List<Tuple<MapSection, int>> sectionsPendingUiUpdate)
		{
			lock (_paintLocker)
			{
				if (jobNumber != _currentMapLoaderJobNumber)
				{
					return false;
				}

				if ( (!mapSection.IsEmpty) && mapSection.Counts != null)
				{
					try
					{
						DrawASection(mapSection, _colorMap, _useEscapeVelocities, drawOffline: true);
						sectionsPendingUiUpdate.Add(new Tuple<MapSection, int>(mapSection, jobNumber));
					}
					catch (Exception e)
					{
						Debug.WriteLine($"While calling DrawASection, got an exception: {e}.");
					}
				}

				bool shouldUpdateUi;
				if (isLastSection)
				{
					//Debug.WriteLine($"Setting should update to true, it is the last section.");
					shouldUpdateUi = true;
					if (_stopwatch != null)
					{
						_stopwatch.Stop();
						_stopwatch = null;
					}
				}
				else
				{
					if (_stopwatch == null)
					{
						_stopwatch = Stopwatch.StartNew();
					}

					var callCounterExpired = --_callCounter <= 0;
					var drawDelayDurationExceeded = _stopwatch?.ElapsedMilliseconds > SECTION_DRAW_DELAY;

					shouldUpdateUi = callCounterExpired || drawDelayDurationExceeded;
					if (shouldUpdateUi)
					{
						//Debug.WriteLine($"Setting should update to true, callCounterExpired: {callCounterExpired}, drawDelayDurationExceeded: {drawDelayDurationExceeded}.");

						if (_stopwatch != null)
						{
							_stopwatch.Restart();
						}
						_callCounter = Math.Min(CanvasSize.Round().Width / BlockSize.Width, 8);
					}
					else
					{
						//Debug.WriteLine($"Setting should update to false.");

					}
				}

				return shouldUpdateUi;
			}
		}

		private bool UpdateUi(List<Tuple<MapSection, int>> sectionsPendingUiUpdate)
		{ 
			foreach (var mapSectionAndJobNumber in sectionsPendingUiUpdate)
			{
				var mapSection = mapSectionAndJobNumber.Item1;
				var jobNumber = mapSectionAndJobNumber.Item2;

				if (jobNumber == _currentMapLoaderJobNumber)
				{
					MapSections.Add(mapSection);
					_screenSectionCollection.Redraw(mapSection.BlockPosition);
				}
				else
				{
					Debug.WriteLine($"UpdateUi is skipping. The jobNumber = {jobNumber}, our JobNumber = {_currentMapLoaderJobNumber}.");
				}
			}

			sectionsPendingUiUpdate.Clear();

			return true;
		}

		#endregion

		#region Private Methods

		private void RedrawSections(ColorMap colorMap, bool useEscapeVelocities, bool highlightSelectedColorBand)
		{
			IReadOnlyList<MapSection> loadedSections;
			lock (_paintLocker)
			{
				loadedSections = GetMapSectionsSnapShot();
			}

			DrawSections(loadedSections, colorMap, useEscapeVelocities, highlightSelectedColorBand);
		}

		private void HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			//Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {newJob?.Id ?? ObjectId.Empty}");
			if (_currentMapLoaderJobNumber != null)
			{
				_mapLoaderManager.StopJob(_currentMapLoaderJobNumber.Value);
				_currentMapLoaderJobNumber = null;
			}

			if (newJob?.IsEmpty == false)
			{
				//if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
				//{
				//	_currentMapLoaderJobNumber = ReuseLoadedSections(newJob);
				//}
				//else
				//{
				//	//Debug.WriteLine($"Clearing Display. TransformType: {newJob.TransformType}.");
				// ClearDisplay();

				//	CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;
				//	_currentMapLoaderJobNumber = _mapLoaderManager.Push(newJob);
				//}

				//Debug.WriteLine($"Clearing Display. TransformType: {newJob.TransformType}.");
				ClearDisplay();

				CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

				if (newJob.ColorBandSet != ColorBandSet)
				{
					_colorMap = LoadColorMap(newJob.ColorBandSet);
				}

				_currentMapLoaderJobNumber = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady);
			}
			else
			{
				ClearDisplay();
			}
		}

		//private int? ReuseLoadedSections(JobAreaAndCalcSettings jobAreaAndCalcSettings)
		//{
		//	var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(jobAreaAndCalcSettings);
		//	var loadedSections = GetMapSectionsSnapShot();

		//	// Avoid requesting sections already drawn
		//	var sectionsToLoad = GetNotYetLoaded(sectionsRequired, loadedSections);

		//	// Remove from the screen sections that are not part of the updated view.
		//	var shiftAmount = UpdateMapSectionCollection(MapSections, sectionsRequired, out var cntRemoved, out var cntRetained, out var cntUpdated);

		//	Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {cntRemoved}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");

		//	var newCanvasControlOffset = jobAreaAndCalcSettings.MapAreaInfo.CanvasControlOffset;

		//	if (!shiftAmount.EqualsZero)
		//	{
		//		_screenSectionCollection.Shift(shiftAmount);

		//		if (CanvasControlOffset != newCanvasControlOffset)
		//		{
		//			CanvasControlOffset = newCanvasControlOffset;
		//		}

		//		RedrawSections(MapSections);
		//	}
		//	else
		//	{
		//		if (CanvasControlOffset != newCanvasControlOffset)
		//		{
		//			CanvasControlOffset = newCanvasControlOffset;
		//		}
		//	}

		//	if (sectionsToLoad.Count > 0)
		//	{
		//		var result = _mapLoaderManager.Push(jobAreaAndCalcSettings, sectionsToLoad);
		//		return result;
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		//private bool ShouldAttemptToReuseLoadedSections(JobAreaAndCalcSettings? previousJob, JobAreaAndCalcSettings newJob)
		//{
		//	//if (MapSections.Count == 0 || previousJob is null)
		//	//{
		//	//	return false;
		//	//}

		//	//if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
		//	//{
		//	//	return false;
		//	//}

		//	////if (newJob.CanvasSizeInBlocks != previousJob.CanvasSizeInBlocks)
		//	////{
		//	////	return false;
		//	////}

		//	//var jobSpd = RNormalizer.Normalize(newJob.MapAreaInfo.Subdivision.SamplePointDelta, previousJob.MapAreaInfo.Subdivision.SamplePointDelta, out var previousSpd);
		//	//return jobSpd == previousSpd;

		//	return false;
		//}

		//private IList<MapSection> GetNotYetLoaded(IList<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		//{
		//	var result = sectionsNeeded.Where(
		//		neededSection => !sectionsPresent.Any(
		//			presentSection => presentSection == neededSection 
		//			&& presentSection.TargetIterations == neededSection.TargetIterations
		//			)
		//		).ToList();

		//	return result;
		//}

		//private VectorInt UpdateMapSectionCollection(ObservableCollection<MapSection> sectionsPresent, IList<MapSection> newSet, out int cntRemoved, out int cntRetained, out int cntUpdated)
		//{
		//	cntRemoved = 0;
		//	cntRetained = 0;
		//	cntUpdated = 0;

		//	var toBeRemoved = new List<MapSection>();
		//	var differences = new Dictionary<VectorInt, int>();

		//	foreach (var mapSection in sectionsPresent)
		//	{
		//		var matchingNewSection = newSet.FirstOrDefault(x => x == mapSection);
		//		if (matchingNewSection == null)
		//		{
		//			toBeRemoved.Add(mapSection);
		//			cntRemoved++;
		//		}
		//		else
		//		{
		//			cntRetained++;
		//			var diff = matchingNewSection.BlockPosition.Sub(mapSection.BlockPosition);
		//			if (diff.X != 0 || diff.Y != 0)
		//			{
		//				cntUpdated++;
		//				mapSection.BlockPosition = matchingNewSection.BlockPosition;

		//				if (differences.TryGetValue(diff, out var value))
		//				{
		//					differences[diff] = value + 1;
		//				}
		//				else
		//				{
		//					differences.Add(diff, 1);
		//				}
		//			}
		//		}
		//	}

		//	VectorInt shiftAmount;
		//	if (differences.Count > 0)
		//	{
		//		var mostPrevalentCnt = differences.Max(x => x.Value);
		//		shiftAmount = differences.First(x => x.Value == mostPrevalentCnt).Key;
		//	}
		//	else
		//	{
		//		shiftAmount = new VectorInt();
		//	}

		//	foreach(var mapSection in toBeRemoved)
		//	{
		//		if (!sectionsPresent.Remove(mapSection))
		//		{
		//			Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
		//			//Thread.Sleep(300);
		//		}
		//	}

		//	return shiftAmount;
		//}

		private IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}

		private void DrawSections(IEnumerable<MapSection> mapSections, ColorMap? colorMap, bool useEscapVelocities, bool highlightSelectedColorBand)
		{
			if (colorMap != null)
			{
				Debug.Assert(colorMap.UseEscapeVelocities == useEscapVelocities, "UseEscapeVelocities MisMatch on DrawSections.");
				Debug.Assert(colorMap.HighlightSelectedColorBand == highlightSelectedColorBand, "HighlightSelectedColorBand MisMatch on DrawSections.");

				foreach (var mapSection in mapSections)
				{
					DrawASection(mapSection, colorMap, useEscapVelocities, drawOffline: false);
				}
			}
			else
			{
				foreach (var mapSection in mapSections)
				{
					Debug.WriteLine($"Not drawing screen section at position: {mapSection.BlockPosition}, the color map is null.");
				}
			}
		}

		private void DrawASection(MapSection mapSection, ColorMap? colorMap, bool useEscapVelocities, bool drawOffline)
		{
			if (mapSection.Counts != null && colorMap != null)
			{
				//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				var pixels = _mapSectionHelper.GetPixelArray(mapSection.Counts, mapSection.EscapeVelocities, mapSection.Size, colorMap, !mapSection.IsInverted, useEscapVelocities);

				_screenSectionCollection.Draw(mapSection.BlockPosition, pixels, drawOffline);
			}
			else
			{
				//Debug.WriteLine($"Not drawing -- Counts are null.");
			}
		}

		private void RedrawSections(IEnumerable<MapSection> source)
		{
			Debug.WriteLine($"Hiding all screen sections and redrawing {source.Count()}.");
			ClearDisplay();

			foreach (var mapSection in source)
			{
				//Debug.WriteLine($"About to redraw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				_screenSectionCollection.Redraw(mapSection.BlockPosition);
				//Thread.Sleep(200);
			}
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					//MapSections.CollectionChanged -= MapSections_CollectionChanged;
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
