using MongoDB.Bson;
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

		private SizeDbl _viewPortSize;
		private VectorDbl _imageOffset;

		//private double _invertedVerticalPosition;
		private double _verticalPosition;
		private double _horizontalPosition;

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
			set
			{
				// This is called by the BitmapGrid, to let us know that we need to raise the OnPropertyChanged event.

				Debug.WriteLine($"The MapSectionViewModel's ImageSource is being set to value: {value}.");
				OnPropertyChanged(nameof(IMapDisplayViewModel.ImageSource));
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewPortSize;
			set
			{
				if (!value.IsNAN() && value != _viewPortSize)
				{
					if (value.Width <= 2 || value.Height <= 2)
					{
						Debug.WriteLine($"WARNING: MapSectionDisplayViewModel is having its ViewportSize set to {value}, which is very small. Update was aborted. Previously it was {_viewPortSize}.");

					}
					else
					{
						Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewportSize set to {value}. Previously it was {_viewPortSize}. The VM is updating the _bitmapGrid.Viewport Size.");
						_viewPortSize = value;

						 _bitmapGrid.ViewportSize = _viewPortSize;

						if (LastMapAreaInfo != null && LastMapAreaInfo.CanvasSize != value.Round())
						{
							// TODO: Check why we have a guard in place to avoid calling HandleDisplaySizeUpdate, if the value is the same as the LastMapAreaInfo.CanvasSize
							var newJobNumber = HandleDisplaySizeUpdate();
							if (newJobNumber != null)
							{
								ActiveJobNumbers.Add(newJobNumber.Value);
							}
						}
						else
						{
							if (LastMapAreaInfo != null)
								Debug.WriteLine($"Not calling HandleDisplaySizeUpdate, the LastMapAreaInfo.CanvasSize {LastMapAreaInfo.CanvasSize} is the same as the new ViewportSize: {value}.");
						}

						OnPropertyChanged(nameof(IMapDisplayViewModel.ViewportSize));
					}
				}
				else
				{
					Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewportSize set to {value}.The current value is aleady: {_viewPortSize}, not calling HandleDisplaySizeUpdate, not raising OnPropertyChanged.");
				}
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

			set
			{
				if (value != _unscaledExtent)
				{
					_unscaledExtent = value;

					// Let the BitmapGridControl know the entire size.
					OnPropertyChanged(nameof(IMapDisplayViewModel.UnscaledExtent));
				}
			}
		}

		public double VerticalPosition
		{
			get => _verticalPosition;
			set
			{
				if (value != _verticalPosition)
				{
					_verticalPosition = value;

					Debug.Assert(!UnscaledExtent.IsNearZero(), "Moving display, but we have no Unscaled Extent.");

					Debug.WriteLine($"Moving to {HorizontalPosition}, {InvertedVerticalPosition}. Uninverted Y:{VerticalPosition}. Poster Size: {UnscaledExtent}. Viewport: {ViewportSize}.");
					MoveTo(new VectorDbl(HorizontalPosition, InvertedVerticalPosition));
				}
			}
		}

		public double InvertedVerticalPosition => GetInvertedYPos(VerticalPosition);

		public double HorizontalPosition
		{
			get => _horizontalPosition;
			set
			{
				if (value != _horizontalPosition)
				{
					_horizontalPosition = value;
					Debug.WriteLine($"Horizontal Pos: {value}.");

					Debug.Assert(!UnscaledExtent.IsNearZero(), "Moving display, but we have no Unscaled Extent.");

					MoveTo(new VectorDbl(HorizontalPosition, InvertedVerticalPosition));
				}
			}
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{

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

		public int? MoveTo(VectorDbl displayPosition)
		{
			int? newJobNumber = null;

			if (_boundedMapArea != null)
			{
				// Get the MapAreaInfo subset for the given display position
				var mapAreaInfo2Subset = _boundedMapArea.GetView(displayPosition);

				//newJobNumber = HandleDisplayPositionChange(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);
				newJobNumber = ReuseAndLoad(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

				if (newJobNumber.HasValue && lastSectionWasIncluded)
				{
					DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
				}
			}
			else
			{
				Debug.WriteLine($"WARNING: Cannot MoveTo {displayPosition}, there is no bounding info set.");
			}

			return newJobNumber;
		}

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
					var displayPosition = new VectorDbl(0, GetInvertedYPos(0));
					var mapAreaInfo2Subset = _boundedMapArea.GetView(displayPosition);

					//newJobNumber = HandleDisplayPositionChange(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);
					newJobNumber = ReuseAndLoad(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

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

		private int? HandleDisplaySizeUpdate()
		{
			int? newJobNumber = null;
			bool lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				if (_boundedMapArea != null)
				{
					var displayPosition = new VectorDbl(HorizontalPosition, InvertedVerticalPosition);

					_boundedMapArea.ViewportSize = ViewportSize;
					var mapAreaInfo2Subset = _boundedMapArea.GetView(displayPosition);

					//newJobNumber = HandleDisplayPositionChange(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
					newJobNumber = ReuseAndLoad(_boundedMapArea.AreaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
				}
				else
				{
					if (CurrentAreaColorAndCalcSettings != null)
					{
						var screenAreaInfo = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, ViewportSize);
						newJobNumber = ReuseAndLoad(CurrentAreaColorAndCalcSettings, screenAreaInfo, out lastSectionWasIncluded);
					}
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
					var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);
					newJobNumber = ReuseAndLoad(newJob, screenAreaInfo, out lastSectionWasIncluded);
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

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
		{
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new ReadOnlyCollection<MapSection>(MapSections);

			//var sectionsToLoad = GetSectionsToLoad(sectionsRequired, loadedSections);
			//var sectionsToRemove = GetSectionsToRemove(sectionsRequired, loadedSections);
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
			var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);

			var newMapSections = _mapLoaderManager.Push(newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsRequired, MapSectionReady, out var newJobNumber);

			var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
			Debug.WriteLine($"Clearing Display and Loading New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;

			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

			ColorBandSet = newJob.ColorBandSet;
			lastSectionWasIncluded = _bitmapGrid.DrawSections(newMapSections);

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

			_bitmapGrid.ClearDisplay();
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

		private double GetInvertedYPos(double yPos)
		{
			double result;
			double maxV;

			//if (UnscaledExtent.IsEmpty)
			//{
			//	maxV = ViewportSize.Height;
			//	result = maxV - yPos;
			//}
			//else
			//{
			//	//maxV = UnscaledExtent.Height; //Math.Max(ViewportSize.Height, PosterSize.Height - ViewportSize.Height);
			//	//result = maxV - (yPos + ViewportSize.Height);

			//	maxV = Math.Max(0.0, UnscaledExtent.Height - ViewportSize.Height);
			//	result = maxV - yPos;
			//}

			maxV = Math.Max(0.0, UnscaledExtent.Height - ViewportSize.Height);
			result = maxV - yPos;

			return result;
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
				Debug.WriteLine("WARNING: ViewportSize is zero, using the value from the BitmapGrid.");
				ViewportSize = _bitmapGrid.ViewportSize;
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
