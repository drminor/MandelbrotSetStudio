using MongoDB.Bson;
using MSetExplorer.MapDisplay.Support;
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
		#region Private Fields

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionBuilder _mapSectionHelper;

		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		private BoundedMapArea? _boundedMapArea;

		private MapAreaInfo? _latestMapAreaInfo;

		private readonly object _paintLocker;

		private IBitmapGrid _bitmapGrid;

		private SizeDbl _viewportSize;
		private VectorDbl _imageOffset;

		private VectorDbl _displayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionBuilder mapSectionHelper, SizeInt blockSize)
		{
			_paintLocker = new object();
			BlockSize = blockSize;
			BoundedMapArea = null;

			MapSections = new ObservableCollection<MapSection>();
			MapSectionsPendingGeneration = new ObservableCollection<MapSection>();

			_bitmapGrid = new BitmapGrid(MapSections, new SizeDbl(128), DisposeMapSection, OnBitmapUpdate, blockSize);

			ActiveJobNumbers = new List<int>();

			_mapLoaderManager = mapLoaderManager;
			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			_currentAreaColorAndCalcSettings = null;
			_latestMapAreaInfo = null;

			_viewportSize = new SizeDbl();
			_imageOffset = new VectorDbl();
			_displayPosition = new VectorDbl();

			_displayZoom = 1;
			_minimumDisplayZoom = 0.015625; // 0.0625;
		}

		#endregion

		#region Events

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		public event EventHandler? JobSubmitted;

		#endregion

		#region Public Properties - Content

		public ObservableCollection<MapSection> MapSections { get; init; }

		public ObservableCollection<MapSection> MapSectionsPendingGeneration { get; init; }

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
			set
			{
				_currentAreaColorAndCalcSettings = CurrentAreaColorAndCalcSettings?.UpdateWith(value);
				_bitmapGrid.ColorBandSet = value;
			}
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

				Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's ImageSource is being set to value: {value}.");
				OnPropertyChanged(nameof(IMapDisplayViewModel.ImageSource));
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
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

		public bool IsBound => BoundedMapArea != null;

		private BoundedMapArea? BoundedMapArea
		{
			get => _boundedMapArea;
			set
			{
				_boundedMapArea = value;

				// Let the BitmapGridControl know the entire size.
				OnPropertyChanged(nameof(IMapDisplayViewModel.UnscaledExtent));
			}
		}

		public SizeDbl UnscaledExtent => _boundedMapArea?.PosterSize ?? SizeDbl.Zero;

		public VectorDbl DisplayPosition
		{
			get => _displayPosition;
			private set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(_displayPosition, value))
				{
					_displayPosition = value;
					//OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayPosition));
				}
			}
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			private set
			{
				var previousValue = _displayZoom;
				_displayZoom = value;

				Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's DisplayZoom is being updated to {DisplayZoom}, the previous value is {previousValue}.");
				//OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayZoom));
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			private set
			{
				_minimumDisplayZoom = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.MinimumDisplayZoom));
			}
		}

		public Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		#endregion

		#region Public Methods

		public int? SubmitJob(AreaColorAndCalcSettings newValue)
		{
			CheckBlockSize(newValue);

			lock (_paintLocker)
			{
				CheckVPSize();

				var lastSectionWasIncluded = false;
				int? newJobNumber = null;

				// Unbounded
				BoundedMapArea = null;

				if (newValue != CurrentAreaColorAndCalcSettings)
				{
					var previousValue = CurrentAreaColorAndCalcSettings;
					if (_useDetailedDebug) ReportSubmitJobDetails(previousValue, newValue);

					CurrentAreaColorAndCalcSettings = newValue;
					newJobNumber = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings, out lastSectionWasIncluded);
				}

				if (newJobNumber.HasValue && lastSectionWasIncluded)
				{
					DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
				}

				return newJobNumber;
			}
		}

		// TODO: SubmitJob may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId
		public int? SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom)
		{
			CheckBlockSize(newValue);

			lock (_paintLocker)
			{
				CheckVPSize();

				var lastSectionWasIncluded = false;
				int? newJobNumber = null;

				// Bounded

				// Save the MapAreaInfo for the entire poster.
				BoundedMapArea = new BoundedMapArea(_mapJobHelper, newValue.MapAreaInfo, posterSize, ViewportSize);

				MinimumDisplayZoom = GetMinDisplayZoom(posterSize, ViewportSize);

				if (displayZoom < MinimumDisplayZoom)
				{
					DisplayZoom = MinimumDisplayZoom;
				}
				else
				{
					DisplayZoom = displayZoom;
				}

				// TODO: Add bindings so that the PanAndZoomControl can have its OffsetX and OffsetY properties set.

				var previousValue = CurrentAreaColorAndCalcSettings;
				if (_useDetailedDebug) ReportSubmitJobDetails(previousValue, newValue);

				// Get the MapAreaInfo subset for the current view. The display postion specifies the left, bottom pixel to be displayed.
				var mapAreaInfo2Subset = BoundedMapArea.GetView(displayPosition);

				newJobNumber = DiscardAndLoad(newValue, mapAreaInfo2Subset, out lastSectionWasIncluded);

				CurrentAreaColorAndCalcSettings = newValue;
				DisplayPosition = displayPosition;

				JobSubmitted?.Invoke(this, new EventArgs());

				if (newJobNumber.HasValue && lastSectionWasIncluded)
				{
					DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
				}

				return newJobNumber;
			}
		}

		private double GetMinDisplayZoom(SizeDbl extent, SizeDbl viewport)
		{
			// Calculate the Zoom level at which the poster fills the screen, leaving a 20 pixel border.

			var framedViewPort = viewport.Sub(new SizeDbl(20));
			var minScale = framedViewPort.Divide(extent);
			var result = Math.Min(minScale.Width, minScale.Height);
			result = Math.Min(result, 1);

			return result;
		}

		// TODO: UpdateViewportSizeAndPos may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId
		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			int? newJobNumber;

			if (CurrentAreaColorAndCalcSettings == null)
			{
				_bitmapGrid.ViewportSize = contentViewportSize;
				ViewportSize = contentViewportSize;
				newJobNumber = null;
			}
			else
			{
				if (BoundedMapArea == null)
				{
					throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
				}

				var (baseScale, relativeScale) = ContentScalerHelper.GetBaseAndRelative(contentScale);

				DisplayZoom = contentScale;
				newJobNumber = LoadNewView(CurrentAreaColorAndCalcSettings, BoundedMapArea, contentViewportSize, contentOffset, baseScale);
			}

			return newJobNumber;
		}

		public int? UpdateViewportSize(SizeDbl newValue)
		{
			int? newJobNumber = null;

			if (!newValue.IsNAN() && newValue != _viewportSize)
			{
				if (newValue.Width <= 2 || newValue.Height <= 2)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"WARNING: MapSectionDisplayViewModel is having its ViewportSize set to {newValue}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}.");
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel is having its ViewportSize set to {newValue}. Previously it was {_viewportSize}. The VM is updating the _bitmapGrid.Viewport Size.");
					newJobNumber = HandleDisplaySizeUpdate(newValue);
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel is having its ViewportSize set to {newValue}.The current value is aleady: {_viewportSize}, not calling HandleDisplaySizeUpdate.");
			}

			return newJobNumber;
		}

		public int? MoveTo(VectorDbl contentOffset)
		{
			if (BoundedMapArea == null || UnscaledExtent.IsNearZero())
			{
				//Debug.WriteLine($"WARNING: Cannot MoveTo {displayPosition}, there is no bounding info set or the UnscaledExtent is zero.");
				//return null;

				throw new InvalidOperationException("Cannot call MoveTo, if the boundedMapArea is null or if the UnscaledExtent is zero.");
			}

			if (CurrentAreaColorAndCalcSettings == null)
			{
				throw new InvalidOperationException("Cannot call MoveTo, if the CurrentAreaColorAndCalcSettings is null.");
			}

			// Get the MapAreaInfo subset for the given display position
			var mapAreaInfo2Subset = BoundedMapArea.GetView(contentOffset);

			ReportMove(BoundedMapArea, contentOffset/*, BoundedMapArea.ContentScale, BoundedMapArea.BaseScale*/);

			var newJobNumber = ReuseAndLoad(CurrentAreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

			DisplayPosition = contentOffset;

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

		//public int? RestartLastJob()
		//{
		//	int? result;
		//	bool lastSectionWasIncluded;

		//	lock (_paintLocker)
		//	{
		//		var currentJob = CurrentAreaColorAndCalcSettings;

		//		if (currentJob != null && !currentJob.IsEmpty)
		//		{
		//			var screenAreaInfo = GetScreenAreaInfo(currentJob.MapAreaInfo, ViewportSize);
		//			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, currentJob.MapCalcSettings);
		//			var newMapSections = _mapLoaderManager.Push(currentJob.JobId, currentJob.JobOwnerType, screenAreaInfo, currentJob.MapCalcSettings, sectionsRequired, MapSectionReady, 
		//				out var newJobNumber, out var mapSectionsPendingGeneration);

		//			AddPendingSections(mapSectionsPendingGeneration);

		//			Debug.WriteLine($"Restarting paused job: received {newMapSections.Count}, {mapSectionsPendingGeneration.Count} are being generated.");

		//			result = newJobNumber;
					
		//			//lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);

		//			_bitmapGrid.DrawSections(newMapSections);
		//			lastSectionWasIncluded = mapSectionsPendingGeneration.Count == 0;
		//		}
		//		else
		//		{
		//			Debug.WriteLine($"RestartLastJob was called but the current job is null or empty.");

		//			lastSectionWasIncluded = false;
		//			result = null;
		//		}
		//	}

		//	if (result.HasValue && lastSectionWasIncluded)
		//	{
		//		DisplayJobCompleted?.Invoke(this, result.Value);
		//	}

		//	return result;
		//}

		private void AddPendingSections(IList<MapSection> pendingSections)
		{
			foreach(var ms in pendingSections)
			{
				MapSectionsPendingGeneration.Add(ms);
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

		public void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				var eventArgs = e.IsPreviewBeingCancelled
					? MapViewUpdateRequestedEventArgs.CreateCancelPreviewInstance(e.TransformType)
					: new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, e.PanAmount, e.Factor, e.ScreenArea, e.DisplaySize, CurrentAreaColorAndCalcSettings.MapAreaInfo, e.IsPreview);

				MapViewUpdateRequested?.Invoke(this, eventArgs);
			}
		}

		public void RaiseMapViewPanUpdate(ImageDraggedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				MapViewUpdateRequestedEventArgs eventArgs;

				if (e.IsPreviewBeingCancelled)
				{
					eventArgs = MapViewUpdateRequestedEventArgs.CreateCancelPreviewInstance(e.TransformType);
				}
				else
				{
					// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
					var panAmount = e.DragOffset.Invert();

					eventArgs = new MapViewUpdateRequestedEventArgs(TransformType.Pan, panAmount, 1, CurrentAreaColorAndCalcSettings.MapAreaInfo, e.IsPreview);
				}

				MapViewUpdateRequested?.Invoke(this, eventArgs);
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
					MapSectionsPendingGeneration.Remove(mapSection);
				}
			}

			if (mapSection.IsLastSection)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
			}
		}

		#endregion

		#region Private Methods

		private int? LoadNewView(AreaColorAndCalcSettings areaColorAndCalcSettings, BoundedMapArea boundedMapArea, SizeDbl viewportSize, VectorDbl contentOffset, double baseScale)
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				var currentBaseScale = boundedMapArea.BaseScale;

				var mapAreaInfo2Subset = boundedMapArea.GetView(viewportSize, contentOffset, baseScale);

				var scaledViewportSize = viewportSize.Scale(boundedMapArea.ScaleFactor);
				_bitmapGrid.ViewportSize = scaledViewportSize;

				ReportUpdateSizeAndPos(boundedMapArea, viewportSize, contentOffset);

				if (boundedMapArea.BaseScale != currentBaseScale)
				{
					newJobNumber = DiscardAndLoad(areaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
				}
				else
				{
					newJobNumber = ReuseAndLoad(areaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
				}
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

		private int? HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob, out bool lastSectionWasIncluded)
		{
			int? newJobNumber;

			if (newJob != null && !newJob.IsEmpty)
			{
				var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);
				if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
				{
					newJobNumber = ReuseAndLoad(newJob, screenAreaInfo, out lastSectionWasIncluded);
				}
				else
				{
					newJobNumber = DiscardAndLoad(newJob, screenAreaInfo, out lastSectionWasIncluded);
				}
			}
			else
			{
				StopCurrentJobAndClearDisplay();
				lastSectionWasIncluded = false;
				newJobNumber = null;
			}

			return newJobNumber;
		}

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
		{
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new List<MapSection>(MapSections);
			
			foreach(var ms in MapSectionsPendingGeneration)
			{
				loadedSections.Add(ms);
			}

			var sectionsToLoad = GetSectionsToLoadAndRemove(sectionsRequired, loadedSections, out var sectionsToRemove);

			int? result;

			if (sectionsToLoad.Count == 0 && sectionsToRemove.Count == 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, "ReuseAndLoad is performing a 'simple' update.");
				_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);
				ColorBandSet = newJob.ColorBandSet;

				lastSectionWasIncluded = false;
				result = null;
			}
			else
			{
				// TODO: Clear the sections that are pending generation
				var sectionsToCancel = new List<MapSection>();
				//var sectionsToClear = new List<MapSection>();

				foreach (var section in sectionsToRemove)
				{
					if (MapSectionsPendingGeneration.Contains(section))
					{
						MapSectionsPendingGeneration.Remove(section);
						sectionsToCancel.Add(section);
					}
					else
					{
						MapSections.Remove(section);
						_mapSectionHelper.ReturnMapSection(section);
						//sectionsToClear.Add(section);
					}
				}

				_mapLoaderManager.CancelRequests(sectionsToCancel);

				//_bitmapGrid.ClearSections(sectionsToClear);

				//_bitmapGrid.ClearDisplay();

				_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);
				ColorBandSet = newJob.ColorBandSet;

				var numberOfSectionsReturned = _bitmapGrid.ReDrawSections();

				var numberOfRequestsCancelled = sectionsToCancel.Count;
				numberOfSectionsReturned += sectionsToRemove.Count - numberOfRequestsCancelled;
				Debug.WriteLineIf(_useDetailedDebug, $"Reusing Loaded Sections. Requesting {sectionsToLoad.Count} sections, Cancelling {numberOfRequestsCancelled} pending requests, returned {numberOfSectionsReturned} sections. " +
					$"Keeping {MapSections.Count} sections. The MapSection Pool has: {_mapSectionHelper.MapSectionsVectorsInPool} sections.");

				if (sectionsToLoad.Count > 0)
				{
					var newMapSections = _mapLoaderManager.Push(newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady,
						out var newJobNumber, out var mapSectionsPendingGeneration);

					AddPendingSections(mapSectionsPendingGeneration);

					Debug.WriteLineIf(_useDetailedDebug, $"ReuseAndLoad: {newMapSections.Count} were found in the repo, {mapSectionsPendingGeneration.Count} are being generated.");

					_bitmapGrid.DrawSections(newMapSections);

					lastSectionWasIncluded = mapSectionsPendingGeneration.Count == 0;

					result = newJobNumber;

					AddJobNumber(newJobNumber);
				}
				else
				{
					lastSectionWasIncluded = false;
					result = null;
				}
			}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
		{
			StopCurrentJobAndClearDisplay();

			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);

			var newMapSections = _mapLoaderManager.Push(newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsRequired, MapSectionReady,
					out var newJobNumber, out var mapSectionsPendingGeneration);

			AddPendingSections(mapSectionsPendingGeneration);

			Debug.WriteLineIf(_useDetailedDebug, $"DiscardAndLoad: {newMapSections.Count} were found in the repo, {mapSectionsPendingGeneration.Count} are being generated.");

			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;

			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

			ColorBandSet = newJob.ColorBandSet;

			//lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);
			_bitmapGrid.DrawSections(newMapSections);
			lastSectionWasIncluded = mapSectionsPendingGeneration.Count == 0;

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
			Debug.WriteLineIf(_useDetailedDebug, $"Adding jobNumber: {jobNumber}. There are now {ActiveJobNumbers.Count} active jobs.");
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

		private List<MapSection> GetSectionsToLoadAndRemove(List<MapSection> sectionsToRequest, IList<MapSection> sectionsPresent, out List<MapSection> sectionsToRemove)
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

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSizeFat(canonicalMapAreaInfo, canvasSize);

			return mapAreaInfoV1;
		}

		private MapAreaInfo GetScreenAreaInfoWithDiagnostics(MapAreaInfo2 canonicalMapAreaInfo, SizeDbl canvasSize)
		{
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSizeFat(canonicalMapAreaInfo, canvasSize);

			// Just for diagnostics.
			var mapAreaInfoV2 = MapJobHelper.Convert(mapAreaInfoV1);
			CompareMapAreaAfterRoundTrip(canonicalMapAreaInfo, mapAreaInfoV2, mapAreaInfoV1);

			var mapAreaInfoV1Diag = MapJobHelper.GetMapAreaWithSize(canonicalMapAreaInfo, canvasSize);

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

		[Conditional("DEBUG2")]
		private void CompareMapAreaAfterRoundTrip(MapAreaInfo2 previousValue, MapAreaInfo2 newValue, MapAreaInfo middleValue)
		{
			Debug.WriteLine($"MapDisplay is RoundTripping MapAreaInfo" +
				$"\nPrevious Scale: {previousValue.SamplePointDelta.Width}. Pos: {previousValue.MapCenter}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"\nNew Scale     : {newValue.SamplePointDelta.Width}. Pos: {newValue.MapCenter}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}" +
				$"\nIntermediate  : {middleValue.SamplePointDelta.Width}. Pos: {middleValue.Coords}. MapOffset: {middleValue.MapBlockOffset}. ImageOffset: {middleValue.CanvasControlOffset} Size: {middleValue.CanvasSize}.");
		}

		[Conditional("DEBUG2")]
		private void CheckVPSize()
		{
			if (_useDetailedDebug)
				Debug.WriteLine($"At checkVPSize: ViewportSize: {ViewportSize}, DisplayZoom: {DisplayZoom}, MinZoom: {MinimumDisplayZoom}.");

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

		[Conditional("DEBUG2")]
		private void ReportSubmitJobDetails(AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings? newValue)
		{
			var currentJobId = previousValue?.JobId ?? ObjectId.Empty.ToString();
			var forClause = IsBound ? "with bounds" : "without bounds";

			if (newValue == null)
			{
				Debug.WriteLine($"MapDisplay is handling SubmitJob {forClause}. The new value is null. CurrentJobId: {currentJobId}.");
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

		[Conditional("DEBUG2")]
		private void ReportMove(BoundedMapArea boundedMapArea, VectorDbl contentOffset/*, double contentScale, double baseScale*/)
		{
			var scaledDispPos = boundedMapArea.GetScaledDisplayPosition(contentOffset, out var unInvertedY);

			var x = scaledDispPos.X;
			var y = scaledDispPos.Y;

			//var posterSize = boundedMapArea.PosterSize;
			//var scaledExtent = posterSize.Scale(boundedMapArea.ScaleFactor);

			//var physicalViewportSize = ViewportSize.Scale(boundedMapArea.ContentScale);

			//Debug.WriteLine($"Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {ViewportSize}. ContentScale: {boundedMapArea.ContentScale}, BaseScaleFactor: {boundedMapArea.ScaleFactor}. " +
			//	$"Scaled Extent: {scaledExtent}, ViewportSize: {physicalViewportSize}.");

			var posterSize = boundedMapArea.PosterSize;

			Debug.WriteLine($"Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {ViewportSize}. BaseScaleFactor: {boundedMapArea.ScaleFactor}.");


		}

		[Conditional("DEBUG2")]
		private void ReportUpdateSizeAndPos(BoundedMapArea boundedMapArea, SizeDbl viewportSize, VectorDbl contentOffset/*, double contentScale, double baseScale*/)
		{
			var scaledDispPos = boundedMapArea.GetScaledDisplayPosition(contentOffset, out var unInvertedY);

			var x = scaledDispPos.X;
			var y = scaledDispPos.Y;

			//var posterSize = boundedMapArea.PosterSize;
			//var scaledExtent = posterSize.Scale(boundedMapArea.ContentScale);

			//var physicalViewportSize = viewportSize.Scale(contentScale);

			//Debug.WriteLine($"Loading new view. Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {viewportSize}. ContentScale: {contentScale}, BaseScale: {baseScale}. " +
			//	$"Scaled Extent: {scaledExtent}, ViewportSize: {physicalViewportSize}.");

			var posterSize = boundedMapArea.PosterSize;

			Debug.WriteLine($"Loading new view. Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {viewportSize}. BaseScaleFactor: {boundedMapArea.ScaleFactor}.");

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
