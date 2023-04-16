using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		//private static readonly bool KEEP_DISPLAY_SQUARE = false;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionHelper _mapSectionHelper;

		//private BitmapGrid _bitmapGrid;

		private ObservableCollection<MapSection> _mapSections;

		private object _paintLocker;

		private SizeDbl _canvasSize;
		private double _displayZoom;
		//private SizeDbl _logicalDisplaySize;

		//private BigVector _mapBlockOffset;
		private VectorDbl _imageOffset;

		private ColorBandSet _colorBandSet;
		private bool _useEscapeVelocities;

		private bool _highlightSelectedColorBand;

		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_paintLocker = new object();

			_mapLoaderManager = mapLoaderManager;
			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;
			BlockSize = blockSize;

			ActiveJobNumbers = new List<int>();

			//Action<WriteableBitmap> updateBitmapAction = x => { Bitmap = x; };
			//Action<MapSection> disposeMapSection = x => { _mapSectionHelper.ReturnMapSection(x); };

			//_bitmapGrid = new BitmapGrid(BlockSize, updateBitmapAction, disposeMapSection);

			//DisposeMapSection = x => { _mapSectionHelper.ReturnMapSection(x); };

			_mapSections = new ObservableCollection<MapSection>();

			_colorBandSet = new ColorBandSet();
			_useEscapeVelocities = false;
			_highlightSelectedColorBand = false;
			_currentAreaColorAndCalcSettings = null;

			_canvasSize = new SizeDbl(10, 10);
			//_logicalDisplaySize = new SizeDbl();

			//_mapBlockOffset = new BigVector();
			_imageOffset = new VectorDbl();

			DisplayZoom = 1.0;
		}

		#endregion

		#region Events

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		#endregion

		#region Public Properties - Content

		public SizeInt BlockSize { get; init; }

		public List<int> ActiveJobNumbers { get; init; }

		public ObservableCollection<MapSection> MapSections
		{
			get { return _mapSections; }
			set
			{
				if (value != _mapSections)
				{
					Debug.WriteLine($"ViewModel is having its MapSections set to a new value.");
					_mapSections = value;
					OnPropertyChanged(nameof(IMapDisplayViewModel.MapSections));
				}
				else
				{
					Debug.WriteLine($"ViewModel is having its MapSections set. The value is the same.");
				}
			}
		}

		public AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings
		{
			get => _currentAreaColorAndCalcSettings;
			private set
			{
				_currentAreaColorAndCalcSettings = value?.Clone() ?? null;
				//OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings));
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				lock (_paintLocker)
				{
					_colorBandSet = value;
					OnPropertyChanged(nameof(IMapDisplayViewModel.ColorBandSet));
				}
			}
		}

		public bool UseEscapeVelocities
		{
			get => _useEscapeVelocities;
			set 
			{
				lock (_paintLocker)
				{
					_useEscapeVelocities = value;
					OnPropertyChanged(nameof(IMapDisplayViewModel.UseEscapeVelocities));
				}
			}
		}

		public bool HighlightSelectedColorBand
		{
			get => _highlightSelectedColorBand;
			set
			{
				lock (_paintLocker)
				{
					_highlightSelectedColorBand = value;
					OnPropertyChanged(nameof(IMapDisplayViewModel.HighlightSelectedColorBand));
				}
			}
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public Action<MapSection> DisposeMapSection => DisposeMapSectionInternal;

		public IBitmapGrid? BitmapGrid { get; set; }

		//public WriteableBitmap Bitmap
		//{
		//	get => _bitmapGrid.Bitmap;
		//	set
		//	{
		//		//_bitmap = value;
		//		OnPropertyChanged();
		//	}
		//}

		public SizeDbl CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (!value.IsNAN() && value != _canvasSize)
				{
					var previousValue = _canvasSize;
					_canvasSize = value;

					// TODO: BMx
					//var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(value, BlockSize, KEEP_DISPLAY_SQUARE);
					//CanvasSizeInBlocks = sizeInWholeBlocks;

					var newJobNumber = HandleDisplaySizeUpdate(previousValue, _canvasSize);
					if (newJobNumber != null)
					{
						ActiveJobNumbers.Add(newJobNumber.Value);
					}

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
					OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
				}
			}
		}

		//public SizeInt CanvasSizeInBlocks
		//{
		//	get => _bitmapGrid.CanvasSizeInBlocks;
		//	set => _bitmapGrid.CanvasSizeInBlocks = value;
		//}

		//public BigVector MapBlockOffset
		//{
		//	get => BitmapGrid?.MapBlockOffset ?? new BigVector();
		//	set
		//	{
		//		if (BitmapGrid != null)
		//		{
		//			BitmapGrid.MapBlockOffset = value;
		//		}
		//	}
		//}

		public VectorDbl ImageOffset
		{
			get => _imageOffset;
			set
			{
				if (value != _imageOffset)
				{
					//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_imageOffset = value;

					OnPropertyChanged(nameof(IMapDisplayViewModel.ImageOffset));
				}
			}
		}

		public SizeDbl LogicalDisplaySize => CanvasSize;

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
					//LogicalDisplaySize = CanvasSize;


					// TODO: DisplayZoom property is not being used on the MapSectionDisplayViewModel
					//OnPropertyChanged();
				}
			}
		}

		//public SizeDbl LogicalDisplaySize
		//{
		//	get => _logicalDisplaySize;
		//	private set
		//	{
		//		if (_logicalDisplaySize != value)
		//		{
		//			_logicalDisplaySize = value;

		//			Debug.WriteLine($"MapDisplay's Logical DisplaySize is now {value}.");

		//			OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
		//		}
		//	}
		//}

		#endregion

		#region Public Methods

		public int? SubmitJob(AreaColorAndCalcSettings newValue)
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				if (newValue != CurrentAreaColorAndCalcSettings)
				{
					var previousValue = CurrentAreaColorAndCalcSettings;
					CurrentAreaColorAndCalcSettings = newValue;

					ReportNewJobOldJob("SubmitJob", previousValue, newValue);

					newJobNumber = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings, out lastSectionWasIncluded);
				}
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		[Conditional("DEBUG")]
		private void ReportNewJobOldJob(string operation, AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings newValue)
		{
			var currentJobId = previousValue?.OwnerId ?? ObjectId.Empty.ToString();
			var currentSamplePointDelta = previousValue?.MapAreaInfo.Subdivision.SamplePointDelta ?? new RSize();
			var newJobId = newValue.OwnerId;
			var newSamplePointDelta = newValue.MapAreaInfo.Subdivision.SamplePointDelta;

			Debug.WriteLine($"MapDisplay is handling {operation}. CurrentJobId: {currentJobId} ({currentSamplePointDelta}), NewJobId: {newJobId} ({newSamplePointDelta}).");
		}

		public void CancelJob()
		{
			CurrentAreaColorAndCalcSettings = null;

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
					var newMapSections = _mapLoaderManager.Push(currentJob.OwnerId, currentJob.OwnerType, currentJob.MapAreaInfo, currentJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

					var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
					Debug.WriteLine($"Restarting paused job: received {newMapSections.Count}, {requestsPending} are being generated.");

					//_bitmapGrid.SetColorBandSet(currentJob.ColorBandSet);
					//MapBlockOffset = currentJob.MapAreaInfo.MapBlockOffset;
					//ImageOffset = new VectorDbl(currentJob.MapAreaInfo.CanvasControlOffset);

					//_ = _bitmapGrid.DrawSections(newMapSections);
					_ = BitmapGrid?.DrawSections(newMapSections);
				}
			}
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				//_bitmapGrid.ClearDisplay();
				BitmapGrid?.ClearDisplay();
			}
		}

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var screenArea = e.Area;
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, screenArea, CanvasSize, e.IsPreview));
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var screenArea = new RectangleInt(new PointInt(invOffset), CanvasSize.Round());
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, screenArea, CanvasSize));
		}

		#endregion

		#region Event Handlers

		private void MapSectionReady(MapSection mapSection)
		{
			BitmapGrid?.Dispatcher.Invoke(GetAndPlacePixelsWrapper, new object[] { mapSection });
		}

		private void GetAndPlacePixelsWrapper(MapSection mapSection)
		{

			if (BitmapGrid != null && mapSection.MapSectionVectors != null)
			{
				lock (_paintLocker)
				{
					BitmapGrid.GetAndPlacePixels(mapSection, mapSection.MapSectionVectors);
				}
			}

			if (mapSection.IsLastSection)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
			}
		}

		#endregion

		#region Private Methods

		private int? HandleDisplaySizeUpdate(SizeDbl previousSize, SizeDbl newSize)
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				if (_currentAreaColorAndCalcSettings != null)
				{
					var previousValue = _currentAreaColorAndCalcSettings;

					var prevMapAreaInfo = previousValue.MapAreaInfo;
					var mapPosition = prevMapAreaInfo.Coords.Position;
					var subdivision = prevMapAreaInfo.Subdivision;

					var newMapAreaInfo = _mapJobHelper.GetMapAreaInfo(mapPosition, subdivision, previousSize, newSize);
					var newAreaColorAndCalcSettings = _currentAreaColorAndCalcSettings.UpdateWith(newMapAreaInfo);
					CurrentAreaColorAndCalcSettings = newAreaColorAndCalcSettings;

					//ReportNewMapArea(prevMapAreaInfo, newMapAreaInfo);
					newJobNumber = HandleCurrentJobChanged(previousValue, newAreaColorAndCalcSettings, out lastSectionWasIncluded);
				}
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		private void ReportNewMapArea(MapAreaInfo previousValue, MapAreaInfo newValue)
		{
			Debug.WriteLine($"MapDisplay is handling DisplaySizeUpdate. " +
				$"Previous Size: {previousValue.CanvasSize}. Pos: {previousValue.Coords.Position}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"New Size: {newValue.CanvasSize}. Pos: {newValue.Coords.Position}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}.");
		}

		private int? HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob, out bool lastSectionWasIncluded)
		{
			int? newJobNumber;

			lastSectionWasIncluded = false;

			if (newJob != null && !newJob.IsEmpty)
			{
				if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
				{
					newJobNumber = ReuseAndLoad(newJob, out lastSectionWasIncluded);
					if (newJobNumber.HasValue)
					{
						ActiveJobNumbers.Add(newJobNumber.Value);
					}
				}
				else
				{
					StopCurrentJobAndClearDisplay();
					newJobNumber = DiscardAndLoad(newJob, out lastSectionWasIncluded);
					ActiveJobNumbers.Add(newJobNumber.Value);
				}
			}
			else
			{
				StopCurrentJobAndClearDisplay();
				newJobNumber = null;
			}

			return newJobNumber;
		}

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(newJob.MapAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new ReadOnlyCollection<MapSection>(MapSections);
			var sectionsToLoad = GetSectionsToLoad(sectionsRequired, loadedSections);
			var sectionsToRemove = GetSectionsToRemove(sectionsRequired, loadedSections);

			foreach (var section in sectionsToRemove)
			{
				MapSections.Remove(section);
				_mapSectionHelper.ReturnMapSection(section);
			}

			if (BitmapGrid != null)
			{
				BitmapGrid.MapBlockOffset = newJob.MapAreaInfo.MapBlockOffset;
			}
			
			ImageOffset = new VectorDbl(newJob.MapAreaInfo.CanvasControlOffset);

			//_bitmapGrid.SetColorBandSet(newJob.ColorBandSet);
			ColorBandSet = newJob.ColorBandSet;

			var sectionsRemoved = BitmapGrid?.ReDrawSections() ?? 0;

			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, we removed {sectionsToRemove.Count} ReDraw removed {sectionsRemoved}. Keeping {MapSections.Count}. {_mapSectionHelper.MapSectionsVectorsInPool} MapSection in the pool.");

			int? result;

			if (sectionsToLoad.Count > 0)
			{
				var newMapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);
				var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
				Debug.WriteLine($"Fetching New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

				lastSectionWasIncluded = BitmapGrid?.DrawSections(newMapSections) ?? false;

				result = newJobNumber;
			}
			else
			{
				lastSectionWasIncluded = false;
				result = null;
			}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			var newMapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

			var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
			//Debug.WriteLine($"Clearing Display and Loading New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

			if (BitmapGrid != null)
			{
				BitmapGrid.MapBlockOffset = newJob.MapAreaInfo.MapBlockOffset;
			}

			ImageOffset = new VectorDbl(newJob.MapAreaInfo.CanvasControlOffset);

			//_bitmapGrid.SetColorBandSet(newJob.ColorBandSet);
			//lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);

			ColorBandSet = newJob.ColorBandSet;
			lastSectionWasIncluded = BitmapGrid?.DrawSections(newMapSections) ?? false;

			//lastSectionWasIncluded = newMapSections.Any(x => x.IsLastSection);

			return newJobNumber;
		}

		private void StopCurrentJobAndClearDisplay()
		{
			foreach(var jobNumber in ActiveJobNumbers)
			{
				_mapLoaderManager.StopJob(jobNumber);
			}

			ActiveJobNumbers.Clear();

			//_bitmapGrid.ClearDisplay();

			if (BitmapGrid != null)
			{
				BitmapGrid.ClearDisplay();
			}
			else
			{
				Debug.WriteLine($"The BitmapGrid is null on call to StopCurrentJobAndClearDisplay.");
			}
		}

		private bool ShouldAttemptToReuseLoadedSections(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings newJob)
		{
			if (MapSections.Count == 0 || previousJob is null)
			{
				return false;
			}

			if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
			{
				return false;
			}

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

		private void DisposeMapSectionInternal(MapSection mapSection)
		{
			var refCount = mapSection.MapSectionVectors?.ReferenceCount ?? 0;

			if (refCount > 1)
			{
				Debug.WriteLine("WARNING: MapSectionDisplayViewModel is Disposing a MapSection whose reference count > 1.");
			}

			_mapSectionHelper.ReturnMapSection(mapSection);
		}


		//private List<MapSection> GetSectionsToLoadX(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent, out List<MapSection> sectionsToRemove)
		//{
		//	var result = new List<MapSection>();
		//	sectionsToRemove = new List<MapSection>();

		//	foreach (var ms in sectionsPresent)
		//	{
		//		var stillNeed = sectionsNeeded.Any(presentSection => presentSection == ms && presentSection.TargetIterations == ms.TargetIterations);

		//		if (!stillNeed)
		//		{
		//			sectionsToRemove.Add(ms);
		//		}

		//	}

		//	//var result = sectionsNeeded.Where(
		//	//	neededSection => !sectionsPresent.Any(
		//	//		presentSection => presentSection == neededSection
		//	//		&& presentSection.TargetIterations == neededSection.TargetIterations
		//	//		)
		//	//	).ToList();

		//	return result;
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
