using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Fields

		private readonly bool CLEAR_MAP_SECTIONS_PENDING_GENERATION = true;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly object _paintLocker;

		private BoundedMapArea? _boundedMapArea;

		private List<MapSection> _mapSectionsPendingGeneration { get; init; }
		private MapAreaInfo? _latestMapAreaInfo;

		private IBitmapGrid _bitmapGrid;
		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		private SizeDbl _unscaledExtent;
		private SizeDbl _viewportSize;
		private VectorInt _imageOffset;

		private VectorDbl _theirDisplayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;
		private double _maximumDisplayZoom;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionVectorProvider mapSectionVectorProvider, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapJobHelper = mapJobHelper;

			BlockSize = blockSize;
			_mapSectionBuilder = new MapSectionBuilder();

			_paintLocker = new object();

			_boundedMapArea = null;
			MapSections = new ObservableCollection<MapSection>();

			_mapSectionsPendingGeneration = new List<MapSection>();
			_latestMapAreaInfo = null;

			_bitmapGrid = new BitmapGrid(MapSections, new SizeDbl(128), DisposeMapSection, OnBitmapUpdate, blockSize);

			ActiveJobNumbers = new List<int>();
			_currentAreaColorAndCalcSettings = null;

			_unscaledExtent = new SizeDbl();
			_viewportSize = new SizeDbl();
			_imageOffset = new VectorInt();

			_theirDisplayPosition = new VectorDbl(double.NaN, double.NaN);

			_minimumDisplayZoom = RMapConstants.DEFAULT_MINIMUM_DISPLAY_ZOOM; // 0.015625; // 0.0625;
			_maximumDisplayZoom = 1.0;
			_displayZoom = 1;
		}

		#endregion

		#region Events

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		public event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;

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
			set
			{
				if (_currentAreaColorAndCalcSettings != null)
				{
					if (_currentAreaColorAndCalcSettings.ColorBandSet != value)
					{
						_currentAreaColorAndCalcSettings = _currentAreaColorAndCalcSettings.UpdateWith(value);
					}
				}

				_bitmapGrid.ColorBandSet = value;
			}
		}

		public ColorBand? CurrentColorBand
		{
			get => _bitmapGrid.CurrentColorBand;
			//set => _bitmapGrid.CurrentColorBand = value;
		}

		public int SelectedColorBandIndex
		{
			get => _bitmapGrid.SelectedColorBandIndex;
			set => _bitmapGrid.SelectedColorBandIndex = value;
		}

		public bool UseEscapeVelocities
		{
			get => _bitmapGrid.UseEscapeVelocities;
			set
			{
				_bitmapGrid.UseEscapeVelocities = value;
				//_mapLoaderManager.CalculateEscapeVelocities = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.UseEscapeVelocities));
			}
		}

		public bool HighlightSelectedColorBand
		{
			get => _bitmapGrid.HighlightSelectedColorBand;
			set => _bitmapGrid.HighlightSelectedColorBand = value;
		}

		public MapAreaInfo? LastMapAreaInfo
		{
			get => _latestMapAreaInfo;
			private set { _latestMapAreaInfo = value; }
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; init; }
		
		/// <summary>
		/// The size of the display in pixels.
		/// Only used in unbounded mode (via a Binding declared on the XAML for the MapSectionDisplayControl.)
		/// </summary>
		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(value, _viewportSize, 0.001))
				{
					if (value.Width >= 2 && value.Height >= 2)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel is having its ViewportSize set to {value}. Previously it was {_viewportSize}. The VM is updating the _bitmapGrid.Viewport Size.");

						_viewportSize = value;
						_bitmapGrid.LogicalViewportSize = value; // ReuseAndLoad is now setting the _bitmapGrid's LogicalViewportSize.

						int? newJobNumber = null;
						bool lastSectionWasIncluded = false;

						lock (_paintLocker)
						{
							if (CurrentAreaColorAndCalcSettings != null)
							{
								Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== As the ViewportSize is updated, the MapSectionDisplayViewModel is calling ReuseAndLoad.");

								var screenAreaInfo = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, value);
								newJobNumber = ReuseAndLoad(JobType.FullScale, CurrentAreaColorAndCalcSettings, screenAreaInfo, reapplyColorMap: false, out lastSectionWasIncluded);
							}
						}

						if (newJobNumber.HasValue && lastSectionWasIncluded)
						{
							DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
						}

						OnPropertyChanged(nameof(IMapDisplayViewModel.ViewportSize));
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"WARNING: MapSectionDisplayViewModel is having its ViewportSize set to {value}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}. The VM is updating the _bitmapGrid.Viewport Size");
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel is having its ViewportSize set to {value}.The current value is aleady: {_viewportSize}, The VM is updating the _bitmapGrid.Viewport Size.");
				}
			}
		}

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

		public VectorInt ImageOffset
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

		/// <summary>
		/// Same as the PanAndZoomControl's ConstrainedViewportSize
		/// </summary>
		public SizeDbl? ContentViewportSize => _boundedMapArea?.ContentViewportSize;

		/// <summary>
		/// Same as the PanAndZoomControl's ConstrainedViewportSize * BaseScale
		/// This is the CanvasSize of the current view.
		/// This is the same as our ViewportSize property.
		/// </summary>
		public SizeDbl LogicalViewportSize => _bitmapGrid.LogicalViewportSize;

		#endregion

		#region Public Properties - Scroll

		public SizeDbl UnscaledExtent
		{
			get => _unscaledExtent;

			set
			{
				if (value != _unscaledExtent)
				{
					_unscaledExtent = value;
					OnPropertyChanged(nameof(IMapDisplayViewModel.UnscaledExtent));
				}
			}
		}

		public VectorDbl DisplayPosition => _boundedMapArea?.GetUnScaledDisplayPosition(_theirDisplayPosition) ?? new VectorDbl();

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				//var previousValue = _displayZoom;

				if (ScreenTypeHelper.IsDoubleChanged(value, _displayZoom, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF))
				{
					_displayZoom = value;

					//Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's DisplayZoom is being updated to {DisplayZoom}, the previous value is {previousValue}.");
					OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayZoom));
				}
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			set
			{
				_minimumDisplayZoom = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.MinimumDisplayZoom));
			}
		}

		public double MaximumDisplayZoom
		{
			get => _maximumDisplayZoom;
			set
			{
				_maximumDisplayZoom = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.MaximumDisplayZoom));
			}
		}

		#endregion

		#region Public Methods

		public int? SubmitJob(AreaColorAndCalcSettings newValue)
		{
			CheckBlockSize(newValue);

			int? newJobNumber;

			lock (_paintLocker)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\n========== A new Job is being submitted, unbounded.");

				CheckViewPortSize();

				var lastSectionWasIncluded = false;

				// Unbounded
				_boundedMapArea = null;
				UnscaledExtent = new SizeDbl();

				if (newValue != CurrentAreaColorAndCalcSettings)
				{
					var previousValue = CurrentAreaColorAndCalcSettings;
					if (_useDetailedDebug) ReportSubmitJobDetails(previousValue, newValue, isBound: false);

					CurrentAreaColorAndCalcSettings = newValue;
					newJobNumber = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings, out lastSectionWasIncluded);

					if (newJobNumber.HasValue && lastSectionWasIncluded)
					{
						RaiseDisplayJobCompletedOnBackground(newJobNumber.Value);
						//DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
					}
				}
				else
				{
					newJobNumber = null;
				}
			}

			return newJobNumber;
		}

		private void RaiseDisplayJobCompletedOnBackground(int newJobNumber)
		{
			ThreadPool.QueueUserWorkItem(
			x =>
			{
				try
				{
					DisplayJobCompleted?.Invoke(this, newJobNumber);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Received error {e} from the ThreadPool QueueWorkItem DisplayJobCompleted");
                }
			});
		}

		public void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom)
		{
			// NOTE: SubmitJob may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId

			CheckBlockSize(newValue);

			lock (_paintLocker)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\n========== A new Job is being submitted: Size: {posterSize}, Display Position: {displayPosition}, Zoom: {displayZoom}.");

				CheckViewPortSize();

				var previousValue = CurrentAreaColorAndCalcSettings;
				ReportSubmitJobDetails(previousValue, newValue, isBound: true);

				_displayZoom = displayZoom;
				var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(displayZoom);

				var contentViewportSize = ViewportSize.Divide(displayZoom);
				var constrainedViewportSize = posterSize.Min(contentViewportSize);

				// Save the MapAreaInfo for the entire poster.
				_boundedMapArea = new BoundedMapArea(_mapJobHelper, newValue.MapAreaInfo, posterSize, constrainedViewportSize, baseFactor);

				// Make sure no content is loaded while we reset the PanAndZoom control.
				CurrentAreaColorAndCalcSettings = null;

				_theirDisplayPosition = _boundedMapArea.GetScaledDisplayPosition(displayPosition, out var unInvertedY);

				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== Raising the DisplaySettingsInitialized Event.");
				DisplaySettingsInitialized?.Invoke(this, new DisplaySettingsInitializedEventArgs(posterSize, _theirDisplayPosition, _displayZoom));

				CurrentAreaColorAndCalcSettings = newValue;

				// Trigger a ViewportChanged event on the PanAndZoomControl -- this will result in our UpdateViewportSizeAndPos method being called.
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== Setting the Unscaled Extent to complete the process of submitting the job.");
				UnscaledExtent = _boundedMapArea.PosterSize;
			}
		}

		/// <summary>
		/// User is changing the Zoom level (Zoom Control Scroll bar or Mouse wheel.)
		/// Updates the ImageSource (i.e., the Bitmap) with Counts, Escape Velocities, etc., for the specified contentOffset (aka Display Position.)
		/// NOTE: This may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId.
		/// </summary>
		/// <param name="contentViewportSize">UnscaledViewportSize divided by the ContentScale</param>
		/// <param name="contentOffset">The logical X and Y coordinates of the top, left-hand pixel of the current view, relative to the top, left-hand pixel of the current map (i.e. Project or Poster.</param>
		/// <param name="contentScale">The number of pixels used to show one sample point of the current Map.</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			int? newJobNumber;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings == null)
				{
					newJobNumber = null;
				}
				else
				{
					if (_boundedMapArea == null)
					{
						throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
					}

					Debug.WriteLineIf(_useDetailedDebug, $"UpdateViewportSizeAndPos is calling LoadNewView. ContentViewportSize: {contentViewportSize}. ContentScale: {contentScale}.");

					newJobNumber = LoadNewScaledView(CurrentAreaColorAndCalcSettings, _boundedMapArea, contentViewportSize, contentOffset, contentScale);
				}
			}

			return newJobNumber;
		}

		// User is changing the size of the app / control
		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset)
		{
			int? newJobNumber;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings == null)
				{
					newJobNumber = null;
				}
				else
				{
					if (_boundedMapArea == null)
					{
						throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
					}

					Debug.WriteLineIf(_useDetailedDebug, $"UpdateViewportSizeAndPos is calling LoadNewView. ContentViewportSize: {contentViewportSize}.");

					newJobNumber = LoadNewView(CurrentAreaColorAndCalcSettings, _boundedMapArea, contentViewportSize, contentOffset);
				}
			}

			return newJobNumber;
		}

		// User is Panning or using the horizontal scroll bar.
		public int? MoveTo(VectorDbl contentOffset)
		{
			int? newJobNumber;

			lock (_paintLocker)
			{
				if (_boundedMapArea == null || UnscaledExtent.IsNearZero())
				{
					throw new InvalidOperationException("Cannot call MoveTo, if the boundedMapArea is null or if the UnscaledExtent is zero.");
				}

				if (CurrentAreaColorAndCalcSettings == null)
				{
					throw new InvalidOperationException("Cannot call MoveTo, if the CurrentAreaColorAndCalcSettings is null.");
				}

				Debug.WriteLineIf(_useDetailedDebug, "\n==========  Executing MoveTo.");

				_theirDisplayPosition = contentOffset;

				// Get the MapAreaInfo subset for the given display position
				var mapAreaSubset = _boundedMapArea.GetView(_theirDisplayPosition);
				var jobType = _boundedMapArea.BaseFactor == 0 ? JobType.FullScale : JobType.ReducedScale;

				ReportMove(_boundedMapArea, contentOffset);

				newJobNumber = ReuseAndLoad(jobType, CurrentAreaColorAndCalcSettings, mapAreaSubset, reapplyColorMap: false, out var lastSectionWasIncluded);


				if (newJobNumber.HasValue && lastSectionWasIncluded)
				{
					DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
				}
			}

			return newJobNumber;
		}

		public void CancelJob()
		{
			CurrentAreaColorAndCalcSettings = null;

			lock (_paintLocker)
			{
				StopCurrentJobs(clearDisplay: true);
			}
		}

		public void PauseJob()
		{
			lock (_paintLocker)
			{
				StopCurrentJobs(clearDisplay: false);
			}
		}

		public int? RestartJob()
		{
			int? newJobNumber;

			lock (_paintLocker)
			{

				if (LastMapAreaInfo == null)
				{
					throw new InvalidOperationException("While restarting the job, the LastMapAreaInfo is null on call.");
				}

				if (CurrentAreaColorAndCalcSettings == null)
				{
					throw new InvalidOperationException("While restarting the job, the CurrentAreaColorAndCalcSettings is null.");
				}

				var jobType = _boundedMapArea == null
					? JobType.FullScale
					: _boundedMapArea.BaseFactor == 0
						? JobType.FullScale
						: JobType.ReducedScale;

				newJobNumber = ReuseAndLoad(jobType, CurrentAreaColorAndCalcSettings, LastMapAreaInfo, reapplyColorMap: false, out var lastSectionWasIncluded);

				if (newJobNumber.HasValue && lastSectionWasIncluded)
				{
					DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
				}
			}

			return newJobNumber;
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
			var sectionIsCurrent = ActiveJobNumbers.Contains(mapSection.JobNumber);

			if (sectionIsCurrent && mapSection.MapSectionVectors != null)
			{
				lock (_paintLocker)
				{
					_bitmapGrid.GetAndPlacePixels(mapSection, mapSection.MapSectionVectors);
					_mapSectionsPendingGeneration.Remove(mapSection);
				}
			}

			if (mapSection.IsLastSection)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
			}
		}

		#endregion

		#region Private Methods

		private int? LoadNewScaledView(AreaColorAndCalcSettings areaColorAndCalcSettings, BoundedMapArea boundedMapArea, SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			int? newJobNumber;
			bool lastSectionWasIncluded;

			var currentBaseFactor = boundedMapArea.BaseFactor;

			// Get the coordinates for the current view, i.e., the ContentViewportSize

			// The contentViewportSize is the actual canvas size, but scaled up by the contentScale
			// The ContentViewportSize is the logical display size, it is the minimum of
			//		a. UnscaledViewportSize divided by the ContentScale and
			//		b. The PosterSize scaled by the ContentScale

			// The ContentViewportSize is used to update the BoundedMapArea.

			// The ViewportSize property on this class stores the Logical Canvas Size.
			// This is the same as the ContentViewportSize, but scaled down by the BaseScale of 1, 0.5, 0.25, 0.125, etc., depending on how 'Zoomed Out' we are.
			// The BitmapGrid's LogicalViewportSize is synched with this ViewportSize.

			Debug.Assert(contentScale == _displayZoom, "The DisplayZoom does not equal the new ContentScale on the call to LoadNewView.");

			//_displayZoom = contentScale;
			var (baseFactor, _) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale);

			boundedMapArea.SetSizeAndScale(contentViewportSize, baseFactor);

			_theirDisplayPosition = contentOffset;

			var mapAreaSubset = boundedMapArea.GetView(_theirDisplayPosition);
			var jobType = boundedMapArea.BaseFactor == 0 ? JobType.FullScale : JobType.ReducedScale;

			ReportUpdateSizeAndPos(boundedMapArea, contentViewportSize, contentOffset);

			// Keep our ViewportSize property in sync.
			_viewportSize = mapAreaSubset.CanvasSize;

			if (boundedMapArea.BaseFactor == currentBaseFactor)
			{
				newJobNumber = ReuseAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset, reapplyColorMap: false, out lastSectionWasIncluded);
			}
			else
			{
				newJobNumber = DiscardAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset, out lastSectionWasIncluded);
			}

			//newJobNumber = DiscardAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset, out lastSectionWasIncluded);

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		private int? LoadNewView(AreaColorAndCalcSettings areaColorAndCalcSettings, BoundedMapArea boundedMapArea, SizeDbl contentViewportSize, VectorDbl contentOffset)
		{
			int? newJobNumber;
			bool lastSectionWasIncluded;

			var currentBaseFactor = boundedMapArea.BaseFactor;

			// Get the coordinates for the current view, i.e., the ContentViewportSize

			boundedMapArea.SetSize(contentViewportSize);

			_theirDisplayPosition = contentOffset;

			var mapAreaSubset = boundedMapArea.GetView(_theirDisplayPosition);
			var jobType = boundedMapArea.BaseFactor == 0 ? JobType.FullScale : JobType.ReducedScale;

			ReportUpdateSizeAndPos(boundedMapArea, contentViewportSize, contentOffset);

			// Note the CanvasSize the ConstrainedViewportSize (provided to this method from the PanAndZoomControl in the contentViewportSize argument.)
			// Multiplied by the BaseScale

			// As the ViewportSize property is set, it sets the BitmapGrid's LogicalViewportSize.

			// Were setting the ViewportSize backing value here 
			// because ReuseAndLoad is going to set the BitmapGrid's LogicalViewportSize from the mapAreaSubset.CanvasSize.
			_viewportSize = mapAreaSubset.CanvasSize;

			newJobNumber = ReuseAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset, reapplyColorMap: false, out lastSectionWasIncluded);

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
					var reapplyColorMap = previousJob == null ? true : ShouldReapplyColorMap(previousJob.ColorBandSet, newJob.ColorBandSet, previousJob.MapCalcSettings, newJob.MapCalcSettings);
					Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== HandleCurrentJobChanged is calling ReuseAndLoad.");
					newJobNumber = ReuseAndLoad(JobType.FullScale, newJob, screenAreaInfo, reapplyColorMap, out lastSectionWasIncluded);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== HandleCurrentJobChanged is calling DiscardAndLoad.");
					newJobNumber = DiscardAndLoad(JobType.FullScale, newJob, screenAreaInfo, out lastSectionWasIncluded);
				}
			}
			else
			{
				StopCurrentJobs(clearDisplay: true);
				lastSectionWasIncluded = false;
				newJobNumber = null;
			}

			return newJobNumber;
		}

		private int? ReuseAndLoad(JobType jobType, AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, bool reapplyColorMap, out bool lastSectionWasIncluded)
		{
			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionBuilder.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new List<MapSection>(MapSections);
			
			loadedSections.AddRange(_mapSectionsPendingGeneration);

			var sectionsToLoad = GetSectionsToLoadAndRemove(sectionsRequired, loadedSections, out var sectionsToRemove);

			int? result;

			if (sectionsToLoad.Count == 0 && sectionsToRemove.Count == 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, "ReuseAndLoad is performing a 'simple' update.");

				// Let our Bitmap Grid know about the change in View size.
				_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				_bitmapGrid.LogicalViewportSize = screenAreaInfo.CanvasSize;
				_bitmapGrid.CanvasControlOffset = screenAreaInfo.CanvasControlOffset;

				ImageOffset = screenAreaInfo.CanvasControlOffset;
				ColorBandSet = newJob.ColorBandSet;

				lastSectionWasIncluded = false;
				result = null;
			}
			else
			{
				var sectionsToCancel = new List<MapSection>();

				foreach (var section in sectionsToRemove)
				{
					if (_mapSectionsPendingGeneration.Contains(section))
					{
						_mapSectionsPendingGeneration.Remove(section);
						sectionsToCancel.Add(section);
					}
					else
					{
						MapSections.Remove(section);
						_mapSectionVectorProvider.ReturnMapSection(section);
						//sectionsToClear.Add(section);
					}
				}

				if (sectionsToCancel.Count > 0)
				{
					_mapLoaderManager.CancelRequests(sectionsToCancel);
				}

				// Let our Bitmap Grid know about the change in View size.
				_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				_bitmapGrid.LogicalViewportSize = screenAreaInfo.CanvasSize;
				_bitmapGrid.CanvasControlOffset = screenAreaInfo.CanvasControlOffset;

				ImageOffset = screenAreaInfo.CanvasControlOffset;
				ColorBandSet = newJob.ColorBandSet;

				var numberOfSectionsReturned = _bitmapGrid.ReDrawSections(reapplyColorMap);

				var numberOfRequestsCancelled = sectionsToCancel.Count;
				numberOfSectionsReturned += sectionsToRemove.Count - numberOfRequestsCancelled;
				Debug.WriteLineIf(_useDetailedDebug, $"Reusing Loaded Sections. Requesting {sectionsToLoad.Count} sections, Cancelling {numberOfRequestsCancelled} pending requests, returned {numberOfSectionsReturned} sections. " +
					$"Keeping {MapSections.Count} sections. The MapSection Pool has: {_mapSectionVectorProvider.MapSectionsVectorsInPool} sections.");

				if (sectionsToLoad.Count > 0)
				{
					var newMapSections = _mapLoaderManager.Push(jobType, newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady,
						out var newJobNumber, out var mapSectionsPendingGeneration);

					_mapSectionsPendingGeneration.AddRange(mapSectionsPendingGeneration);

					Debug.WriteLineIf(_useDetailedDebug, $"ReuseAndLoad: {newMapSections.Count} were found in the repo, {mapSectionsPendingGeneration.Count} are being generated.");

					_bitmapGrid.DrawSections(newMapSections);

					if (CLEAR_MAP_SECTIONS_PENDING_GENERATION)
					{
						// Clear all sections for which we are waiting to receive a MapSection.
						var numberCleared = _bitmapGrid.ClearSections(_mapSectionsPendingGeneration);
						var numberRequestedToClear = _mapSectionsPendingGeneration.Count;

						if (numberCleared != numberRequestedToClear)
						{
							var diff = numberRequestedToClear - numberCleared;
							Debug.WriteLineIf(_useDetailedDebug, $"{diff} MapSections were not cleared out of a total {numberRequestedToClear} requested.");
						}
						else
						{
							Debug.WriteLineIf(_useDetailedDebug, $"{numberCleared} MapSections were cleared.");
						}
					}

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

		private int DiscardAndLoad(JobType jobType, AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
		{
			// Let our Bitmap Grid know about the change in View size.
			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
			_bitmapGrid.LogicalViewportSize = screenAreaInfo.CanvasSize;
			_bitmapGrid.CanvasControlOffset = screenAreaInfo.CanvasControlOffset;
			
			StopCurrentJobs(clearDisplay: true);

			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionBuilder.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);

			var newMapSections = _mapLoaderManager.Push(jobType, newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsRequired, MapSectionReady,
					out var newJobNumber, out var mapSectionsPendingGeneration);

			_mapSectionsPendingGeneration.AddRange(mapSectionsPendingGeneration);

			Debug.WriteLineIf(_useDetailedDebug, $"DiscardAndLoad: {newMapSections.Count} were found in the repo, {mapSectionsPendingGeneration.Count} are being generated.");

			ImageOffset = screenAreaInfo.CanvasControlOffset;

			ColorBandSet = newJob.ColorBandSet;

			_bitmapGrid.DrawSections(newMapSections);
			lastSectionWasIncluded = mapSectionsPendingGeneration.Count == 0;

			AddJobNumber(newJobNumber);

			return newJobNumber;
		}

		private void StopCurrentJobs(bool clearDisplay)
		{
			var stopWatch = Stopwatch.StartNew();

			_mapLoaderManager.StopJobs(ActiveJobNumbers);
			ActiveJobNumbers.Clear();
			_mapSectionsPendingGeneration.Clear();

			var msToStopJobs = stopWatch.ElapsedMilliseconds;

			if (clearDisplay)
			{
				stopWatch.Restart();
				_bitmapGrid.ClearDisplay();
				var msToClearDisplay = stopWatch.ElapsedMilliseconds;
				//Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel took:{msToStopJobs}ms to Stop the Jobs and took {msToClearDisplay}ms to Clear the display.");
				Debug.WriteLine($"MapSectionDisplayViewModel took:{msToStopJobs}ms to Stop the Jobs and took {msToClearDisplay}ms to Clear the display.");
			}
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

		private bool ShouldReapplyColorMap(ColorBandSet previousColorBandSet, ColorBandSet newColorBandSet, MapCalcSettings previousCalcSettings, MapCalcSettings newCalcSettings)
		{
			if (newColorBandSet != previousColorBandSet)
			{
				return false;
			}

			if (newCalcSettings.CalculateEscapeVelocities != previousCalcSettings.CalculateEscapeVelocities)
			{
				return false;
			}

			//if (newCalcSettings.SaveTheZValues != previousCalcSettings.SaveTheZValues)
			//{
			//	return false;
			//}


			return false;
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

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSize(canonicalMapAreaInfo, canvasSize);

			return mapAreaInfoV1;
		}

		private MapAreaInfo GetScreenAreaInfoWithDiagnostics(MapAreaInfo2 canonicalMapAreaInfo, SizeDbl canvasSize)
		{
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSize(canonicalMapAreaInfo, canvasSize);

			// Just for diagnostics.
			var mapAreaInfoV2 = MapJobHelper.Convert(mapAreaInfoV1);
			CompareMapAreaAfterRoundTrip(canonicalMapAreaInfo, mapAreaInfoV2, mapAreaInfoV1);

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

			_mapSectionVectorProvider.ReturnMapSection(mapSection);
		}

		private void OnBitmapUpdate(WriteableBitmap bitmap)
		{
			ImageSource = bitmap;
		}

		#endregion

		#region Diagnostics

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
		private void CheckViewPortSize()
		{
			if (_useDetailedDebug)
				Debug.WriteLine($"At checkVPSize: ViewportSize: {ViewportSize}, DisplayZoom: {DisplayZoom}, MinZoom: {MinimumDisplayZoom}.");

			if (ViewportSize.Width < 0.1 || ViewportSize.Height < 0.1)
			{
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
		private void ReportSubmitJobDetails(AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings? newValue, bool isBound)
		{
			var currentJobId = previousValue?.JobId ?? ObjectId.Empty.ToString();
			var forClause = isBound ? "with bounds" : "without bounds";

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
		private void ReportMove(BoundedMapArea boundedMapArea, VectorDbl contentOffset/*, SizeDbl contentViewportSize*/)
		{
			var scaledDispPos = boundedMapArea.GetScaledDisplayPosition(contentOffset, out var unInvertedY);

			var x = scaledDispPos.X;
			var y = scaledDispPos.Y;


			var posterSize = boundedMapArea.PosterSize;
			var contentViewportSize = boundedMapArea.ContentViewportSize;

			Debug.WriteLine($"Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {contentViewportSize}. BaseScaleFactor: {boundedMapArea.BaseScale}.");
		}

		[Conditional("DEBUG2")]
		private void ReportUpdateSizeAndPos(BoundedMapArea boundedMapArea, SizeDbl viewportSize, VectorDbl contentOffset/*, double contentScale, double baseFactor*/)
		{

			// TODO: Update ReportUpdateSizeAndPos to report on what changed, compared to the LastMapAreaInfo.


			var scaledDispPos = boundedMapArea.GetScaledDisplayPosition(contentOffset, out var unInvertedY);

			var x = scaledDispPos.X;
			var y = scaledDispPos.Y;

			//var posterSize = boundedMapArea.PosterSize;
			//var scaledExtent = posterSize.Scale(boundedMapArea.ContentScale);

			//var physicalViewportSize = viewportSize.Scale(contentScale);

			//Debug.WriteLine($"Loading new view. Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {viewportSize}. ContentScale: {contentScale}, BaseFactor: {baseFactor}. " +
			//	$"Scaled Extent: {scaledExtent}, ViewportSize: {physicalViewportSize}.");

			var posterSize = boundedMapArea.PosterSize;

			Debug.WriteLine($"Loading new view. Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {viewportSize}. BaseScaleFactor: {boundedMapArea.BaseScale}.");

		}

		//private void CheckContentScale(SizeDbl unscaledExtent, SizeDbl contentViewportSize, double contentScale, double baseFactor, double relativeScale) 
		//{
		//	Debug.Assert(UnscaledExtent == BoundedMapArea?.PosterSize, "UnscaledExtent is out of sync.");

		//	var sanityContentScale = CalculateContentScale(UnscaledExtent, contentViewportSize);
		////  var sanityContentScale = contentViewPortSize.Divide(unscaledExtent);

		//	if (Math.Abs(sanityContentScale.Width - contentScale) > 0.01 && Math.Abs(sanityContentScale.Height - contentScale) > 0.01)
		//	{
		//		Debug.WriteLine($"Content Scale is Off. SanityCheck vs Value at UpdateViewPortSize: {sanityContentScale} vs {contentScale}.");
		//		//throw new InvalidOperationException("Content Scale is OFF!!");
		//	}

		//	Debug.WriteLine($"CHECK THIS: The MapSectionDisplayViewModel is UpdatingViewportSizeAndPos. ViewportSize:{contentViewportSize}, Scale:{contentScale}. BaseFactor: {baseFactor}, RelativeScale: {relativeScale}.");

		//}

		#endregion

		#region Experimental DisplayPosition

		//public double DisplayPositionX
		//{
		//	get => DisplayPosition.X;
		//	set
		//	{
		//		if (ScreenTypeHelper.IsDoubleChanged(value, _displayPosition.X, 0.00001))
		//		{
		//			_displayPosition = new VectorDbl(value, _displayPosition.Y);
		//			MoveTo(DisplayPosition);
		//			OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayPositionX));
		//		}
		//	}
		//}

		//public double DisplayPositionY
		//{
		//	get => DisplayPosition.Y;
		//	set
		//	{
		//		if (ScreenTypeHelper.IsDoubleChanged(value, _displayPosition.Y, 0.00001))
		//		{
		//			_displayPosition = new VectorDbl(_displayPosition.X, value);
		//			MoveTo(DisplayPosition);
		//			OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayPositionY));
		//		}
		//	}
		//}

		//public VectorDbl DisplayPosition
		//{
		//	get => _displayPosition;
		//	set
		//	{
		//		if (ScreenTypeHelper.IsVectorDblChanged(value, _displayPosition))
		//		{
		//			_displayPosition = value;
		//			OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayPosition));

		//			OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayPositionX));
		//			OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayPositionY));
		//		}
		//	}
		//}

		//public double DisplayPositionX { get; set; }

		//public double DisplayPositionY { get; set; }

		//public double DisplayPositionYInverted { get; set; }

		//public ValueTuple<VectorDbl, double>? ScaledDisplayPositionYInverted { get; set; }      // This is the value of the PanAndZoom control's ContentOffset.

		//public int? MoveToX(double displayPositionX)
		//{
		//	DisplayPositionX = displayPositionX;
		//	var result = MoveTo(new VectorDbl(DisplayPositionX, DisplayPositionY));
		//	return result;
		//}

		//public int? MoveToYInverted(double displayPositionYInverted)
		//{
		//	if (_boundedMapArea == null)
		//	{
		//		throw new InvalidOperationException("MoveToYInverted is only supported when there is a bounded context.");
		//	}

		//	DisplayPositionY = _boundedMapArea.GetInvertedYPos(displayPositionYInverted);

		//	var result = MoveTo(new VectorDbl(DisplayPositionX, DisplayPositionY));
		//	return result;
		//}

		//public VectorDbl GetCurrentDisplayPosition()
		//{
		//	if (_boundedMapArea == null)
		//	{
		//		throw new InvalidOperationException("GetCurrentDisplayPosition is only supported when there is a Bounded context.");
		//	}

		//	if (!ScaledDisplayPositionYInverted.HasValue)
		//	{
		//		throw new InvalidOperationException("There is no current value for the DisplayPosition.");
		//	}

		//	var displayPosition = _boundedMapArea.GetUnScaledDisplayPosition(ScaledDisplayPositionYInverted.Value.Item1, ScaledDisplayPositionYInverted.Value.Item2, out var unInvertedY);
		//	return displayPosition;
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
