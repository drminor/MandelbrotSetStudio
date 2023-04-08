using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		private static bool KEEP_DISPLAY_SQUARE;
		private static bool REUSE_SECTIONS_FOR_SOME_OPS;

		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;

		private BitmapGrid _bitmapGrid;

		private object _paintLocker;

		private SizeDbl _containerSize;
		private SizeDbl _canvasSize;

		private VectorInt _canvasControlOffset;
		private double _displayZoom;
		private SizeDbl _logicalDisplaySize;

		private AreaColorAndCalcSettings? _currentJobAreaAndCalcSettings;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			BlockSize = blockSize;

			KEEP_DISPLAY_SQUARE = true;
			REUSE_SECTIONS_FOR_SOME_OPS = false;    // TODO: Set REUSE_SECTIONS_FOR_SOME_OPS to TRUE!!

			_paintLocker = new object();

			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;

			_bitmapGrid = new BitmapGrid(_mapSectionHelper, BlockSize, x => { Bitmap = x; });

			//MapSections = new ObservableCollection<MapSection>();

			_currentJobAreaAndCalcSettings = null;

			_logicalDisplaySize = new SizeDbl();
			CanvasControlOffset = new VectorInt();

			HandleContainerSizeUpdates = true;

			DisplayZoom = 1.0;
			ContainerSize = new SizeDbl(600);
		}

		#endregion

		#region Public Properties - Content

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		public SizeInt BlockSize { get; init; }

		public ObservableCollection<MapSection> MapSections => _bitmapGrid.MapSections;

		public AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings
		{
			get => _currentJobAreaAndCalcSettings;
			set
			{
				if (value != _currentJobAreaAndCalcSettings)
				{
					var previousValue = _currentJobAreaAndCalcSettings;
					_currentJobAreaAndCalcSettings = value?.Clone() ?? null;

					Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {_currentJobAreaAndCalcSettings?.OwnerId ?? ObjectId.Empty.ToString()}");
					HandleCurrentJobChanged(previousValue, _currentJobAreaAndCalcSettings);

					OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings));
				}
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _bitmapGrid.ColorBandSet;
			set
			{
				lock (_paintLocker)
				{
					_bitmapGrid.ColorBandSet = value;
				}
			}
		}

		public bool UseEscapeVelocities
		{
			get => _bitmapGrid.UseEscapeVelocities;
			set
			{
				lock (_paintLocker)
				{
					_bitmapGrid.UseEscapeVelocities = value;
				}
			}
		}

		public bool HighlightSelectedColorBand
		{
			get => _bitmapGrid.HighlightSelectedColorBand;
			set
			{
				lock (_paintLocker)
				{
					_bitmapGrid.HighlightSelectedColorBand = value;
				}
			}
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public WriteableBitmap Bitmap
		{
			get => _bitmapGrid.Bitmap;
			set
			{
				//_bitmap = value;
				OnPropertyChanged();
			}
		}

		public bool HandleContainerSizeUpdates { get; set; }

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				if (HandleContainerSizeUpdates)
				{
					_containerSize = value;

					var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(ContainerSize, BlockSize, KEEP_DISPLAY_SQUARE);
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

					//LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);

					CanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize.Round(), BlockSize);

					LogicalDisplaySize = CanvasSize;

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
				}
			}
		}

		public SizeInt CanvasSizeInBlocks
		{
			get => _bitmapGrid.CanvasSizeInBlocks;
			set
			{
				_bitmapGrid.CanvasSizeInBlocks = value;
				//OnPropertyChanged(nameof(IMapDisplayViewModel.Bitmap));
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
					OnPropertyChanged();
				}
			}
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - _displayZoom) > 0.01)
				{
					//ClearMapSectionsAndBitmap(mapLoaderJobNumber: null);

					// TODX: Prevent the DisplayZoom from being set to a value that would require more than 100 x 100 blocks.
					// 1 = LogicalDisplay Size = PosterSize
					// 2 = LogicalDisplay Size Width is 1/2 PosterSize Width (Every screen pixels covers a 2x2 group of pixels from the final image, i.e., the poster.)
					// 4 = 1/4 PosterSize
					// Maximum is PosterSize / Actual CanvasSize 

					//Debug.WriteLine($"The DrawingGroup has {_screenSectionCollection.CurrentDrawingGroupCnt} item.");

					_displayZoom = value;

					// TODX: scc -- Need to place the WriteableBitmap within a DrawingGroup.
					//_scaleTransform.ScaleX = 1 / _displayZoom;
					//_scaleTransform.ScaleY = 1 / _displayZoom;

					//LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);
					LogicalDisplaySize = CanvasSize;

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

					OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
				}
			}
		}

		#endregion

		#region Public Methods

		public void SubmitJob(AreaColorAndCalcSettings newAreaColorAndCalcSettings)
		{
			CurrentAreaColorAndCalcSettings = newAreaColorAndCalcSettings;
		}

		public void CancelJob()
		{
			lock (_paintLocker)
			{
				StopCurrentJobAndClearDisplay();
			}
		}

		public void RestartLastJob()
		{
			lock (_paintLocker)
			{
				var currentJob = CurrentAreaColorAndCalcSettings;

				if (currentJob != null && !currentJob.IsEmpty)
				{
					var mapSections = _mapLoaderManager.Push(currentJob.OwnerId, currentJob.OwnerType, currentJob.MapAreaInfo, currentJob.MapCalcSettings, MapSectionReady, out var newJobNumber);
					_bitmapGrid.AddJobNumAndMapOffset(newJobNumber, currentJob.MapAreaInfo.MapBlockOffset);
				}
			}
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				_bitmapGrid.ClearDisplay();
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

		private void MapSectionReady(MapSection mapSection)
		{
			_bitmapGrid.Dispatcher.Invoke(GetAndPlacePixelsWrapper, new object[] { mapSection });
		}

		public void GetAndPlacePixelsWrapper(MapSection mapSection)
		{
			if (mapSection.MapSectionVectors != null)
			{
				lock (_paintLocker)
				{
					_ = _bitmapGrid.GetAndPlacePixels(mapSection, mapSection.MapSectionVectors, out var blockPosition);
				}
			}

			if (mapSection.IsLastSection)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
			}
		}

		#endregion

		#region Private Methods

		private void HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			int? newJobNumber = null;

			var lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				if (newJob != null && !newJob.IsEmpty)
				{
					if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
					{
						newJobNumber = ReuseAndLoad(newJob, out lastSectionWasIncluded);
					}
					else
					{
						StopCurrentJobAndClearDisplay();
						newJobNumber = DiscardAndLoad(newJob, out lastSectionWasIncluded);
					}
				}
				else
				{
					StopCurrentJobAndClearDisplay();
				}
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}
		}

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			lastSectionWasIncluded = false;
			int? result = null;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(newJob.MapAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new ReadOnlyCollection<MapSection>(MapSections);
			var sectionsToLoad = GetSectionsToLoad(sectionsRequired, loadedSections);
			var sectionsToRemove = GetSectionsToRemove(sectionsRequired, loadedSections);

			foreach (var section in sectionsToRemove)
			{
				MapSections.Remove(section);
				_mapSectionHelper.ReturnMapSection(section);
			}

			//Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {sectionsToRemove.Count}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");
			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {sectionsToRemove.Count}.");

			CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;


			// TODO: Create method on _bitmapGrid to
			// 1. Take a new ColorMap
			// 2. Clear and redraw existing sections
			// 3. Take a list of new sections and draw these.


			//// Refresh the display, load the sections immediately available, and send request to generate those not available.
			//if (newJob.ColorBandSet != ColorBandSet)
			//{
			//	_colorMap = LoadColorMap(newJob.ColorBandSet);
			//}

			//ClearBitmap(_bitmap);

			//if (_colorMap != null)
			//{
			//	RedrawSections(_colorMap, newJob.MapAreaInfo.MapBlockOffset);

			//	if (sectionsToLoad.Count > 0)
			//	{
			//		var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);

			//		_bitmapGrid.CurrentMapLoaderJobNumber = newJobNumber;

			//		//_currentMapLoaderJobNumber = newJobNumber;
			//		//_jobMapOffsets.Add(newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

			//		_bitmapGrid.AddJobNumAndMapOffset(newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

			//		result = newJobNumber;

			//		lastSectionWasIncluded = LoadAndDrawNewSections(mapSections, _colorMap, newJob.MapAreaInfo.MapBlockOffset);
			//	}
			//}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

			lastSectionWasIncluded = _bitmapGrid.DiscardAndLoad(mapSections, newJob.ColorBandSet, newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

			return newJobNumber;
		}

		private void StopCurrentJobAndClearDisplay()
		{
			var currentJobNumber = _bitmapGrid.CurrentMapLoaderJobNumber;

			if (currentJobNumber != null)
			{
				_mapLoaderManager.StopJob(currentJobNumber.Value);

				_bitmapGrid.JobMapOffsets.Remove(currentJobNumber.Value);

				_bitmapGrid.CurrentMapLoaderJobNumber = null;
			}

			foreach (var kvp in _bitmapGrid.JobMapOffsets)
			{
				_mapLoaderManager.StopJob(kvp.Key);
			}

			_bitmapGrid.JobMapOffsets.Clear();

			_bitmapGrid.ClearDisplay();
		}

		private bool ShouldAttemptToReuseLoadedSections(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings newJob)
		{
			if (!REUSE_SECTIONS_FOR_SOME_OPS)
			{
				return false;
			}

			if (MapSections.Count == 0 || previousJob is null)
			{
				return false;
			}

			if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
			{
				return false;
			}

			//var jobSpd = RNormalizer.Normalize(newJob.MapAreaInfo.Subdivision.SamplePointDelta, previousJob.MapAreaInfo.Subdivision.SamplePointDelta, out var previousSpd);
			//return jobSpd == previousSpd;

			var inSameSubdivision = newJob.MapAreaInfo.Subdivision.Id == previousJob.MapAreaInfo.Subdivision.Id;

			return inSameSubdivision;
		}

		private List<MapSection> GetSectionsToLoad(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		{
			// Find all sections where exists in needed, but is not found in those present.

			var result = sectionsNeeded.Where(
				neededSection => !sectionsPresent.Any(
					presentSection => presentSection == neededSection
					&& presentSection.TargetIterations == neededSection.TargetIterations
					)
				).ToList();

			return result;
		}

		private List<MapSection> GetSectionsToRemove(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		{
			// Find all sections where exists in present, but is not found in those needed.

			var result = sectionsPresent.Where(
				existingSection => !sectionsNeeded.Any(
					neededSection => neededSection == existingSection
					&& neededSection.TargetIterations == existingSection.TargetIterations
					)
				).ToList();

			return result;
		}

		private List<MapSection> GetSectionsToLoadX(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent, out List<MapSection> sectionsToRemove)
		{
			var result = new List<MapSection>();
			sectionsToRemove = new List<MapSection>();

			foreach (var ms in sectionsPresent)
			{
				var stillNeed = sectionsNeeded.Any(presentSection => presentSection == ms && presentSection.TargetIterations == ms.TargetIterations);

				if (!stillNeed)
				{
					sectionsToRemove.Add(ms);
				}

			}

			//var result = sectionsNeeded.Where(
			//	neededSection => !sectionsPresent.Any(
			//		presentSection => presentSection == neededSection
			//		&& presentSection.TargetIterations == neededSection.TargetIterations
			//		)
			//	).ToList();

			return result;
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
