using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private static readonly SizeInt INITIAL_SCREEN_SECTION_ALLOCATION = new(100);

		private static bool _keepDisplaySquare;

		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private int? _currentMapLoaderJobNumber;

		private readonly DrawingGroup _drawingGroup;
		private readonly ScaleTransform _scaleTransform;
		private readonly IScreenSectionCollection _screenSectionCollection;

		private SizeInt _canvasSize;
		private VectorInt _canvasControlOffset;
		private double _displayZoom;

		private JobAreaAndCalcSettings? _currentJobAreaAndCalcSettings;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		private SizeDbl _containerSize;

		private bool _cmLoadedButNotHandled;

		#region Constructor

		public MapDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_useEscapeVelocities = true;
			_keepDisplaySquare = false;
			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;
			_currentMapLoaderJobNumber = null;
			_mapLoaderManager.MapSectionReady += MapSectionReady;

			BlockSize = blockSize;

			_drawingGroup = new DrawingGroup();
			_scaleTransform = new ScaleTransform();
			_drawingGroup.Transform = _scaleTransform;
			_screenSectionCollection = new ScreenSectionCollection(_drawingGroup, BlockSize, INITIAL_SCREEN_SECTION_ALLOCATION);
			ImageSource = new DrawingImage(_drawingGroup);

			_currentJobAreaAndCalcSettings = null;

			_colorBandSet = new ColorBandSet();
			_colorMap = null;

			//_containerSize = new SizeDbl(1050, 1050);
			//CanvasSize = new SizeInt(1024, 1024);
			//var screenSectionExtent = new SizeInt(12, 12);

			_logicalDisplaySize = new SizeDbl();

			DisplayZoom = 1.0;
			ContainerSize = new SizeDbl(1050, 1050);

			CanvasControlOffset = new VectorInt();

			MapSections = new ObservableCollection<MapSection>();
			MapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; }

		public ImageSource ImageSource { get; init; }

		public JobAreaAndCalcSettings? CurrentJobAreaAndCalcSettings
		{
			get => _currentJobAreaAndCalcSettings;
			set
			{
				if (value != _currentJobAreaAndCalcSettings)
				{
					var previousValue = _currentJobAreaAndCalcSettings;
					_currentJobAreaAndCalcSettings = value?.Clone();
					HandleCurrentJobChanged(previousValue, _currentJobAreaAndCalcSettings);
					OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentJobAreaAndCalcSettings));
				}
			}
		}

		public ColorBandSet ColorBandSet => _colorBandSet;

		public void SetColorBandSet(ColorBandSet value, bool updateDisplay)
		{
			if (value != _colorBandSet)
			{
				Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}. UpdateDisplay = {updateDisplay}");

				if (CurrentJobAreaAndCalcSettings is null)
				{
					// Take the value given, as is. Without a current job, we cannot adjust the iterations.
					LoadColorMap(value);
					_cmLoadedButNotHandled = true;
				}
				else
				{
					var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(value, CurrentJobAreaAndCalcSettings.MapCalcSettings.TargetIterations);
					LoadColorMap(adjustedColorBandSet);
					//LoadColorMap(value);

					if (updateDisplay)
					{
						HandleColorMapChanged(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
						_cmLoadedButNotHandled = false;
					}
					else
					{
						_cmLoadedButNotHandled = true;
					}

				}
			}
			else
			{
				if (updateDisplay)
				{
					Debug.WriteLine($"The MapDisplay is processing the existing ColorMap event though the new value is the same as the existing value. Id = {value.Id}. ColorMapLoadedButNotHandled = {_cmLoadedButNotHandled}.");
					HandleColorMapChanged(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
					_cmLoadedButNotHandled = false;
				}
				else
				{
					Debug.WriteLine($"The MapDisplay is NOT processing a new ColorMap, the new value is the same as the existing value. Id = {value.Id}. UpdateDisplay = {updateDisplay}");
				}
			}
		}

		private void LoadColorMap(ColorBandSet colorBandSet)
		{
			if (ColorBandSet != colorBandSet)
			{
				_colorBandSet = colorBandSet;
				_colorMap = new ColorMap(colorBandSet)
				{
					UseEscapeVelocities = _useEscapeVelocities,
					HighlightSelectedColorBand = _highlightSelectedColorBand
				};
			}
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

					if (!(_colorMap is null))
					{
						_colorMap.UseEscapeVelocities = value;
						HandleColorMapChanged(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
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

					if (!(_colorMap is null))
					{
						_colorMap.HighlightSelectedColorBand = value;
						HandleColorMapChanged(_colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
					}
				}
			}
		}

		// TODO: Prevent the ContainerSize from being set to a value that would require more than 100 x 100 blocks.
		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;

				Debug.WriteLine($"The container size is now {value}.");

				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(_containerSize, BlockSize, _keepDisplaySquare);
				CanvasSize = sizeInWholeBlocks.Scale(BlockSize);
			}
		}

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					Debug.WriteLine($"The MapDisplay Canvas Size is now {value}.");
					_canvasSize = value;
					LogicalDisplaySize = new SizeDbl(_canvasSize).Scale(1 / DisplayZoom);

					OnPropertyChanged();
				}
			}
		}

		//public RectangleDbl ClipRegion => ScreenTypeHelper.ConvertToRectangleDbl(_drawingGroup.ClipGeometry.Bounds);

		// TODO: Prevent the DisplayZoom from being set to a value that would require more than 100 x 100 blocks.

		/// <summary>
		/// Value between 0.0 and 1.0
		/// 1.0 presents 1 map "pixel" to 1 screen pixel
		/// 0.5 presents 2 map "pixels" to 1 screen pixel
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value  -_displayZoom) > 0.1)
				{
					_displayZoom = value;

					//_drawingGroup.Transform = new ScaleTransform(_displayZoom, _displayZoom);
					_scaleTransform.ScaleX = _displayZoom;
					_scaleTransform.ScaleY = _displayZoom;

					LogicalDisplaySize = new SizeDbl(CanvasSize).Scale(1 / _displayZoom);

					OnPropertyChanged();
				}
			}
		}

		private SizeDbl _logicalDisplaySize;
		public SizeDbl LogicalDisplaySize
		{
			get => _logicalDisplaySize;
			set
			{
				if (_logicalDisplaySize != value)
				{
					_logicalDisplaySize = value;

					UpdateScreenCollectionSize(_logicalDisplaySize, CanvasControlOffset);

					Debug.WriteLine($"MapDisplay's Logical DisplaySize is now {value}.");

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

					UpdateScreenCollectionSize(LogicalDisplaySize, _canvasControlOffset);

					OnPropertyChanged();
				}
			}
		}

		public ObservableCollection<MapSection> MapSections { get; }

		private void UpdateScreenCollectionSize(SizeDbl logicalContainerSize, VectorInt canvasControlOffset)
		{
			// Calculate the number of Block-Sized screen sections needed to fill the display at the current Zoom.

			//var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(logicalContainerSize, BlockSize, _keepDisplaySquare);

			var sizeInBlocks = RMapHelper.GetMapExtentInBlocks(logicalContainerSize, canvasControlOffset, BlockSize);

			_screenSectionCollection.CanvasSizeInBlocks = sizeInBlocks;
		}

		#endregion

		#region Public Methods

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
			var screenArea = new RectangleInt(new PointInt(invOffset), CanvasSize);
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, screenArea));
		}

		public void TearDown()
		{
			// TODO: Unsubscribe our Event Handlers in MapDisplayViewModel::TearDown
		}

		#endregion

		#region Event Handlers

		private void MapSectionReady(object? sender, Tuple<MapSection, int> e)
		{
			// TODO: Use a lock on MapSectionReady to avoid race conditions as the ColorMap is applied.

			if (e.Item2 == _currentMapLoaderJobNumber)
			{
				MapSections.Add(e.Item1);
			}
		}

		private void MapSections_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				_screenSectionCollection.HideScreenSections();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Add items
				var mapSections = e.NewItems?.Cast<MapSection>() ?? new List<MapSection>();
				DrawSections(mapSections, _colorMap, _useEscapeVelocities, _highlightSelectedColorBand);
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var mapSections = e.OldItems?.Cast<MapSection>() ?? new List<MapSection>();
				foreach (var mapSection in mapSections)
				{
					if (!_screenSectionCollection.Hide(mapSection))
					{
						Debug.WriteLine($"While handling the MapSections_CollectionChanged:Remove, the MapDisplayViewModel cannot Hide the MapSection: {mapSection}.");
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private void HandleColorMapChanged(ColorMap? colorMap, bool useEscapeVelocities, bool highlightSelectedColorBand)
		{
			if (colorMap != null)
			{
				var loadedSections = GetMapSectionsSnapShot();
				DrawSections(loadedSections, colorMap, useEscapeVelocities, highlightSelectedColorBand);
			}
		}

		private void HandleCurrentJobChanged(JobAreaAndCalcSettings? previousJob, JobAreaAndCalcSettings? newJob)
		{
			//Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {newJob?.Id ?? ObjectId.Empty}");
			if (_currentMapLoaderJobNumber != null)
			{
				_mapLoaderManager.StopJob(_currentMapLoaderJobNumber.Value);
				_currentMapLoaderJobNumber = null;
			}

			if (newJob?.IsEmpty == false)
			{
				if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
				{
					_currentMapLoaderJobNumber = ReuseLoadedSections(newJob);
				}
				else
				{
					//Debug.WriteLine($"Clearing Display. TransformType: {newJob.TransformType}.");
					MapSections.Clear();
					CanvasControlOffset = newJob.JobAreaInfo.CanvasControlOffset;
					_currentMapLoaderJobNumber = _mapLoaderManager.Push(newJob);
				}
			}
			else
			{
				MapSections.Clear();
			}
		}

		private int? ReuseLoadedSections(JobAreaAndCalcSettings jobAreaAndCalcSettings)
		{
			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(jobAreaAndCalcSettings);
			var loadedSections = GetMapSectionsSnapShot();

			// Avoid requesting sections already drawn
			var sectionsToLoad = GetNotYetLoaded(sectionsRequired, loadedSections);

			// Remove from the screen sections that are not part of the updated view.
			var shiftAmount = UpdateMapSectionCollection(MapSections, sectionsRequired, out var cntRemoved, out var cntRetained, out var cntUpdated);

			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {cntRemoved}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");

			var newCanvasControlOffset = jobAreaAndCalcSettings.JobAreaInfo.CanvasControlOffset;

			if (!shiftAmount.EqualsZero)
			{
				_screenSectionCollection.Shift(shiftAmount);

				if (CanvasControlOffset != newCanvasControlOffset)
				{
					CanvasControlOffset = newCanvasControlOffset;
				}

				RedrawSections(MapSections);
			}
			else
			{
				if (CanvasControlOffset != newCanvasControlOffset)
				{
					CanvasControlOffset = newCanvasControlOffset;
				}
			}

			if (sectionsToLoad.Count > 0)
			{
				var result = _mapLoaderManager.Push(jobAreaAndCalcSettings, sectionsToLoad);
				return result;
			}
			else
			{
				return null;
			}
		}

		private bool ShouldAttemptToReuseLoadedSections(JobAreaAndCalcSettings? previousJob, JobAreaAndCalcSettings newJob)
		{
			//if (MapSections.Count == 0 || previousJob is null)
			//{
			//	return false;
			//}

			//if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
			//{
			//	return false;
			//}

			////if (newJob.CanvasSizeInBlocks != previousJob.CanvasSizeInBlocks)
			////{
			////	return false;
			////}

			//var jobSpd = RNormalizer.Normalize(newJob.JobAreaInfo.Subdivision.SamplePointDelta, previousJob.JobAreaInfo.Subdivision.SamplePointDelta, out var previousSpd);
			//return jobSpd == previousSpd;

			return false;
		}

		private IList<MapSection> GetNotYetLoaded(IList<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		{
			var result = sectionsNeeded.Where(
				neededSection => !sectionsPresent.Any(
					presentSection => presentSection == neededSection 
					&& presentSection.TargetIterations == neededSection.TargetIterations
					)
				).ToList();

			return result;
		}

		private VectorInt UpdateMapSectionCollection(ObservableCollection<MapSection> sectionsPresent, IList<MapSection> newSet, out int cntRemoved, out int cntRetained, out int cntUpdated)
		{
			cntRemoved = 0;
			cntRetained = 0;
			cntUpdated = 0;

			var toBeRemoved = new List<MapSection>();
			var differences = new Dictionary<VectorInt, int>();

			foreach (var mapSection in sectionsPresent)
			{
				var matchingNewSection = newSet.FirstOrDefault(x => x == mapSection);
				if (matchingNewSection == null)
				{
					toBeRemoved.Add(mapSection);
					cntRemoved++;
				}
				else
				{
					cntRetained++;
					var diff = matchingNewSection.BlockPosition.Sub(mapSection.BlockPosition);
					if (diff.X != 0 || diff.Y != 0)
					{
						cntUpdated++;
						mapSection.BlockPosition = matchingNewSection.BlockPosition;

						if (differences.TryGetValue(diff, out var value))
						{
							differences[diff] = value + 1;
						}
						else
						{
							differences.Add(diff, 1);
						}
					}
				}
			}

			VectorInt shiftAmount;
			if (differences.Count > 0)
			{
				var mostPrevalentCnt = differences.Max(x => x.Value);
				shiftAmount = differences.First(x => x.Value == mostPrevalentCnt).Key;
			}
			else
			{
				shiftAmount = new VectorInt();
			}

			foreach(var mapSection in toBeRemoved)
			{
				if (!sectionsPresent.Remove(mapSection))
				{
					Debug.WriteLine($"Could not remove MapSection: {mapSection}.");
					//Thread.Sleep(300);
				}
			}

			return shiftAmount;
		}

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
					if (mapSection.Counts != null)
					{
						//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
						var pixels = _mapSectionHelper.GetPixelArray(mapSection.Counts, mapSection.Size, colorMap, !mapSection.IsInverted, useEscapVelocities);
						_screenSectionCollection.Draw(mapSection.BlockPosition, pixels);
					}
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

		private void RedrawSections(IEnumerable<MapSection> source)
		{
			Debug.WriteLine($"Hiding all screen sections and redrawing {source.Count()}.");
			_screenSectionCollection.HideScreenSections();

			foreach (var mapSection in source)
			{
				//Debug.WriteLine($"About to redraw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
				_screenSectionCollection.Redraw(mapSection.BlockPosition);
				//Thread.Sleep(200);
			}
		}

		#endregion
	}
}
