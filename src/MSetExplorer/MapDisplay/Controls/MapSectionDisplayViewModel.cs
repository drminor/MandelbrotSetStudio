using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MSetExplorer.MapDisplay.Controls;
using MSetExplorer.MapDisplay.ScrollAndZoom;
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

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionHelper _mapSectionHelper;

		private ObservableCollection<MapSection> _mapSections;

		private ColorBandSet _colorBandSet;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;
		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		private MapAreaInfo? _latestMapAreaInfo;

		private object _paintLocker;

		private SizeDbl _viewPortSize;
		private VectorDbl _imageOffset;

		private double _invertedVerticalPosition;
		private double _verticalPosition;
		private double _horizontalPosition;

		private SizeInt? _posterSize;

		private double _displayZoom;
		private double _maximumDisplayZoom;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_posterSize = new SizeInt();
			_paintLocker = new object();
			BlockSize = blockSize;

			BitmapGrid = null;

			ActiveJobNumbers = new List<int>();

			_mapLoaderManager = mapLoaderManager;
			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			_mapSections = new ObservableCollection<MapSection>();

			_colorBandSet = new ColorBandSet();
			_useEscapeVelocities = false;
			_highlightSelectedColorBand = false;
			_currentAreaColorAndCalcSettings = null;
			_latestMapAreaInfo = null;

			_imageOffset = new VectorDbl();

			_displayZoom = 1;
			_maximumDisplayZoom = 1;
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

		public ColorBand? CurrentColorBand
		{
			get => ColorBandSet.SelectedColorBand;
			set
			{
				ColorBandSet.SelectedColorBand = value;

				// TODO: Consider using a Binding to update the SelectedColorBand, instead of setting a property on the BitmapGrid class directly.

				if (HighlightSelectedColorBand && BitmapGrid != null)
				{
					BitmapGrid.CurrentColorBand = value;
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
				//lock (_paintLocker)
				//{
				//	_highlightSelectedColorBand = value;
				//	OnPropertyChanged(nameof(IMapDisplayViewModel.HighlightSelectedColorBand));
				//}

				_highlightSelectedColorBand = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.HighlightSelectedColorBand));
			}
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public Action<MapSection> DisposeMapSection => DisposeMapSectionInternal;

		public IBitmapGrid? BitmapGrid { get; set; }

		public SizeDbl ViewPortSize
		{
			get => _viewPortSize;
			set
			{
				if (!value.IsNAN() && value != _viewPortSize)
				{
					if (value.Width <= 2 || value.Height <= 2)
					{
						Debug.WriteLine($"WARNING: MapSectionDisplayViewModel is having its ViewPortSize set to {value}, which is very small. Update was aborted. Previously it was {_viewPortSize}.");

					}
					else
					{
						Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewPortSize set to {value}. Previously it was {_viewPortSize}.");
						_viewPortSize = value;

						if (LastMapAreaInfo != null && LastMapAreaInfo.CanvasSize != value.Round())
						{
							// TODO: Check why we have a guard in place to avoid calling HandleDisplaySizeUpdate, if the value is the same as the LastMapAreaInfo.CanvasSize
							var newJobNumber = HandleDisplaySizeUpdate();
							if (newJobNumber != null)
							{
								ActiveJobNumbers.Add(newJobNumber.Value);
							}
						}

						OnPropertyChanged(nameof(IMapDisplayViewModel.ViewPortSize));
					}
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

		public bool IsBound => PosterSize != null;

		public SizeInt? PosterSize
		{
			get => _posterSize;

			set
			{
				if (value != _posterSize)
				{
					_posterSize = value;
					//InvertedVerticalPosition = GetInvertedYPos(VerticalPosition);

					//MaximumDisplayZoom = GetMaximumDisplayZoom(PosterSize, ViewPortSize);

					OnPropertyChanged(nameof(IMapDisplayViewModel.PosterSize));
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
					_invertedVerticalPosition = GetInvertedYPos(value);
					Debug.WriteLine($"Vertical Pos: {VerticalPosition}, Inverted: {InvertedVerticalPosition}.");
					OnPropertyChanged(nameof(IMapDisplayViewModel.VerticalPosition));
					OnPropertyChanged(nameof(IMapDisplayViewModel.InvertedVerticalPosition));
				}
			}
		}

		public double InvertedVerticalPosition
		{
			get => _invertedVerticalPosition;
			set
			{
				if (value != _invertedVerticalPosition)
				{
					_invertedVerticalPosition = value;
					_verticalPosition = GetInvertedYPos(value);
					Debug.WriteLine($"Vertical Pos: {VerticalPosition}, Inverted: {InvertedVerticalPosition}.");
					OnPropertyChanged(nameof(IMapDisplayViewModel.InvertedVerticalPosition));
					OnPropertyChanged(nameof(IMapDisplayViewModel.VerticalPosition));
				}
			}
		}

		public double HorizontalPosition
		{
			get => _horizontalPosition;
			set
			{
				if (value != _horizontalPosition)
				{
					_horizontalPosition = value;
					Debug.WriteLine($"Horizontal Pos: {value}.");
					OnPropertyChanged(nameof(IMapDisplayViewModel.HorizontalPosition));
				}
			}
		}


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
				//if (Math.Abs(value - DisplayZoom) > 0.001)
				//{
				//	_displayZoom = Math.Min(MaximumDisplayZoom, value);

				//	MapDisplayViewModel.DisplayZoom = _displayZoom;

				//	Debug.WriteLine($"The DispZoom is {DisplayZoom}.");
				//	OnPropertyChanged(nameof(IMapScrollViewModel.DisplayZoom));
				//}

				var previousValue = _displayZoom;

				_displayZoom = Math.Min(MaximumDisplayZoom, value);

				//MapDisplayViewModel.DisplayZoom = _displayZoom;

				Debug.WriteLine($"The MapScrollViewModel's DisplayZoom is being updated to {DisplayZoom}, the previous value is {previousValue}.");
				// Log: Add Spacer
				Debug.WriteLine("\n\n");
				OnPropertyChanged(nameof(IMapScrollViewModel.DisplayZoom));
			}
		}

		public double MaximumDisplayZoom
		{
			get => _maximumDisplayZoom;
			private set
			{
				if (Math.Abs(value - _maximumDisplayZoom) > 0.001)
				{
					_maximumDisplayZoom = value;

					if (DisplayZoom > MaximumDisplayZoom)
					{
						Debug.WriteLine($"The MapScrollViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being adjusted to be less or equal to this.");
						DisplayZoom = MaximumDisplayZoom;
					}
					else
					{
						Debug.WriteLine($"The MapScrollViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being kept the same.");
					}

					OnPropertyChanged(nameof(IMapScrollViewModel.MaximumDisplayZoom));
				}
			}
		}

		#endregion

		#region Public Methods

		private BoundedMapArea? _boundedMapArea;

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
			lock (_paintLocker)
			{
				CheckVPSize();

				int? newJobNumber = null;

				if (posterSize.HasValue)
				{
					// Bounded

					// Save the MapAreaInfo for the entire poster.
					_boundedMapArea = new BoundedMapArea(_mapJobHelper, newValue.MapAreaInfo, ViewPortSize, posterSize.Value);

					// Get the MapAreaInfo subset for the upper, left-hand corner.
					var mapAreaInfo2Subset = _boundedMapArea.GetView(new VectorInt(0, 0));

					// Update the value using the new view. 
					newValue = newValue.UpdateWith(mapAreaInfo2Subset);
				}

				// Unbounded

				if (newValue != CurrentAreaColorAndCalcSettings)
				{
					var previousValue = CurrentAreaColorAndCalcSettings;

					CurrentAreaColorAndCalcSettings = newValue;
					ReportSubmitJobDetails(previousValue, newValue);

					newJobNumber = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings);
				}

				return newJobNumber;
			}
		}

		private void CheckVPSize()
		{
			if (ViewPortSize.Width < 0.1 || ViewPortSize.Height < 0.1)
			{
				Debug.WriteLine("WARNING: ViewPortSize is zero, using the value from the BitmapGrid.");
				ViewPortSize = BitmapGrid?.ViewPortSize ?? throw new InvalidOperationException("ViewPortSize is 0 and the BitmapGrid is null.");
			}
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
					var screenAreaInfo = GetScreenAreaInfo(currentJob.MapAreaInfo, ViewPortSize);
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

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
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

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
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
			var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewPortSize);
			LastMapAreaInfo = screenAreaInfo;

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
			var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewPortSize);
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);

			var newMapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsRequired, MapSectionReady, out var newJobNumber);

			var requestsPending = _mapLoaderManager.GetPendingRequests(newJobNumber);
			Debug.WriteLine($"Clearing Display and Loading New Sections: received {newMapSections.Count}, {requestsPending} are being generated.");

			if (BitmapGrid != null)
			{
				BitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
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

		private void DisposeMapSectionInternal(MapSection mapSection)
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

		#region Private Methods - Scroll

		private double GetInvertedYPos(double yPos)
		{
			double result = 0;

			if (PosterSize.HasValue)
			{
				result = PosterSize.Value.Height - yPos;
				
				var logicalDisplayHeight = ViewPortSize.Height;

				result -= logicalDisplayHeight;
			}

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
