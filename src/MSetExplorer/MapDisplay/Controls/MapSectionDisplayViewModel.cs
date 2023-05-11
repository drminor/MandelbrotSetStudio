using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionBuilder _mapSectionHelper;

		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		private BoundedMapArea? _boundedMapArea;
		private MapAreaInfo? _latestMapAreaInfo;

		private object _paintLocker;

		private BitmapGrid _bitmapGrid;

		private SizeDbl _viewportSize;
		private VectorDbl _imageOffset;

		private VectorDbl _displayPosition;

		private SizeDbl _unscaledExtent;

		private double _displayZoom;
		private double _minimumDisplayZoom;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionBuilder mapSectionHelper, SizeInt blockSize)
		{
			_unscaledExtent = new SizeDbl();
			_paintLocker = new object();
			BlockSize = blockSize;

			MapSections = new ObservableCollection<MapSection>();

			_bitmapGrid = new BitmapGrid(MapSections, new SizeDbl(128), DisposeMapSection, OnBitmapUpdate, blockSize);

			ActiveJobNumbers = new List<int>();

			_mapLoaderManager = mapLoaderManager;
			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			_currentAreaColorAndCalcSettings = null;
			_latestMapAreaInfo = null;

			_imageOffset = new VectorDbl();

			_displayPosition = new VectorDbl();

			_displayZoom = 1;
			_minimumDisplayZoom = 0.0625;
		}

		#endregion

		#region Events

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		#endregion

		#region Public Properties - Content

		public ObservableCollection<MapSection> MapSections { get; init; }

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
			get => _bitmapGrid.ColorBandSet;
			set => _bitmapGrid.ColorBandSet = value;
		}

		public ColorBand? CurrentColorBand
		{
			get => _bitmapGrid.CurrentColorBand;
			set => _bitmapGrid.CurrentColorBand = value;
		}

		public bool UseEscapeVelocities
		{
			get => _bitmapGrid.UseEscapeVelocities;
			set => _bitmapGrid.UseEscapeVelocities = value;
		}

		public bool HighlightSelectedColorBand
		{
			get => _bitmapGrid.HighlightSelectedColorBand;
			set => _bitmapGrid.HighlightSelectedColorBand = value;
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; init; }

		public List<int> ActiveJobNumbers { get; init; }

		public ImageSource ImageSource
		{
			get => _bitmapGrid.Bitmap;
			private set
			{
				// This is called by the BitmapGrid, to let us know that we need to raise the OnPropertyChanged event.

				Debug.WriteLine($"The MapSectionViewModel's ImageSource is being set to value: {value}.");
				OnPropertyChanged(nameof(IMapDisplayViewModel.ImageSource));
			}
		}

		public SizeDbl ViewportSize
		{
			get =>_viewportSize;
			private set
			{
				_viewportSize = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.ViewportSize));
			}
		}

		public VectorDbl ImageOffset
		{
			get => _imageOffset;
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(_imageOffset, value))
				{
					//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_imageOffset = value;

					OnPropertyChanged(nameof(IMapDisplayViewModel.ImageOffset));
				}
			}
		}

		public MapAreaInfo? LastMapAreaInfo
		{
			get => _latestMapAreaInfo;
			private set { _latestMapAreaInfo = value; }
		}

		#endregion

		#region Public Properties - Scroll

		public bool IsBound => _boundedMapArea != null;

		public SizeDbl UnscaledExtent
		{
			get => _unscaledExtent;

			private set
			{
				if (value != _unscaledExtent)
				{
					_unscaledExtent = value;

					// Let the BitmapGridControl know the entire size.
					OnPropertyChanged(nameof(IMapDisplayViewModel.UnscaledExtent));
				}
			}
		}

		public VectorDbl DisplayPosition
		{
			get => _displayPosition;
			private set => _displayPosition = value;
		}

		/*

		TODO: Update MapSectionDisplayControl with logic that 
		sets the DisplayZoom and MinimumDisplayZoom properties.
		Verify that the PanAndZoom control bindings work and these updates propagate to the 
		MapDisplayZoom control via the ZoomSlider 'mediator.'

		*/

		public double DisplayZoom
		{
			get => _displayZoom;
			private set
			{
				_displayZoom = value;
				//// Value between 0.0 and 1.0
				//// 1.0 presents 1 map "pixel" to 1 screen pixel
				//// 0.5 presents 2 map "pixels" to 1 screen pixel

				////if (Math.Abs(value - DisplayZoom) > 0.001)
				////{
				////	_displayZoom = Math.Min(MaximumDisplayZoom, value);

				////	MapDisplayViewModel.DisplayZoom = _displayZoom;

				////	Debug.WriteLine($"The DispZoom is {DisplayZoom}.");
				////	OnPropertyChanged(nameof(IMapScrollViewModel.DisplayZoom));
				////}

				//var previousValue = _displayZoom;

				//_displayZoom = Math.Min(MaximumDisplayZoom, value);

				////MapDisplayViewModel.DisplayZoom = _displayZoom;

				//Debug.WriteLine($"The MapSectionViewModel's DisplayZoom is being updated to {DisplayZoom}, the previous value is {previousValue}.");
				//// Log: Add Spacer
				//Debug.WriteLine("\n\n");
				//OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayZoom));
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			private set
			{
				_minimumDisplayZoom = value;
				//if (Math.Abs(value - _maximumDisplayZoom) > 0.001)
				//{
				//	_maximumDisplayZoom = value;

				//	if (DisplayZoom > MaximumDisplayZoom)
				//	{
				//		Debug.WriteLine($"The MapSectionViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being adjusted to be less or equal to this.");
				//		DisplayZoom = MaximumDisplayZoom;
				//	}
				//	else
				//	{
				//		Debug.WriteLine($"The MapSectionViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being kept the same.");
				//	}

				//	OnPropertyChanged(nameof(IMapDisplayViewModel.MaximumDisplayZoom));
				//}
			}
		}

		public Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		#endregion

		#region Public Methods

		public int? SubmitJob(AreaColorAndCalcSettings newValue)
		{
			return SubmitJob(newValue, posterSize: null);
		}

		public int? SubmitJob(AreaColorAndCalcSettings newValue, SizeInt posterSize)
		{
			return SubmitJob(newValue, (SizeInt?) posterSize);
		}

		private int? SubmitJob(AreaColorAndCalcSettings newValue, SizeInt? posterSize)
		{
			CheckBlockSize(newValue);

			lock (_paintLocker)
			{
				CheckVPSize();

				int? newJobNumber = null;

				if (posterSize != null)
				{
					// Bounded
					UnscaledExtent = new SizeDbl(posterSize.Value);

					// Save the MapAreaInfo for the entire poster.
					_boundedMapArea = new BoundedMapArea(_mapJobHelper, newValue, ViewportSize, posterSize.Value);

					// Get the MapAreaInfo subset for the upper, left-hand corner.
					var displayPosition = new VectorDbl(0, 0);

					var mapAreaInfo2Subset = _boundedMapArea.GetView(displayPosition);

					//newJobNumber = HandleDisplayPositionChange(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);
					//newJobNumber = ReuseAndLoad(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

					StopCurrentJobAndClearDisplay();
					//var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);
					newJobNumber = DiscardAndLoad(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

					if (newJobNumber.HasValue && lastSectionWasIncluded)
					{
						DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
					}
				}
				else
				{
					// Unbounded

					UnscaledExtent = SizeDbl.Zero;

					_boundedMapArea = null;

					if (newValue != CurrentAreaColorAndCalcSettings)
					{
						var previousValue = CurrentAreaColorAndCalcSettings;

						CurrentAreaColorAndCalcSettings = newValue;
						ReportSubmitJobDetails(previousValue, newValue);

						newJobNumber = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings);
					}
				}

				return newJobNumber;
			}
		}

		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			if (_boundedMapArea == null)
			{
				throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
			}

			var newJobNumber = LoadNewView(_boundedMapArea, contentViewportSize, contentOffset, contentScale);
			return newJobNumber;
		}

		public int? UpdateViewportSize(SizeDbl newValue)
		{
			int? newJobNumber = null;

			if (!newValue.IsNAN() && newValue != _viewportSize)
			{
				if (newValue.Width <= 2 || newValue.Height <= 2)
				{
					Debug.WriteLine($"WARNING: MapSectionDisplayViewModel is having its ViewportSize set to {newValue}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}.");
				}
				else
				{
					Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewportSize set to {newValue}. Previously it was {_viewportSize}. The VM is updating the _bitmapGrid.Viewport Size.");
					newJobNumber = HandleDisplaySizeUpdate(newValue);
				}
			}
			else
			{
				Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewportSize set to {newValue}.The current value is aleady: {_viewportSize}, not calling HandleDisplaySizeUpdate, not raising OnPropertyChanged.");
			}

			return newJobNumber;
		}

		public int? MoveTo(VectorDbl displayPosition)
		{
			if (_boundedMapArea == null || UnscaledExtent.IsNearZero())
			{
				Debug.WriteLine($"WARNING: Cannot MoveTo {displayPosition}, there is no bounding info set or the UnscaledExtent is zero.");
				return null;
			}

			ReportMove(_boundedMapArea, displayPosition);

			// Get the MapAreaInfo subset for the given display position
			var mapAreaInfo2Subset = _boundedMapArea.GetView(displayPosition);
			var newJobNumber = ReuseAndLoad(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

			DisplayPosition = displayPosition;

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
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
					var screenAreaInfo = GetScreenAreaInfo(currentJob.MapAreaInfo, ViewportSize);
					var newMapSections = _mapLoaderManager.Push(currentJob.JobId, currentJob.JobOwnerType, screenAreaInfo, currentJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

					var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
					Debug.WriteLine($"Restarting paused job: received {newMapSections.Count}, {requestsPending} are being generated.");

					result = newJobNumber;
					lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);
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
				_bitmapGrid.ClearDisplay();
			}
		}

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				if (!e.IsPreview)
				{
					Debug.WriteLine("Here");
				}

				MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, e.PanAmount, e.Factor, CurrentAreaColorAndCalcSettings.MapAreaInfo, e.IsPreview));
			}
		}

		public void RaiseMapViewPanUpdate(ImageDraggedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				var dragOffset = e.DragOffset;

				// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
				var panAmount = dragOffset.Invert();
				MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, panAmount, 1, CurrentAreaColorAndCalcSettings.MapAreaInfo));
			}
		}

		#endregion

		#region Event Handlers

		private void MapSectionReady(MapSection mapSection)
		{
			_bitmapGrid.Dispatcher.Invoke(GetAndPlacePixelsWrapper, new object[] { mapSection });
		}

		private void GetAndPlacePixelsWrapper(MapSection mapSection)
		{
			if (mapSection.MapSectionVectors != null)
			{
				lock (_paintLocker)
				{
					_bitmapGrid.GetAndPlacePixels(mapSection, mapSection.MapSectionVectors);
				}
			}

			if (mapSection.IsLastSection)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
			}
		}

		#endregion

		#region Private Methods

		private int? LoadNewView(BoundedMapArea boundedMapArea, SizeDbl viewportSize, VectorDbl contentOffset, double contentScale)
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;

			_bitmapGrid.ViewportSize = viewportSize;

			lock (_paintLocker)
			{
				boundedMapArea.ViewportSize = viewportSize;
				var mapAreaInfo2Subset = boundedMapArea.GetView(contentOffset);

				newJobNumber = ReuseAndLoad(boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
			}

			ViewportSize = viewportSize;
			DisplayPosition = contentOffset;

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		private int? HandleDisplaySizeUpdate(SizeDbl viewportSize)
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;
			
			_bitmapGrid.ViewportSize = viewportSize;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings != null)
				{
					var screenAreaInfo = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, viewportSize);
					newJobNumber = ReuseAndLoad(CurrentAreaColorAndCalcSettings, screenAreaInfo, out lastSectionWasIncluded);
				}
			}

			ViewportSize = viewportSize;

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
					var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);
					newJobNumber = ReuseAndLoad(newJob, screenAreaInfo, out lastSectionWasIncluded);
				}
				else
				{
					StopCurrentJobAndClearDisplay();
					var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);
					newJobNumber = DiscardAndLoad(newJob, screenAreaInfo, out lastSectionWasIncluded);
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

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
		{
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new ReadOnlyCollection<MapSection>(MapSections);

			var sectionsToLoad = GetSectionsToLoadAndRemove(sectionsRequired, loadedSections, out var sectionsToRemove);

			foreach (var section in sectionsToRemove)
			{
				MapSections.Remove(section);
				_mapSectionHelper.ReturnMapSection(section);
			}
	
			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
			
			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

			ColorBandSet = newJob.ColorBandSet;

			var sectionsRemoved = _bitmapGrid.ReDrawSections();

			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, we removed {sectionsToRemove.Count} ReDraw removed {sectionsRemoved}. Keeping {MapSections.Count}. {_mapSectionHelper.MapSectionsVectorsInPool} MapSection in the pool.");

			int? result;

			if (sectionsToLoad.Count > 0)
			{
				var newMapSections = _mapLoaderManager.Push(newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);
				var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
				Debug.WriteLine($"Fetching New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

				lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);

				result = newJobNumber;

				//ActiveJobNumbers.Add(newJobNumber);
				AddJobNumber(newJobNumber);
			}
			else
			{
				lastSectionWasIncluded = false;
				result = null;
			}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
		{
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);

			var newMapSections = _mapLoaderManager.Push(newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsRequired, MapSectionReady, out var newJobNumber);

			var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
			Debug.WriteLine($"Clearing Display and Loading New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;

			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

			ColorBandSet = newJob.ColorBandSet;
			lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);

			//ActiveJobNumbers.Add(newJobNumber);
			AddJobNumber(newJobNumber);

			return newJobNumber;
		}

		private void StopCurrentJobAndClearDisplay()
		{
			foreach(var jobNumber in ActiveJobNumbers)
			{
				_mapLoaderManager.StopJob(jobNumber);
			}

			ActiveJobNumbers.Clear();

			_bitmapGrid.ClearDisplay();
		}

		private void AddJobNumber(int jobNumber)
		{
			ActiveJobNumbers.Add(jobNumber);
			Debug.WriteLine($"Adding jobNumber: {jobNumber}. There are now {ActiveJobNumbers.Count} active jobs.");
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

		private List<MapSection> GetSectionsToLoadAndRemove(List<MapSection> sectionsToRequest, IReadOnlyList<MapSection> sectionsPresent, out List<MapSection> sectionsToRemove)
		{
			var result = new List<MapSection>(sectionsToRequest);

			sectionsToRemove = new List<MapSection>();

			foreach (var ms in sectionsPresent)
			{
				var alreadyPresent = sectionsToRequest.Any(reqSection => reqSection == ms && reqSection.TargetIterations == ms.TargetIterations);

				if (alreadyPresent)
				{
					// We already have it, remove it from the list of sectionsRequested.
					result.Remove(ms);
				}

				if (!alreadyPresent)
				{
					// The section from the current list could not be matched in the list of sectionsToRequest
					// we will not be needing this section any longer
					sectionsToRemove.Add(ms);
				}
			}

			return result;
		}

		private MapAreaInfo GetScreenAreaInfo(MapAreaInfo2 canonicalMapAreaInfo, SizeDbl canvasSize)
		{
			if (canvasSize.IsNAN())
			{
				throw new InvalidOperationException("canvas size is undefined.");
			}

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSizeFat(canonicalMapAreaInfo, canvasSize.Round());

			return mapAreaInfoV1;
		}

		private MapAreaInfo GetScreenAreaInfoWithDiagnostics(MapAreaInfo2 canonicalMapAreaInfo, SizeDbl canvasSize)
		{
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSizeFat(canonicalMapAreaInfo, canvasSize.Round());

			// Just for diagnostics.
			var mapAreaInfoV2 = MapJobHelper.Convert(mapAreaInfoV1);
			CompareMapAreaAfterRoundTrip(canonicalMapAreaInfo, mapAreaInfoV2, mapAreaInfoV1);

			var mapAreaInfoV1Diag = MapJobHelper.GetMapAreaWithSize(canonicalMapAreaInfo, canvasSize.Round());

			// Just for diagnostics.
			var mapAreaInfoV2Diag = MapJobHelper.Convert(mapAreaInfoV1Diag);
			CompareMapAreaAfterRoundTrip(canonicalMapAreaInfo, mapAreaInfoV2Diag, mapAreaInfoV1Diag);

			return mapAreaInfoV1;
		}

		private void DisposeMapSection(MapSection mapSection)
		{
			//var refCount = mapSection.MapSectionVectors?.ReferenceCount ?? 0;

			// The MapSection may have refCount > 1; the MapSectionPersistProcessor may not have completed its work, 
			// But when the MapSectionPersistProcessor doe complete its work, it will Dispose and at that point the refCount will be only 1.
			//if (refCount > 1)
			//{
			//	Debug.WriteLine("WARNING: MapSectionDisplayViewModel is Disposing a MapSection whose reference count > 1.");
			//}

			_mapSectionHelper.ReturnMapSection(mapSection);
		}

		private void OnBitmapUpdate(WriteableBitmap bitmap)
		{
			ImageSource = bitmap;
		}

		private void ReportNewMapArea(MapAreaInfo previousValue, MapAreaInfo newValue)
		{
			Debug.WriteLine($"MapDisplay is handling DisplaySizeUpdate. " +
				$"Previous Size: {previousValue.CanvasSize}. Pos: {previousValue.Coords.Position}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"New Size: {newValue.CanvasSize}. Pos: {newValue.Coords.Position}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}.");

			Debug.WriteLine($"UpdateSize is moving the pos from {previousValue.Coords.Position} to {newValue.Coords.Position}.");
		}

		[Conditional("DEBUG")]
		private void CompareMapAreaAfterRoundTrip(MapAreaInfo2 previousValue, MapAreaInfo2 newValue, MapAreaInfo middleValue)
		{
			Debug.WriteLine($"MapDisplay is RoundTripping MapAreaInfo" +
				$"\nPrevious Scale: {previousValue.SamplePointDelta.Width}. Pos: {previousValue.MapCenter}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"\nNew Scale     : {newValue.SamplePointDelta.Width}. Pos: {newValue.MapCenter}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}" +
				$"\nIntermediate  : {middleValue.SamplePointDelta.Width}. Pos: {middleValue.Coords}. MapOffset: {middleValue.MapBlockOffset}. ImageOffset: {middleValue.CanvasControlOffset} Size: {middleValue.CanvasSize}.");
		}

		[Conditional("DEBUG")]
		private void CheckVPSize()
		{
			Debug.WriteLine($"At checkVPSize: ViewportSize: {ViewportSize}, DisplayZoom: {DisplayZoom}, MaxZoom: {MinimumDisplayZoom}.");

			if (ViewportSize.Width < 0.1 || ViewportSize.Height < 0.1)
			{
				//Debug.WriteLine("WARNING: ViewportSize is zero, using the value from the BitmapGrid.");
				//ViewportSize = _bitmapGrid.ViewportSize;
				throw new InvalidOperationException("ViewportSize is zero at CheckVPSize.");
			}
		}

		[Conditional("NEVER")]
		private void CheckBlockSize(AreaColorAndCalcSettings newValue)
		{
			if (newValue.MapAreaInfo.Subdivision.BlockSize != BlockSize)
			{
				throw new ArgumentException("BlockSize mismatch", nameof(AreaColorAndCalcSettings.MapAreaInfo.Subdivision));
			}
		}

		[Conditional("DEBUG")]
		private void ReportSubmitJobDetails(AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings? newValue)
		{
			var currentJobId = previousValue?.JobId ?? ObjectId.Empty.ToString();

			if (newValue == null)
			{
				Debug.WriteLine($"MapDisplay is handling SumbitJob. The new value is null. CurrentJobId: {currentJobId}.");
			}
			else
			{
				var newJobId = newValue.JobId;

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

		[Conditional("DEBUG")]
		private void ReportMove(BoundedMapArea boundedMapArea, VectorDbl displayPosition)
		{
			var x = displayPosition.X;
			var y = displayPosition.Y;
			var invertedY = boundedMapArea.GetInvertedYPos(y);

			Debug.WriteLine($"Moving to {x}, {invertedY}. Uninverted Y:{y}. Poster Size: {UnscaledExtent}. Viewport: {ViewportSize}.");
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
