using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Web.Syndication;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear = new byte[0];
		private static bool _keepDisplaySquare;

		//private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private int? _currentMapLoaderJobNumber;

		private object _paintLocker;

		private SizeDbl _containerSize;
		private SizeDbl _canvasSize;

		private SizeInt _canvasSizeInBlocks;
		private SizeInt _allocatedBlocks;
		private int _maxYPtr;

		private VectorInt _canvasControlOffset;
		private double _displayZoom;
		private SizeDbl _logicalDisplaySize;

		private AreaColorAndCalcSettings? _currentJobAreaAndCalcSettings;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		private Dictionary<int, BigVector> _jobMapOffsets;
		
		#endregion

		#region Constructor

		public MapDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			BlockSize = blockSize;
			BlockRect = new Int32Rect(0, 0, BlockSize.Width, BlockSize.Height);

			_keepDisplaySquare = true;
			_paintLocker = new object();

			_jobMapOffsets = new Dictionary<int, BigVector>();

			//_synchronizationContext = SynchronizationContext.Current;
			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;

			_bitmap = CreateBitmap(new SizeInt(10));
			_maxYPtr = 1;

			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;
			_colorBandSet = new ColorBandSet();
			_colorMap = null;

			MapSections = new ObservableCollection<MapSection>();

			_currentMapLoaderJobNumber = null;
			_currentJobAreaAndCalcSettings = null;

			_logicalDisplaySize = new SizeDbl();
			CanvasControlOffset = new VectorInt();

			HandleContainerSizeUpdates = true;

			DisplayZoom = 1.0;
			ContainerSize = new SizeDbl(600);
			}

		#endregion

		#region Public Properties

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; init; }
		private Int32Rect BlockRect { get; init; }

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<MapSection> MapSections { get; init; }

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
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
					_colorMap = LoadColorMap(value);

					if (_colorMap != null)
					{
						RedrawSections();
					}
				}
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

					if (_colorMap != null)
					{
						_colorMap.UseEscapeVelocities = value;
						RedrawSections();
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
						RedrawSections();
					}
				}
			}
		}

		private void RedrawSections()
		{
			lock (_paintLocker)
			{
				if (_colorMap != null && _currentJobAreaAndCalcSettings != null)
				{
					RedrawSections(_colorMap, _currentJobAreaAndCalcSettings.MapAreaInfo.MapBlockOffset);
				}
			}
		}

		public bool HandleContainerSizeUpdates { get; set; }

		public SizeDbl ContainerSizeExp
		{
			get => _containerSize;
			set
			{
				if (HandleContainerSizeUpdates)
				{
					_containerSize = value;

					var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(_containerSize, BlockSize, _keepDisplaySquare);
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

					//LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);
					LogicalDisplaySize = CanvasSize;

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
				}
			}
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value  -_displayZoom) > 0.01)
				{
					ClearMapSectionsAndBitmap(mapLoaderJobNumber: null);

					// TODO: Prevent the DisplayZoom from being set to a value that would require more than 100 x 100 blocks.
					// 1 = LogicalDisplay Size = PosterSize
					// 2 = LogicalDisplay Size Width is 1/2 PosterSize Width (1 Screen Pixel = 2 * (CanvasSize / PosterSize)
					// 4 = 1/4 PosterSize
					// Maximum is PosterSize / Actual CanvasSize 

					//Debug.WriteLine($"The DrawingGroup has {_screenSectionCollection.CurrentDrawingGroupCnt} item.");

					_displayZoom = value;

					// TODO: scc -- Need to place the WriteableBitmap within a DrawingGroup.
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

					//CanvasSizeInBlocks = CalculateCanvasSizeInBlocks(LogicalDisplaySize, CanvasControlOffset);
					CanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(LogicalDisplaySize.Round(), BlockSize);

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

					//CanvasSizeInBlocks = CalculateCanvasSizeInBlocks(LogicalDisplaySize, CanvasControlOffset);
					OnPropertyChanged();
				}
			}
		}

		public SizeInt CanvasSizeInBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_canvasSizeInBlocks != value)
				{
					// Calculate new size of bitmap in block-sized units
					var newAllocatedBlocks = value.Inflate(2);
					Debug.WriteLine($"Resizing the MapDisplay Writeable Bitmap. Old size: {_allocatedBlocks}, new size: {newAllocatedBlocks}.");

					_allocatedBlocks = newAllocatedBlocks;
					_maxYPtr = _allocatedBlocks.Height - 1;
					_canvasSizeInBlocks = value;

					// Create a new Writeable bitmap instance
					var newSize = newAllocatedBlocks.Scale(BlockSize);
					Bitmap = CreateBitmap(newSize);
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
			var currentJob = CurrentAreaColorAndCalcSettings;

			if (currentJob != null && !currentJob.IsEmpty)
			{
				var mapSections = _mapLoaderManager.Push(currentJob.OwnerId, currentJob.OwnerType, currentJob.MapAreaInfo, currentJob.MapCalcSettings, MapSectionReady, out var newJobNumber);
				_currentMapLoaderJobNumber = newJobNumber;
				_jobMapOffsets.Add(newJobNumber, currentJob.MapAreaInfo.MapBlockOffset);
			}
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				ClearMapSectionsAndBitmap(mapLoaderJobNumber: null);
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
			if (mapSection.MapSectionVectors != null)
			{
				_bitmap.Dispatcher.Invoke(GetAndPlacePixels, new object[] { mapSection, mapSection.MapSectionVectors });
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

			OnPropertyChanged(nameof(Bitmap));
		}

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			lastSectionWasIncluded = false;
			int? result = null;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(newJob.MapAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new ReadOnlyCollection<MapSection>(MapSections);
			var sectionsToLoad = GetSectionsToLoad(sectionsRequired, loadedSections);
			var sectionsToRemove = GetSectionsToRemove(sectionsRequired, loadedSections);

			foreach(var section in sectionsToRemove)
			{
				MapSections.Remove(section);
				_mapSectionHelper.ReturnMapSection(section);
			}

			//Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {sectionsToRemove.Count}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");
			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {sectionsToRemove.Count}.");

			CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			// Refresh the display, load the sections immediately available, and send request to generate those not available.
			if (newJob.ColorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(newJob.ColorBandSet);
			}

			ClearBitmap(_bitmap);

			if (_colorMap != null)
			{
				RedrawSections(_colorMap, newJob.MapAreaInfo.MapBlockOffset);

				if (sectionsToLoad.Count > 0)
				{
					var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);
					_currentMapLoaderJobNumber = newJobNumber;
					_jobMapOffsets.Add(newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

					result = newJobNumber;

					lastSectionWasIncluded = LoadAndDrawNewSections(mapSections, _colorMap, newJob.MapAreaInfo.MapBlockOffset);
				}
			}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady, out var newJobNumber);
			_currentMapLoaderJobNumber = newJobNumber;
			_jobMapOffsets.Add(newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

			if (newJob.ColorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(newJob.ColorBandSet);
			}

			if (_colorMap != null)
			{
				lastSectionWasIncluded = LoadAndDrawNewSections(mapSections, _colorMap, newJob.MapAreaInfo.MapBlockOffset);
			}
			else
			{
				lastSectionWasIncluded = false;
			}

			return newJobNumber;
		}

		private bool LoadAndDrawNewSections(List<MapSection> mapSections, ColorMap colorMap, BigVector jobMapBlockOffset)
		{
			// All of these mapSections are new and have the same jobMapBlockOffset as the one provided to the method.

			var lastSectionWasIncluded = false;

			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					//if (GetAdjustedBlockPositon(mapSection, jobMapBlockOffset, out var blockPosition))
					//{
					//	if (IsBLockVisible(blockPosition, CanvasSizeInBlocks))
					//	{
					//		var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					//		var loc = invertedBlockPos.Scale(BlockSize);

					//		MapSections.Add(mapSection);

					//		_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
					//		_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
					//	}
					//}


					//if (TryGetAdjustedBlockPositon(mapSection, jobMapBlockOffset, out var blockPosition))
					//{
					//	Debug.Assert(mapSection.BlockPosition == blockPosition, $"GetAdjusted does not equal the original. ms: {mapSection.BlockPosition}, jobs: {jobMapBlockOffset}.");
					//}

					var invertedBlockPos = GetInvertedBlockPos(mapSection.BlockPosition);
					var loc = invertedBlockPos.Scale(BlockSize);

					MapSections.Add(mapSection);

					_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
					_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);


					if (mapSection.IsLastSection)
					{
						lastSectionWasIncluded = true;
					}
				}
			}

			return lastSectionWasIncluded;
		}

		private void RedrawSections(ColorMap colorMap, BigVector currentJobMapBlockOffset)
		{
			// The jobMapBlockOffset reflects the current content on the screen and will not change during the lifetime of this method.
			foreach (var mapSection in MapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					if (TryGetAdjustedBlockPositon(mapSection, currentJobMapBlockOffset, out var blockPosition))
					{
						if (IsBLockVisible(blockPosition, CanvasSizeInBlocks))
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);
							var loc = invertedBlockPos.Scale(BlockSize);

							_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
							_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
						}
					}
					else
					{
						Debug.WriteLine($"Not drawing, the MapSectionVectors are empty.");
					}
				}
			}
			
			OnPropertyChanged(nameof(Bitmap));
		}

		private void StopCurrentJobAndClearDisplay()
		{
			if (_currentMapLoaderJobNumber != null)
			{
				_mapLoaderManager.StopJob(_currentMapLoaderJobNumber.Value);
				_jobMapOffsets.Remove(_currentMapLoaderJobNumber.Value);

				_currentMapLoaderJobNumber = null;
			}

			foreach (var kvp in _jobMapOffsets)
			{
				_mapLoaderManager.StopJob(kvp.Key);
			}

			_jobMapOffsets.Clear();
			ClearMapSectionsAndBitmap();
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

		public void ClearMapSectionsAndBitmap(int? mapLoaderJobNumber = null)
		{
			ClearBitmap(_bitmap);

			if (mapLoaderJobNumber.HasValue)
			{
				var sectionsToRemove = MapSections.Where(x => x.JobNumber == mapLoaderJobNumber.Value).ToList();

				foreach (var ms in sectionsToRemove)
				{
					MapSections.Remove(ms);
					_mapSectionHelper.ReturnMapSection(ms);
				}
			}
			else
			{
				foreach (var ms in MapSections)
				{
					_mapSectionHelper.ReturnMapSection(ms);
				}

				MapSections.Clear();
			}
		}

		private bool ShouldAttemptToReuseLoadedSections(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings newJob)
		{
			//if (MapSections.Count == 0 || previousJob is null)
			//{
			//	return false;
			//}

			//if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
			//{
			//	return false;
			//}

			////var jobSpd = RNormalizer.Normalize(newJob.MapAreaInfo.Subdivision.SamplePointDelta, previousJob.MapAreaInfo.Subdivision.SamplePointDelta, out var previousSpd);
			////return jobSpd == previousSpd;

			//var inSameSubdivision = newJob.MapAreaInfo.Subdivision.Id == previousJob.MapAreaInfo.Subdivision.Id;

			//return inSameSubdivision;

			return false;
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

		#region Bitmap Methods

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

			return result;
		}

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			var zeros = GetPixelsToClear(bitmap.PixelWidth * bitmap.PixelHeight * 4);
			var rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);

			bitmap.WritePixels(rect, zeros, rect.Width * 4, 0);
		}

		private byte[] GetPixelsToClear(int length)
		{
			if (_pixelsToClear.Length != length)
			{
				_pixelsToClear = new byte[length];
			}

			return _pixelsToClear;
		}

		private WriteableBitmap CreateBitmap(SizeInt size)
		{
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

			return result;
		}

		private void GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			// The current content of the screen may change from invocation to invocation of this method, but will not change while the _paintLocker is held.
			bool jobIsCompleted = false;

			lock (_paintLocker)
			{
				if (_currentJobAreaAndCalcSettings == null)
				{
					return;
				}

				var currentMapBlockOffset = _currentJobAreaAndCalcSettings.MapAreaInfo.MapBlockOffset;

				if (TryGetAdjustedBlockPositon(mapSection, currentMapBlockOffset, out var blockPosition))
				{
					if (IsBLockVisible(blockPosition, CanvasSizeInBlocks))
					{
						MapSections.Add(mapSection);

						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);
							var loc = invertedBlockPos.Scale(BlockSize);

							_mapSectionHelper.LoadPixelArray(mapSectionVectors, _colorMap, !mapSection.IsInverted);
							_bitmap.WritePixels(BlockRect, mapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
						}
					}
				}
				else
				{
					Debug.WriteLine($"Not drawing map section: {mapSection} with adjusted block position: {blockPosition} for job number = {mapSection.JobNumber}.");
					_mapSectionHelper.ReturnMapSection(mapSection);
				}

				if (mapSection.IsLastSection)
				{
					jobIsCompleted = true;
				}
			}

			if (jobIsCompleted)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
				OnPropertyChanged(nameof(Bitmap));
			}
		}

		private bool TryGetAdjustedBlockPositon(MapSection mapSection, BigVector mapBlockOffset, out PointInt blockPosition)
		{
			blockPosition = new PointInt();
			var result = false;

			if (_jobMapOffsets.TryGetValue(mapSection.JobNumber, out var thisSectionsMapBlockOffset))
			{
				var df = thisSectionsMapBlockOffset.Diff(mapBlockOffset);

				if (df.IsZero())
				{
					blockPosition = mapSection.BlockPosition;
					result = true;
				}
				else
				{
					if (int.TryParse(df.X.ToString(), out int x))
					{
						if (int.TryParse(df.Y.ToString(), out int y))
						{
							var offset = new VectorInt(x, y);
							blockPosition = mapSection.BlockPosition.Translate(offset);
							result = true;
						}
					}
				}
			}

			return result;
		}

		private bool IsBLockVisible(PointInt blockPosition, SizeInt canvasSizeInBlocks)
		{
			if (blockPosition.X < 0 || blockPosition.Y < 0)
			{
				return false;
			}

			if (blockPosition.X > canvasSizeInBlocks.Width || blockPosition.Y > canvasSizeInBlocks.Height)
			{
				return false;
			}

			return true;
		}

		private bool IsCanvasSizeInWBsReasonable(SizeInt canvasSizeInWholeBlocks)
		{
			var result = !(canvasSizeInWholeBlocks.Width > 50 || canvasSizeInWholeBlocks.Height > 50);
			return result;
		}

		private void GetAndPlacePixelsExp(WriteableBitmap bitmap, PointInt blockPosition, MapSectionVectors mapSectionVectors, ColorMap colorMap, bool isInverted, bool useEscapeVelocities)
		{
			if (useEscapeVelocities)
			{
				Debug.WriteLine("UseEscapeVelocities is not supported. Resetting value.");
				useEscapeVelocities = false;
			}

			var invertedBlockPos = GetInvertedBlockPos(blockPosition);
			var loc = invertedBlockPos.Scale(BlockSize);

			_mapSectionHelper.FillBackBuffer(bitmap.BackBuffer, bitmap.BackBufferStride, loc, BlockSize, mapSectionVectors, colorMap, !isInverted, useEscapeVelocities);

			bitmap.Lock();
			bitmap.AddDirtyRect(new Int32Rect(loc.X, loc.Y, BlockSize.Width, BlockSize.Height));
			bitmap.Unlock();

			OnPropertyChanged(nameof(Bitmap));
		}

		private SizeInt CalculateCanvasSizeInBlocks(SizeDbl logicalContainerSize, VectorInt canvasControlOffset)
		{
			//var result = RMapHelper.GetMapExtentInBlocks(logicalContainerSize.Round(), canvasControlOffset, BlockSize);
			var result = RMapHelper.GetMapExtentInBlocks(logicalContainerSize.Round(), BlockSize);
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
