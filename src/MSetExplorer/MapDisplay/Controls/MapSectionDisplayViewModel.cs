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
using Windows.Media.SpeechRecognition;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapJobHelper2 _mapJobHelper;
		private readonly MapSectionHelper _mapSectionHelper;

		private ObservableCollection<MapSection> _mapSections;

		private ColorBandSet _colorBandSet;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;
		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		private object _paintLocker;

		private SizeDbl _canvasSize;
		private VectorDbl _imageOffset;
		private double _displayZoom;
		//private SizeDbl _logicalDisplaySize;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapJobHelper2 mapJobHelper, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_paintLocker = new object();
			BlockSize = blockSize;

			_bitMapGrid = null;

			ActiveJobNumbers = new List<int>();

			_mapLoaderManager = mapLoaderManager;
			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			_mapSections = new ObservableCollection<MapSection>();

			_colorBandSet = new ColorBandSet();
			_useEscapeVelocities = false;
			_highlightSelectedColorBand = false;
			_currentAreaColorAndCalcSettings = null;

			_canvasSize = new SizeDbl(10, 10);

			_imageOffset = new VectorDbl();

			DisplayZoom = 1.0;
			//_logicalDisplaySize = new SizeDbl();
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
				OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings));
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

		private IBitmapGrid? _bitMapGrid;
		public IBitmapGrid? BitmapGrid
		{
			get => _bitMapGrid;
			set
			{
				_bitMapGrid = value;
			}
		}

		public SizeDbl CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (!value.IsNAN() && value != _canvasSize)
				{
					_canvasSize = value;

					var newJobNumber = HandleDisplaySizeUpdate();
					if (newJobNumber != null)
					{
						ActiveJobNumbers.Add(newJobNumber.Value);
					}

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
					OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
				}
			}
		}

		public VectorDbl ImageOffset
		{
			get => _imageOffset;
			set
			{
				//if (!_registrationComplete)
				//{
				//	ForceImageOffset(value);
				//	_registrationComplete = true;
				//	return;
				//}

				if (ScreenTypeHelper.IsVectorDblChanged(_imageOffset, value))
				{
					//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_imageOffset = value;

					OnPropertyChanged(nameof(IMapDisplayViewModel.ImageOffset));
				}
			}
		}

		//private void ForceImageOffset(VectorDbl value)
		//{
		//	_imageOffset = value;
		//	OnPropertyChanged(nameof(IMapDisplayViewModel.ImageOffset));
		//}

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

			lock (_paintLocker)
			{
				if (newValue != CurrentAreaColorAndCalcSettings)
				{
					var previousValue = CurrentAreaColorAndCalcSettings;

					//if (newValue != null && ScreenTypeHelper.IsSizeDblChanged(new SizeDbl(newValue.MapAreaInfo.CanvasSize), CanvasSize))
					//{
					//	var newAreaColorAndCalcSettings = UpdateSize(newValue, new SizeDbl(newValue.MapAreaInfo.CanvasSize), CanvasSize);
					//	CurrentAreaColorAndCalcSettings = newAreaColorAndCalcSettings;
					//}
					//else
					//{
					//	CurrentAreaColorAndCalcSettings = newValue;
					//}

					CurrentAreaColorAndCalcSettings = newValue;
					ReportSubmitJobDetails(previousValue, newValue);

					newJobNumber = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings);
				}
			}

			return newJobNumber;
		}

		[Conditional("DEBUG")]
		private void ReportSubmitJobDetails(AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings? newValue)
		{
			var currentJobId = previousValue?.OwnerId ?? ObjectId.Empty.ToString();

			if (newValue == null)
			{
				Debug.WriteLine($"MapDisplay is handling SumbitJob. The new value is null. CurrentJobId: {currentJobId}.");
			}
			else
			{
				var newJobId = newValue.OwnerId;

				//if (newValue.MapAreaInfo.Coords != finalValue.MapAreaInfo.Coords)
				//{
				//	Debug.WriteLine($"MapDisplay is handling SumbitJob. Updating the new value's Area using the current Canvas Size. CurrentJobId: {currentJobId}. NewJobId: {newJobId}. " +
				//		$"Old CanvasSize: {newValue.MapAreaInfo.CanvasSize}, Updated CanvasSize: {finalValue.MapAreaInfo.CanvasSize}");
				//}
				//else
				//{
				//	Debug.WriteLine($"MapDisplay is handling SumbitJob. Not adjusting the new value's Area. CurrentJobId: {currentJobId}. NewJobId: {newJobId}.");
				//}

				Debug.WriteLine($"MapDisplay is handling SumbitJob. CurrentJobId: {currentJobId}. NewJobId: {newJobId}.");
			}
		}

		public void CancelJob()
		{
			CurrentAreaColorAndCalcSettings = null;

			lock (_paintLocker)
			{
				StopCurrentJobAndClearDisplay();
			}
		}

		public int? RestartLastJob()
		{
			int? result;
			bool lastSectionWasIncluded;

			lock (_paintLocker)
			{
				var currentJob = CurrentAreaColorAndCalcSettings;

				if (currentJob != null && !currentJob.IsEmpty)
				{
					var screenAreaInfo = GetScreenAreaInfo(currentJob.MapAreaInfo, CanvasSize);
					var newMapSections = _mapLoaderManager.Push(currentJob.OwnerId, currentJob.OwnerType, screenAreaInfo, currentJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

					var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
					Debug.WriteLine($"Restarting paused job: received {newMapSections.Count}, {requestsPending} are being generated.");

					result = newJobNumber;
					lastSectionWasIncluded = BitmapGrid?.DrawSections(newMapSections) ?? false;
				}
				else
				{
					lastSectionWasIncluded = false;
					result = null;
				}
			}

			if (result.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, result.Value);
			}

			return result;
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				BitmapGrid?.ClearDisplay();
			}
		}

		public MapAreaInfo? GetMapAreaInfo()
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				var result = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, CanvasSize);
				return result;
			}
			else
			{
				return null;
			}
		}

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				var screenArea = e.Area;

				if (!e.IsPreview)
				{
					Debug.WriteLine("Here");
				}

				var panAmount = screenArea.GetCenter();
				var factor = 3;

				MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, panAmount, factor, CurrentAreaColorAndCalcSettings.MapAreaInfo, e.IsPreview));
			}
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				var offset = e.Offset;

				// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
				var invOffset = offset.Invert();
				//var screenArea = new RectangleInt(new PointInt(invOffset), CanvasSize.Round());
				MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, invOffset, 1, CurrentAreaColorAndCalcSettings.MapAreaInfo));
			}
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

		//private int? HandleDisplaySizeUpdate(SizeDbl previousSize, SizeDbl newSize)
		//{
		//	int? newJobNumber = null;
		//	bool lastSectionWasIncluded = false;

		//	lock (_paintLocker)
		//	{
		//		if (CurrentAreaColorAndCalcSettings != null)
		//		{
		//			var previousValue = CurrentAreaColorAndCalcSettings;
		//			var newAreaColorAndCalcSettings = UpdateSize(previousValue, previousSize, newSize);
		//			CurrentAreaColorAndCalcSettings = newAreaColorAndCalcSettings;

		//			//ReportNewMapArea(previousValue.MapAreaInfo, newAreaColorAndCalcSettings.MapAreaInfo);
		//			newJobNumber = HandleCurrentJobChanged(previousValue, newAreaColorAndCalcSettings, out lastSectionWasIncluded);
		//		}
		//	}

		//	if (newJobNumber.HasValue && lastSectionWasIncluded)
		//	{
		//		DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
		//	}

		//	return newJobNumber;
		//}

		//private AreaColorAndCalcSettings UpdateSize(AreaColorAndCalcSettings currentAreaColorAndCalcSettings, SizeDbl previousSize, SizeDbl newSize)
		//{
		//	var previousMapAreaInfo = currentAreaColorAndCalcSettings.MapAreaInfo;

		//	var newMapAreaInfo = _mapJobHelper.UpdateSize(previousMapAreaInfo, previousSize, newSize);

		//	var newAreaColorAndCalcSettings = currentAreaColorAndCalcSettings.UpdateWith(newMapAreaInfo);

		//	return newAreaColorAndCalcSettings;
		//}

		private int? HandleDisplaySizeUpdate()
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings != null)
				{
					newJobNumber = ReuseAndLoad(CurrentAreaColorAndCalcSettings, out lastSectionWasIncluded);
				}
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		private int? HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			int? newJobNumber;

			var lastSectionWasIncluded = false;

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
				newJobNumber = null;
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, CanvasSize);

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);
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
				BitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				//BitmapGrid.ImageOffset = new VectorDbl(newJob.MapAreaInfo.CanvasControlOffset);
			}
			
			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

			ColorBandSet = newJob.ColorBandSet;

			var sectionsRemoved = BitmapGrid?.ReDrawSections() ?? 0;

			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, we removed {sectionsToRemove.Count} ReDraw removed {sectionsRemoved}. Keeping {MapSections.Count}. {_mapSectionHelper.MapSectionsVectorsInPool} MapSection in the pool.");

			int? result;

			if (sectionsToLoad.Count > 0)
			{
				var newMapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);
				var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
				Debug.WriteLine($"Fetching New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

				lastSectionWasIncluded = BitmapGrid?.DrawSections(newMapSections) ?? false;

				result = newJobNumber;
				ActiveJobNumbers.Add(newJobNumber);
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
			var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, CanvasSize);

			var newMapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, screenAreaInfo, newJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

			var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
			Debug.WriteLine($"Clearing Display and Loading New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

			if (BitmapGrid != null)
			{
				BitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				//BitmapGrid.ImageOffset = new VectorDbl(newJob.MapAreaInfo.CanvasControlOffset);
			}

			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

			ColorBandSet = newJob.ColorBandSet;
			lastSectionWasIncluded = BitmapGrid?.DrawSections(newMapSections) ?? false;

			ActiveJobNumbers.Add(newJobNumber);
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

		private MapAreaInfo GetScreenAreaInfo(MapAreaInfo2 canonicalMapAreaInfo, SizeDbl canvasSize)
		{
			var mapAreaInfoV1 = _mapJobHelper.Convert(canonicalMapAreaInfo, canvasSize.Round());

			var mapAreaInfoV2 = MapJobHelper2.Convert(mapAreaInfoV1);

			CompareMapAreaAfterRoundTrip(canonicalMapAreaInfo, mapAreaInfoV2, mapAreaInfoV1);

			return mapAreaInfoV1;
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

		private void ReportNewMapArea(MapAreaInfo previousValue, MapAreaInfo newValue)
		{
			Debug.WriteLine($"MapDisplay is handling DisplaySizeUpdate. " +
				$"Previous Size: {previousValue.CanvasSize}. Pos: {previousValue.Coords.Position}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"New Size: {newValue.CanvasSize}. Pos: {newValue.Coords.Position}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}.");

			Debug.WriteLine($"UpdateSize is moving the pos from {previousValue.Coords.Position} to {newValue.Coords.Position}.");
		}

		private void CompareMapAreaAfterRoundTrip(MapAreaInfo2 previousValue, MapAreaInfo2 newValue, MapAreaInfo middleValue)
		{
			Debug.WriteLine($"MapDisplay is RoundTripping MapAreaInfo" +
				$"\nPrevious Scale: {previousValue.SamplePointDelta.Width}. Pos: {previousValue.MapCenter}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"\nNew Scale     : {newValue.SamplePointDelta.Width}. Pos: {newValue.MapCenter}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}" +
				$"\nIntermediate  : {middleValue.SamplePointDelta.Width}. Pos: {middleValue.MapPosition}. MapOffset: {middleValue.MapBlockOffset}. ImageOffset: {middleValue.CanvasControlOffset} Size: {middleValue.CanvasSize}.");
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
