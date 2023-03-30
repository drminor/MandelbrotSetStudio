using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	//delegate void PlaceBitmap(WriteableBitmap writeableBitmap, MapSection mapSection, byte[] pixels);

	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		private static bool _keepDisplaySquare;

		private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private int? _currentMapLoaderJobNumber;

		//private readonly DrawingGroup _drawingGroup;
		//private readonly ScaleTransform _scaleTransform;

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

		private object _paintLocker;

		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear = new byte[0];

		private SizeInt _canvasSizeInBlocks;
		private SizeInt _allocatedBlocks;
		private int _maxYPtr;
		
		#endregion

		#region Constructor

		public MapDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_bitmap = CreateBitmap(new SizeInt(10));
			_maxYPtr = 1;

			_useEscapeVelocities = true;
			_keepDisplaySquare = true;

			_synchronizationContext = SynchronizationContext.Current;
			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;
			_currentMapLoaderJobNumber = null;
			//_mapLoaderManager.MapSectionReady += MapSectionReady;

			BlockSize = blockSize;
			BlockRect = new Int32Rect(0, 0, BlockSize.Width, BlockSize.Height);

			//_drawingGroup = new DrawingGroup();
			//_scaleTransform = new ScaleTransform();
			//_drawingGroup.Transform = _scaleTransform;
			//_screenSectionCollection = new ScreenSectionCollection(_drawingGroup, BlockSize, INITIAL_SCREEN_SECTION_ALLOCATION);
			//ImageSource = new DrawingImage(_drawingGroup);

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

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; init; }
		private Int32Rect BlockRect { get; init; }

		public ObservableCollection<MapSection> MapSections { get; init; }

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

					RedrawSections(_colorMap);
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
						RedrawSections(_colorMap);
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
						RedrawSections(_colorMap);
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
					ClearDisplay(mapLoaderJobNumber: null);

					//Debug.WriteLine($"The DrawingGroup has {_screenSectionCollection.CurrentDrawingGroupCnt} item.");

					_displayZoom = value;

					// TODO: scc -- Need to place the WriteableBitmap within a DrawingGroup.
					//_scaleTransform.ScaleX = 1 / _displayZoom;
					//_scaleTransform.ScaleY = 1 / _displayZoom;

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

		//private SizeInt CalculateCanvasSizeInBlocks(SizeDbl logicalContainerSize, VectorInt canvasControlOffset)
		//{
		//	//var result = RMapHelper.GetMapExtentInBlocks(logicalContainerSize.Round(), canvasControlOffset, BlockSize);
		//	var result = RMapHelper.GetMapExtentInBlocks(logicalContainerSize.Round(), BlockSize);
		//	return result;
		//}

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

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				OnPropertyChanged();
			}
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

		public void RestartLastJob()
		{
			var currentJob = CurrentAreaColorAndCalcSettings;

			if (currentJob != null && !currentJob.IsEmpty)
			{
				SubmitJob(currentJob);
			}
		}

		public void ClearDisplay(int? mapLoaderJobNumber)
		{
			lock (_paintLocker)
			{
				ClearBitmap(_bitmap);

				if (mapLoaderJobNumber.HasValue)
				{
					//var sectionsToRemove = new List<MapSection>();
					//foreach (var ms in MapSections)
					//{
					//	if (ms.JobNumber == mapLoaderJobNumber.Value)
					//	{
					//		sectionsToRemove.Add(ms);
					//	}
					//}

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

				OnPropertyChanged(nameof(Bitmap));
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
			_bitmap.Dispatcher.Invoke(GetAndPlacePixels, new object[] { mapSection });
		}

		#endregion

		#region Private Methods

		private void GetAndPlacePixels(MapSection mapSection)
		{
			lock (_paintLocker)
			{
				if (mapSection.JobNumber == _currentMapLoaderJobNumber)
				{
					MapSections.Add(mapSection);

					if (_colorMap != null && mapSection.MapSectionVectors != null)
					{
						var invertedBlockPos = GetInvertedBlockPos(mapSection.BlockPosition);
						var loc = invertedBlockPos.Scale(BlockSize);

						_mapSectionHelper.LoadPixelArray(mapSection, _colorMap);

						_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);

						//OnPropertyChanged(nameof(Bitmap));
					}

					if (mapSection.IsLastSection)
					{
						DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
						OnPropertyChanged(nameof(Bitmap));
					}
				}
				else
				{
					Debug.WriteLine($"Not drawing map section: {mapSection}. The job number = {mapSection.JobNumber}, our job number = {_currentMapLoaderJobNumber}.");
					_mapSectionHelper.ReturnMapSection(mapSection);
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

		private void HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			//Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {newJob?.Id ?? ObjectId.Empty}");
			if (_currentMapLoaderJobNumber != null)
			{
				_mapLoaderManager.StopJob(_currentMapLoaderJobNumber.Value);

				//Debug.WriteLine($"Clearing Display. TransformType: {newJob.TransformType}.");
				ClearDisplay(_currentMapLoaderJobNumber);

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
				//ClearDisplay();

				CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

				if (newJob.ColorBandSet != ColorBandSet)
				{
					_colorMap = LoadColorMap(newJob.ColorBandSet);
				}

				_currentMapLoaderJobNumber = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady);
			}
			else
			{
				//ClearDisplay();
			}
		}

		private void RedrawSections(ColorMap colorMap)
		{
			lock (_paintLocker)
			{
				foreach (var mapSection in MapSections)
				{
					if (mapSection.MapSectionVectors != null)
					{
						var invertedBlockPos = GetInvertedBlockPos(mapSection.BlockPosition);
						var loc = invertedBlockPos.Scale(BlockSize);

						_mapSectionHelper.LoadPixelArray(mapSection, colorMap);

						_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);

					}
					else
					{
						Debug.WriteLine($"Not drawing, the MapSectionVectors are empty.");
					}
				}
			}

			OnPropertyChanged(nameof(Bitmap));
		}
		
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

		#endregion

		#region Old Update Bitmap Routines

		//private void GetAndPlacePixelsOld(WriteableBitmap bitmap, PointInt blockPosition, MapSectionVectors mapSectionVectors, ColorMap colorMap, bool isInverted, bool useEscapeVelocities)
		//{
		//	var invertedBlockPos = GetInvertedBlockPos(blockPosition);
		//	var loc = invertedBlockPos.Scale(BlockSize);

		//	var pixels = _mapSectionHelper.GetPixelArray(mapSectionVectors, BlockSize, colorMap, !isInverted, useEscapeVelocities);

		//	bitmap.WritePixels(BlockRect, pixels, BlockRect.Width * 4, loc.X, loc.Y);

		//	//OnPropertyChanged(nameof(Bitmap));
		//}

		//private void GetAndPlacePixelsExp(WriteableBitmap bitmap, PointInt blockPosition, MapSectionVectors mapSectionVectors, ColorMap colorMap, bool isInverted, bool useEscapeVelocities)
		//{
		//	if (useEscapeVelocities)
		//	{
		//		Debug.WriteLine("UseEscapeVelocities is not supported. Resetting value.");
		//		useEscapeVelocities = false;
		//	}

		//	var invertedBlockPos = GetInvertedBlockPos(blockPosition);
		//	var loc = invertedBlockPos.Scale(BlockSize);

		//	//var pixels = _mapSectionHelper.GetPixelArray(mapSectionVectors, BlockSize, colorMap, !isInverted, useEscapeVelocities);
		//	//bitmap.WritePixels(BlockRect, pixels, BlockRect.Width * 4, loc.X, loc.Y);

		//	_mapSectionHelper.FillBackBuffer(_bitmap.BackBuffer, _bitmap.BackBufferStride, loc, BlockSize, mapSectionVectors, colorMap, !isInverted, useEscapeVelocities);

		//	bitmap.Lock();
		//	bitmap.AddDirtyRect(new Int32Rect(loc.X, loc.Y, BlockSize.Width, BlockSize.Height));
		//	bitmap.Unlock();

		//	OnPropertyChanged(nameof(Bitmap));
		//}

		//private void PlacePixels(WriteableBitmap bitmap, MapSection mapSection, byte[] pixels)
		//{
		//	var invertedBlockPos = GetInvertedBlockPos(mapSection.BlockPosition);
		//	var loc = invertedBlockPos.Scale(BlockSize);
		//	bitmap.WritePixels(BlockRect, pixels, BlockRect.Width * 4, loc.X, loc.Y);

		//	OnPropertyChanged(nameof(Bitmap));
		//}

		#endregion

		#region Old Handle Map Changed Handlers

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

		//private void RedrawSections(IEnumerable<MapSection> source)
		//{
		//	Debug.WriteLine($"Hiding all screen sections and redrawing {source.Count()}.");
		//	ClearDisplay(mapLoaderJobNumber: null);

		//	foreach (var mapSection in source)
		//	{
		//		//Debug.WriteLine($"About to redraw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");

		//		//_screenSectionCollection.Redraw(mapSection.BlockPosition);

		//		//Thread.Sleep(200);
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
