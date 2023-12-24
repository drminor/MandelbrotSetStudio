﻿using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Fields

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly object _paintLocker;

		private BoundedMapArea? _boundedMapArea;

		private List<MapSectionRequest> _currentMapSectionRequests { get; set; }

		private List<MapSectionRequest> _requestsPendingGeneration { get; init; }	

		private MapPositionSizeAndDelta? _latestMapAreaInfo;

		private IBitmapGrid _bitmapGrid;
		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		private SizeDbl _unscaledExtent;
		private SizeDbl _viewportSize;
		private VectorInt _imageOffset;

		private VectorDbl _theirDisplayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;
		private double _maximumDisplayZoom;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionVectorProvider mapSectionVectorProvider, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapJobHelper = mapJobHelper;

			//BlockSize = blockSize;
			_mapSectionBuilder = new MapSectionBuilder();

			_paintLocker = new object();

			_boundedMapArea = null;
			MapSections = new ObservableCollection<MapSection>();

			_currentMapSectionRequests = new List<MapSectionRequest>();
			_requestsPendingGeneration = new List<MapSectionRequest>();
			_latestMapAreaInfo = null;

			_bitmapGrid = new BitmapGrid(MapSections, new SizeDbl(128), OnBitmapUpdate, blockSize);

			ActiveJobs = new List<MsrJob>();
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

		public event EventHandler<MapViewUpdateCompletedEventArgs>? MapViewUpdateCompleted;

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

				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel is updating the ColorBandSet. The Id is being updated from {_bitmapGrid.ColorBandSet.Id} to {value.Id}.");

				_bitmapGrid.ColorBandSet = value;
			}
		}

		public ColorBand? CurrentColorBand
		{
			get => _bitmapGrid.CurrentColorBand;
			//set => _bitmapGrid.CurrentColorBand = value;
		}

		public int CurrentColorBandIndex
		{
			get => _bitmapGrid.CurrentColorBandIndex;
			set => _bitmapGrid.CurrentColorBandIndex = value;
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

		public MapPositionSizeAndDelta? LastMapAreaInfo
		{
			get => _latestMapAreaInfo;
			private set { _latestMapAreaInfo = value; }
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		//public SizeInt BlockSize { get; init; }
		
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

						lock (_paintLocker)
						{
							if (CurrentAreaColorAndCalcSettings != null)
							{
								Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== As the ViewportSize is updated, the MapSectionDisplayViewModel is calling ReuseAndLoad.");

								var screenAreaInfo = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, value);
								var reApplyColorMap = screenAreaInfo.Coords.CrossesYZero;
									var msrJob = ReuseAndLoad(JobType.FullScale, CurrentAreaColorAndCalcSettings, screenAreaInfo, reapplyColorMap: reApplyColorMap);
								_ = msrJob.JobNumber;
							}
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

		public List<MsrJob> ActiveJobs { get; init; }

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
					Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's DisplayZoom is being updated from {_displayZoom} to {value}.");

					_displayZoom = value;
					OnPropertyChanged(nameof(IMapDisplayViewModel.DisplayZoom));
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's DisplayZoom is being updated to it's current value: {value}. No Change.");
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

		public MsrJob? SubmitJob(AreaColorAndCalcSettings newValue)
		{
			//CheckBlockSize(newValue);

			MsrJob? msrJob;

			lock (_paintLocker)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\n========== A new Job is being submitted, unbounded.");

				CheckViewportSize();

				// Unbounded
				_boundedMapArea = null;
				UnscaledExtent = new SizeDbl();

				if (newValue != CurrentAreaColorAndCalcSettings)
				{
					var previousValue = CurrentAreaColorAndCalcSettings;
					if (_useDetailedDebug) ReportSubmitJobDetails(previousValue, newValue, isBound: false);

					CurrentAreaColorAndCalcSettings = newValue;
					msrJob = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings);
				}
				else
				{
					msrJob = null;
				}
			}

			return msrJob;
		}

		public void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom)
		{
			// NOTE: SubmitJob may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId

			//CheckBlockSize(newValue);

			lock (_paintLocker)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\n========== A new Job is being submitted: Size: {posterSize}, Display Position: {displayPosition}, Zoom: {displayZoom}.");

				CheckViewportSize();

				var previousValue = CurrentAreaColorAndCalcSettings;
				if (_useDetailedDebug) ReportSubmitJobDetails(previousValue, newValue, isBound: true);

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
		public MsrJob? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			MsrJob? msrJob;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings == null)
				{
					msrJob = null;
				}
				else
				{
					if (_boundedMapArea == null)
					{
						throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
					}

					Debug.WriteLineIf(_useDetailedDebug, $"UpdateViewportSizeAndPos is calling LoadNewView. ContentViewportSize: {contentViewportSize}. ContentScale: {contentScale}.");

					msrJob = LoadNewScaledView(CurrentAreaColorAndCalcSettings, _boundedMapArea, contentViewportSize, contentOffset, contentScale);
				}
			}

			return msrJob;
		}

		// User is changing the size of the app / control
		public MsrJob? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset)
		{
			MsrJob? msrJob;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings == null)
				{
					msrJob = null;
				}
				else
				{
					if (_boundedMapArea == null)
					{
						throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
					}

					Debug.WriteLineIf(_useDetailedDebug, $"UpdateViewportSizeAndPos is calling LoadNewView. ContentViewportSize: {contentViewportSize}.");

					msrJob = LoadNewView(CurrentAreaColorAndCalcSettings, _boundedMapArea, contentViewportSize, contentOffset);
				}
			}

			return msrJob;
		}

		// User is Panning or using the horizontal scroll bar.
		public MsrJob MoveTo(VectorDbl contentOffset)
		{
			MsrJob msrJob;

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

				var reApplyColorMap = mapAreaSubset.Coords.CrossesYZero;
				msrJob = ReuseAndLoad(jobType, CurrentAreaColorAndCalcSettings, mapAreaSubset, reapplyColorMap: reApplyColorMap);
			}

			return msrJob;
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
				// Update the list of current requests to reflect that we are cancelling each pending request.
				RemoveCurrentRequests(_requestsPendingGeneration);

				StopCurrentJobs(clearDisplay: false);
			}
		}

		public MsrJob? RestartJob()
		{
			MsrJob? msrJob;

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

				msrJob = ReuseAndLoad(jobType, CurrentAreaColorAndCalcSettings, LastMapAreaInfo, reapplyColorMap: false);
			}

			return msrJob;
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				_bitmapGrid.ClearDisplay();

				foreach (var mapSection in MapSections)
				{
					_mapSectionVectorProvider.ReturnToPool(mapSection);
				}

				MapSections.Clear();
			}

			_mapSectionVectorProvider.ReportObjectPoolDetails();
		}

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e)
		{
			if (CurrentAreaColorAndCalcSettings != null)
			{
				var eventArgs = e.IsPreviewBeingCancelled
					? MapViewUpdateRequestedEventArgs.CreateCancelPreviewInstance(e.TransformType)
					: new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, e.PanAmount, e.Factor, e.ScreenArea, e.DisplaySize, e.AdjustedDisplaySize, CurrentAreaColorAndCalcSettings.MapAreaInfo, e.IsPreview);

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
			var mapSectionShouldBeUsed = false;

			lock (_paintLocker)
			{
				if (IsJobActive(mapSection.JobNumber))
				{
					mapSectionShouldBeUsed = true;
					RemovePendingRequest(mapSection); // TODO: Don't remove it until its been added to the MapSections
				}
				else
				{
					Debug.WriteLine($"GetAndPlacePixelsWrapper not drawing section: Its JobNumber: {mapSection.JobNumber} is not in the list of Active Job Numbers: {string.Join("; ", ActiveJobs)}.");
				}
			}

			if (mapSectionShouldBeUsed)
			{
				if (mapSection.MapSectionVectors != null)
				{
					if (!mapSection.RequestCancelled)
					{
						_bitmapGrid.Dispatcher.Invoke(DrawOneSectionWrapper, new object[] { mapSection });
					}
					else
					{
						Debug.WriteLine("MapSectionDisplayViewModel. MapSectionReady received a Cancelled MapSection.");
					}
				}
				else
				{
					Debug.WriteLine("WARNING!!: MapSectionDisplayViewModel. MapSectionReady received an Empty MapSection.");
				}
			}
		}

		private void DrawOneSectionWrapper(MapSection mapSection)
		{
			lock (_paintLocker)
			{
				if (mapSection.MapSectionVectors == null)
				{
					Debug.WriteLine("WARNING. Not Drawing Section. The MapSectionVectors is null here, but in the caller it was not null!");
				}
				else
				{
					_bitmapGrid.DrawOneSection(mapSection, mapSection.MapSectionVectors, "DrawOneAsync");
					MapSections.Add(mapSection);
					//RemovePendingRequest(mapSection); // TODO: Remove it here.
				}
			}
		}

		private void MapViewUpdateIsComplete(int jobNumber, bool isCancelled)
		{
			RaiseMapViewUpdateCompletedOnBackground(jobNumber, isCancelled);
		}

		private void RaiseMapViewUpdateCompletedOnBackground(int newJobNumber, bool isCancelled)
		{
			ThreadPool.QueueUserWorkItem(
			x =>
			{
				try
				{
					MapViewUpdateCompleted?.Invoke(this, new MapViewUpdateCompletedEventArgs(newJobNumber, isCancelled));
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Received error {e} from the ThreadPool QueueWorkItem DisplayJobCompleted");
				}
			});
		}

		private bool IsJobActive(int jobNumber)
		{
			for (var i = ActiveJobs.Count - 1; i >= 0; i--)
			{
				if (jobNumber == ActiveJobs[i].MapLoaderJobNumber)
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Private Methods

		private MsrJob LoadNewScaledView(AreaColorAndCalcSettings areaColorAndCalcSettings, BoundedMapArea boundedMapArea, SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			// TODO: Compare the currrent and new SubdivisionIds. If different, use DiscardAndLoad
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

			MsrJob msrJob;

			if (boundedMapArea.BaseFactor == currentBaseFactor)
			{
				var reApplyColorMap = mapAreaSubset.Coords.CrossesYZero;
				msrJob = ReuseAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset, reapplyColorMap: reApplyColorMap);
			}
			else
			{
				msrJob = DiscardAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset);
			}

			return msrJob;
		}

		private MsrJob LoadNewView(AreaColorAndCalcSettings areaColorAndCalcSettings, BoundedMapArea boundedMapArea, SizeDbl contentViewportSize, VectorDbl contentOffset)
		{
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

			var reApplyColorMap = mapAreaSubset.Coords.CrossesYZero;
			var msrJob = ReuseAndLoad(jobType, areaColorAndCalcSettings, mapAreaSubset, reapplyColorMap: reApplyColorMap);

			return msrJob;
		}

		private MsrJob? HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			MsrJob? msrJob;

			if (newJob != null && !newJob.IsEmpty)
			{
				var screenAreaInfo = GetScreenAreaInfo(newJob.MapAreaInfo, ViewportSize);

				ReportNewMapArea(LastMapAreaInfo, screenAreaInfo, ViewportSize);

				if (ShouldAttemptToReuseLoadedSections(previousJob, LastMapAreaInfo, newJob, screenAreaInfo))
				{
					//var reapplyColorMap = previousJob == null ? true : ShouldReapplyColorMap(previousJob.ColorBandSet, newJob.ColorBandSet, previousJob.MapCalcSettings, newJob.MapCalcSettings);
					var reapplyColorMap = ShouldReapplyColorMap(previousJob, LastMapAreaInfo, newJob, screenAreaInfo);

					Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== HandleCurrentJobChanged is calling ReuseAndLoad.");
					//Debug.WriteLine("\n\t\t====== HandleCurrentJobChanged is calling ReuseAndLoad.");
					msrJob = ReuseAndLoad(JobType.FullScale, newJob, screenAreaInfo, reapplyColorMap);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== HandleCurrentJobChanged is calling DiscardAndLoad.");
					//Debug.WriteLine("\n\t\t====== HandleCurrentJobChanged is calling DiscardAndLoad.");
					msrJob = DiscardAndLoad(JobType.FullScale, newJob, screenAreaInfo);
				}
			}
			else
			{
				StopCurrentJobs(clearDisplay: true);
				msrJob = null;
			}

			return msrJob;
		}

		private MsrJob DiscardAndLoad(JobType jobType, AreaColorAndCalcSettings newJob, MapPositionSizeAndDelta screenAreaInfo)
		{
			// Let our Bitmap Grid know about the change in View size.
			// These must be set before we call clear screen.
			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
			_bitmapGrid.LogicalViewportSize = screenAreaInfo.CanvasSize;
			_bitmapGrid.CanvasControlOffset = screenAreaInfo.CanvasControlOffset;

			StopCurrentJobs(clearDisplay: true);

			LastMapAreaInfo = screenAreaInfo;

			var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(jobType, newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings);

			var newMapExtentInBlocks = _bitmapGrid.ImageSizeInBlocks;
			_currentMapSectionRequests = _mapSectionBuilder.CreateSectionRequests(msrJob, newMapExtentInBlocks);

			ImageOffset = screenAreaInfo.CanvasControlOffset;
			ColorBandSet = newJob.ColorBandSet;

			if (_currentMapSectionRequests != null)
			{
				// ***** Submit the new requests. *****
				var requestsPendingGeneration = SubmitMSRequests(msrJob, _currentMapSectionRequests);
				_requestsPendingGeneration.AddRange(requestsPendingGeneration);
			}
			else
			{
				Debug.WriteLine($"Discard and Load: Not submitting MSRequests, the currentMapSectionRequests is null for job number: {msrJob.MapLoaderJobNumber}.");
			}

			return msrJob;
		}

		private MsrJob ReuseAndLoad(JobType jobType, AreaColorAndCalcSettings newJob, MapPositionSizeAndDelta screenAreaInfo, bool reapplyColorMap)
		{
			var prevMapExtentInBlocks = _bitmapGrid.ImageSizeInBlocks;

			LastMapAreaInfo = screenAreaInfo;

			// Let our Bitmap Grid know about the change in View size.
			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
 			_bitmapGrid.LogicalViewportSize = screenAreaInfo.CanvasSize;
			_bitmapGrid.CanvasControlOffset = screenAreaInfo.CanvasControlOffset;

			var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(jobType, newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings);

			var newMapExtentInBlocks = _bitmapGrid.ImageSizeInBlocks;
			var allRequestsForNewJob = _mapSectionBuilder.CreateSectionRequests(msrJob, newMapExtentInBlocks);

			CheckSubdivisions(allRequestsForNewJob, _currentMapSectionRequests);

			var newRequests = GetRequestsToLoadAndRemove(allRequestsForNewJob, _currentMapSectionRequests, out var requestsNoLongerNeeded);
			_currentMapSectionRequests = allRequestsForNewJob;

			// TODO: The ImageOffset and ColorBandSet -- may need to be called after the call to _bitMapGrid.RedrawSections
			ImageOffset = screenAreaInfo.CanvasControlOffset;
			ColorBandSet = newJob.ColorBandSet;

			var sectionsNotVisible = _bitmapGrid.GetSectionsNotVisible();

			if (newRequests.Count == 0 && requestsNoLongerNeeded.Count == 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ReuseAndLoad is performing a 'simple' update for job number: {msrJob.MapLoaderJobNumber}.");

				CheckSectionsNotVisible(_bitmapGrid);
			}
			else
			{ 
				// Cancel requests in play that are no longer needed. Remove and dispose MapSections no longer needed
				if (_useDetailedDebug)
				{
					var requestsForSectionsToRemove = CancelRequests(requestsNoLongerNeeded, _requestsPendingGeneration);
					var numberOfRequestsCancelled = requestsNoLongerNeeded.Count - requestsForSectionsToRemove.Count;

					var sectionsToRemoveViaReqs = FindSectionsToRemoveFromRequests(requestsForSectionsToRemove, MapSections);
					var numberOfSectionsRemovedViaReq = RemoveSections(sectionsToRemoveViaReqs, MapSections);
					var numberOfSectionsRemovedNotVis = RemoveSections(sectionsNotVisible, MapSections);
					var numberOfSectionsNotDrawn = _bitmapGrid.ReDrawSections(reapplyColorMap);

					if (numberOfSectionsNotDrawn > 0)
					{
						Debug.WriteLine($"WARNING: The BitmapGrid found {numberOfSectionsNotDrawn} sections whose visibility has changed since the method: GetSectionsNoVisible was called.");
					}

					if (prevMapExtentInBlocks != newMapExtentInBlocks)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"MapExtent is changing from: {prevMapExtentInBlocks} to {newMapExtentInBlocks}.");
					}

					ReportReuseAndLoadedSections(newRequests.Count, numberOfRequestsCancelled, MapSections.Count, requestsForSectionsToRemove.Count, numberOfSectionsRemovedViaReq, 
						sectionsNotVisible.Count, numberOfSectionsRemovedNotVis);
				} 
				else
				{
					var requestsForSectionsToRemove = CancelRequests(requestsNoLongerNeeded, _requestsPendingGeneration);
					
					var sectionsToRemoveViaReqs = FindSectionsToRemoveFromRequests(requestsForSectionsToRemove, MapSections);
					_ = RemoveSections(sectionsToRemoveViaReqs, MapSections);
					_ = RemoveSections(sectionsNotVisible, MapSections);
					_ = _bitmapGrid.ReDrawSections(reapplyColorMap);
				}

				if (newRequests.Count > 0)
				{
					// ***** Submit the new requests. *****
					var mapRequestsPendingGenration = SubmitMSRequests(msrJob, newRequests);
					_requestsPendingGeneration.AddRange(mapRequestsPendingGenration);
				}
				else
				{
					Debug.WriteLine($"Reuse and Load: Not submitting MSRequests, there are no new requests for job number: {msrJob.MapLoaderJobNumber}.");

				}

				if (_requestsPendingGeneration.Count > 0)
				{
					ClearMapSections(_requestsPendingGeneration);
				}
			}

			return msrJob;
		}

		private List<MapSectionRequest> SubmitMSRequests(MsrJob msrJob, List<MapSectionRequest> newRequests, [CallerMemberName] string? callerMemberName = null)
		{
			ReportNewRequests(newRequests);

			AddJob(msrJob);

			// This uses the callback property of the MsrJob.
			var newMapSections = _mapLoaderManager.Push(msrJob, newRequests, MapSectionReady, MapViewUpdateIsComplete, msrJob.CancellationTokenSource.Token, out var mapRequestsPendingGeneration);

			Debug.WriteLineIf(_useDetailedDebug, $"{callerMemberName}: {newMapSections.Count} were found in the repo, {mapRequestsPendingGeneration.Count} are being generated.");

			foreach (var mapSection in newMapSections)
			{
				MapSections.Add(mapSection);
			}

			_bitmapGrid.DrawSections(newMapSections);

			return mapRequestsPendingGeneration;
		}

		private void ClearMapSections(List<MapSectionRequest> requestsPendingGeneration)
		{
			// Clear all sections for which we are waiting to receive a MapSection.
			var mapSectionsToClear = new List<Tuple<int, PointInt, VectorLong>>();
			var mapSectionsToRemove = new List<MapSection>();

			foreach (var request in requestsPendingGeneration)
			{
				if (request.IsPaired)
				{
					mapSectionsToClear.Add(new Tuple<int, PointInt, VectorLong>(request.MapLoaderJobNumber, request.RegularPosition!.ScreenPosition, request.JobBlockOffset));

					var ms = MapSections.FirstOrDefault(x => !x.IsInverted & x.SectionBlockOffset == request.RegularPosition!.SectionBlockOffset);
					if (ms != null)
						mapSectionsToRemove.Add(ms);

					mapSectionsToClear.Add(new Tuple<int, PointInt, VectorLong>(request.MapLoaderJobNumber, request.InvertedPosition!.ScreenPosition,request.JobBlockOffset));

					ms = MapSections.FirstOrDefault(x => x.IsInverted & x.SectionBlockOffset == request.InvertedPosition!.SectionBlockOffset);
					if (ms != null)
						mapSectionsToRemove.Add(ms);
				}
				else if (request.RegularPosition != null)
				{
					mapSectionsToClear.Add(new Tuple<int, PointInt, VectorLong>(request.MapLoaderJobNumber, request.RegularPosition.ScreenPosition, request.JobBlockOffset));

					var ms = MapSections.FirstOrDefault(x => !x.IsInverted & x.SectionBlockOffset == request.RegularPosition.SectionBlockOffset);
					if (ms != null)
						mapSectionsToRemove.Add(ms);
				}
				else
				{
					mapSectionsToClear.Add(new Tuple<int, PointInt, VectorLong>(request.MapLoaderJobNumber, request.InvertedPosition!.ScreenPosition, request.JobBlockOffset));
					var ms = MapSections.FirstOrDefault(x => x.IsInverted & x.SectionBlockOffset == request.InvertedPosition!.SectionBlockOffset);
					if (ms != null)
						mapSectionsToRemove.Add(ms);
				}
			}

			var numberCleared = _bitmapGrid.ClearSections(mapSectionsToClear);

			var numberRemoved = 0;

			foreach (var ms in mapSectionsToRemove)
			{
				if (MapSections.Remove(ms))
				{
					numberRemoved++;
					_mapSectionVectorProvider.ReturnToPool(ms);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, "Could not find a MapSection matching a pending request.");
				}
			}

			ReportClearMapSections(numberCleared, numberRemoved);
		}

		private void StopCurrentJobs(bool clearDisplay)
		{
			var stopWatch = Stopwatch.StartNew();

			//_mapLoaderManager.StopJobs(ActiveJobNumbers);

			foreach(var msrJob in ActiveJobs)
			{
				msrJob.Cancel();
			}

			ActiveJobs.Clear();

			foreach(var request in _requestsPendingGeneration)
			{
				request.Cancel();
			}

			_requestsPendingGeneration.Clear();

			var msToStopJobs = stopWatch.ElapsedMilliseconds;

			if (clearDisplay)
			{
				stopWatch.Restart();
				_bitmapGrid.ClearDisplay();

				foreach (var mapSection in MapSections)
				{
					_mapSectionVectorProvider.ReturnToPool(mapSection);
				}

				MapSections.Clear();

				var msToClearDisplay = stopWatch.ElapsedMilliseconds;
				Debug.WriteLineIf(_useDetailedDebug, $"MapSectionDisplayViewModel took:{msToStopJobs}ms to Stop the Jobs and took {msToClearDisplay}ms to Clear the display.");

				_mapSectionVectorProvider.ReportObjectPoolDetails();
			}
		}

		private void AddJob(MsrJob msrJob)
		{
			ActiveJobs.Add(msrJob);
			Debug.WriteLineIf(_useDetailedDebug, $"Adding job number: {msrJob.MapLoaderJobNumber}. There are now {ActiveJobs.Count} active jobs.");
		}

		private bool ShouldAttemptToReuseLoadedSections(AreaColorAndCalcSettings? previousJob, MapPositionSizeAndDelta? previousAreaInfo, AreaColorAndCalcSettings newJob, MapPositionSizeAndDelta newAreaInfo)
		{
			// TODO: Try this without requiring the previousAreaInfo to be non-null.
			if (MapSections.Count == 0 || previousJob is null || previousAreaInfo is null)
			{
				return false;
			}

			if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
			{
				return false;
			}

			var inSameSubdivision = newJob.MapAreaInfo.Subdivision.Id == previousJob.MapAreaInfo.Subdivision.Id;

			if (!inSameSubdivision)
			{
				return false;
			}

			// TODO: Remove the ImageSize and CanvasSize checks in the ShouldAttemptToReuseLoadedSections method.
			var curImageSize = _bitmapGrid.ImageSizeInBlocks;
			var newImageSize = _bitmapGrid.CalculateImageSize(newAreaInfo.CanvasSize, newAreaInfo.CanvasControlOffset);

			//if (newImageSize.Width > curImageSize.Width || newImageSize.Height != curImageSize.Height)
			if (newImageSize.Height != curImageSize.Height)
			{
				//Debug.WriteLine("WARNING: Using ReuseAndLoad even though the ImageSize has changed.");
				return false;
			}

			var curCanvasSize = _bitmapGrid.CanvasSizeInBlocks;
			var newCanvasSize = _bitmapGrid.CalculateCanvasSize(newAreaInfo.CanvasSize);

			if (newCanvasSize != curCanvasSize)
			{
				Debug.WriteLine("WARNING: Not using ReuseAndLoad because the CanvasSize has changed. THIS SHOULD NEVER HAPPEN.");
				return false;
			}

			return true;
		}

		private bool ShouldReapplyColorMap(AreaColorAndCalcSettings? previousJob, MapPositionSizeAndDelta? previousAreaInfo, AreaColorAndCalcSettings newJob, MapPositionSizeAndDelta newAreaInfo)
		{
			if (previousJob == null || previousAreaInfo == null)
			{
				return true;
			}

			if (previousAreaInfo.Coords.CrossesYZero || newAreaInfo.Coords.CrossesYZero)
			{
				Debug.WriteLineIf(_useDetailedDebug, "Reapplying the ColorMap: Either the previous, the new or both maps cross the YZero point.");
				return true;
			}

			if (newJob.ColorBandSet != previousJob.ColorBandSet)
			{
				return true;
			}

			if (newJob.MapCalcSettings.CalculateEscapeVelocities != previousJob.MapCalcSettings.CalculateEscapeVelocities)
			{
				return true;
			}

			Debug.WriteLineIf(_useDetailedDebug, "Not reapplying the ColorMap.");
			return false;
		}


		private bool ShouldReapplyColorMapOld(ColorBandSet previousColorBandSet, ColorBandSet newColorBandSet, MapCalcSettings previousCalcSettings, MapCalcSettings newCalcSettings)
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

		private List<MapSectionRequest> GetRequestsToLoadAndRemove(List<MapSectionRequest> newRequests, List<MapSectionRequest> existingRequests, out List<MsrPosition> requestsNoLongerNeeded)
		{
			var result = new List<MapSectionRequest>(newRequests);

			requestsNoLongerNeeded = new List<MsrPosition>();

			foreach (var existingReq in existingRequests)
			{
				var alreadyPresent = newRequests.Where(x => x.SectionBlockOffset == existingReq.SectionBlockOffset);

				var foundCnt = alreadyPresent.Count();

				if (foundCnt == 0)
				{
					// The existing request could not be matched to any new request.
					// We will not be needing this request any longer
					if (existingReq.HasRegular) 
					{
						requestsNoLongerNeeded.Add(existingReq.RegularPosition!);
					}
					if (existingReq.HasInverted)
					{
						requestsNoLongerNeeded.Add(existingReq.InvertedPosition!);
					}
				}
				else
				{
					Debug.Assert(foundCnt == 1, "foundCnt should be 1 here.");

					var newReq = alreadyPresent.First();

					if (newReq.IsPaired)
					{
						// Can only remove the newReq from Result, if the existingReq also has a mirror.
						if (existingReq.IsPaired)
						{
							// The existing request is for both regular and inverted.
							// The new request is also for both regular and inverted,
							// The new request is not needed.
							result.Remove(newReq);
						}
						else
						{
							// The exiting request is for either regular or inverted
							// The new request is for both regular and inverted.

							//// Cancel the portion of the new request, covered by the existing request
							//if (existingReq.IsInverted)
							//{
							//	if (newReq.IsInverted)
							//	{
							//		newReq.Cancelled = true;
							//	}
							//	else
							//	{
							//		newReq.Mirror.Cancelled = true;
							//	}
							//}
							//else
							//{
							//	if (newReq.IsInverted)
							//	{
							//		newReq.Mirror.Cancelled = true;
							//	}
							//	else
							//	{
							//		newReq.Cancelled = true;
							//	}
							//}

							// Cancel the existing request -- the new request includes both
							if (existingReq.HasRegular)
							{
								requestsNoLongerNeeded.Add(existingReq.RegularPosition!);
							}
							else
							{
								requestsNoLongerNeeded.Add(existingReq.InvertedPosition!);
							}
						}
					}
					else
					{
						if (existingReq.IsPaired)
						{
							// The exiting request is for both regular and inverted
							// The new request is for either regular or inverted.
							if (newReq.HasInverted)
							{
								// Cancel the Regular component of the existing request
								// Do not keep the new request.
								//existingReq.MapSectionId = "CancelOnlyRegular";
								requestsNoLongerNeeded.Add(existingReq.RegularPosition!);

							}
							else
							{
								// Cancel the IsInverted component of the existing request
								// Do not keep the new request.
								//existingReq.MapSectionId = "CancelOnlyInverted";
								requestsNoLongerNeeded.Add(existingReq.InvertedPosition!);
							}

							result.Remove(newReq);
						}
						else
						{
							// The exiting request is for either regular or inverted
							// The new request is for either regular or inverted.
							if (existingReq.HasInverted != newReq.HasInverted)
							{
								// The existing request is no longer needed.
								if (existingReq.HasRegular)
								{
									requestsNoLongerNeeded.Add(existingReq.RegularPosition!);
								}
								else
								{
									requestsNoLongerNeeded.Add(existingReq.InvertedPosition!);
								}
							}
							else
							{
								// The new request is not needed -- the existing request is asking for the same.
								result.Remove(newReq);
							}
						}

					}


				}
			}

			return result;
		}

		private List<MsrPosition> CancelRequests(List<MsrPosition> requestsNoLongerNeeded, List<MapSectionRequest> existingRequests)
		{
			List<MsrPosition> sectionsToRemove = new List<MsrPosition>();

			foreach (var msrPosition in requestsNoLongerNeeded)
			{
				var mapSectionRequest = FindMapSectionRequest(msrPosition, existingRequests);

				if (mapSectionRequest != null)
				{
					mapSectionRequest.Cancel(msrPosition.IsInverted);

					if (mapSectionRequest.NeitherRegularOrInvertedRequestIsInPlay)
					{
						existingRequests.Remove(mapSectionRequest);
					}
				}
				else
				{
					// No Pending Request, so lets try to find an existing section to remove.
					sectionsToRemove.Add(msrPosition);
				}
			}

			return sectionsToRemove;
		}

		private List<MapSection> FindSectionsToRemoveFromRequests(List<MsrPosition> mapSectionRequests, IList<MapSection> listOfSectionsToSearch)
		{
			List<MapSection> result = new List<MapSection>();

			foreach (var msrPosition in mapSectionRequests)
			{
				var mapSection = FindMapSection(msrPosition, listOfSectionsToSearch);
				if (mapSection != null)
				{
					result.Add(mapSection);
				}
			}

			return result;
		}

		private int RemoveSections(List<MapSection> mapSectionsToRemove, IList<MapSection> listOfSections)
		{
			var numberOfMapSectionsRemoved = 0;

			foreach (var mapSection in mapSectionsToRemove)
			{
				if (listOfSections.Remove(mapSection))
				{
					numberOfMapSectionsRemoved++;
					_mapSectionVectorProvider.ReturnToPool(mapSection);
				}
			}

			return numberOfMapSectionsRemoved;
		}

		private bool RemovePendingRequest(MapSection mapSection)
		{
			var subId = new ObjectId(mapSection.SubdivisionId);
			var request = _requestsPendingGeneration.Find(x => x.Subdivision.Id == subId && x.SectionBlockOffset == mapSection.SectionBlockOffset);

			if (request != null)
			{
				var result = _requestsPendingGeneration.Remove(request);
				return result;
			}
			else
			{
				return false;
			}
		}

		private void RemoveCurrentRequests(List<MapSectionRequest> requestsToRemove)
		{
			foreach (var request in requestsToRemove)
			{
				var curReq = _currentMapSectionRequests.FirstOrDefault(x => x.SectionBlockOffset == request.SectionBlockOffset);

				if (curReq != null)
				{
					_currentMapSectionRequests.Remove(curReq);
				}
			}
		}

		private MapSection? FindMapSection(MsrPosition msrPosition, IList<MapSection> mapSections)
		{
			return mapSections.FirstOrDefault(x => x.IsInverted == msrPosition.IsInverted && x.SectionBlockOffset == msrPosition.SectionBlockOffset);
		}

		private MapSectionRequest? FindMapSectionRequest(MsrPosition msr, List<MapSectionRequest> mapSectionRequests)
		{
			MapSectionRequest? result;

			if (msr.IsInverted)
			{
				result = mapSectionRequests.Find(x => x.HasInverted && x.SectionBlockOffset == msr.SectionBlockOffset);
			}
			else
			{
				result = mapSectionRequests.Find(x => (x.HasRegular) && x.SectionBlockOffset == msr.SectionBlockOffset);
			}

			return result;
		}

		private MapPositionSizeAndDelta GetScreenAreaInfo(MapCenterAndDelta mapAreaInfo, SizeDbl canvasSize)
		{
			if (canvasSize.IsNAN())
			{
				throw new InvalidOperationException("canvas size is undefined.");
			}

			var mapAreaInfoV1 = _mapJobHelper.GetMapPositionSizeAndDelta(mapAreaInfo, canvasSize);

			return mapAreaInfoV1;
		}

		private MapPositionSizeAndDelta GetScreenAreaInfoWithDiagnostics(MapCenterAndDelta mapAreaInfo, SizeDbl canvasSize)
		{
			var mapAreaInfoV1 = _mapJobHelper.GetMapPositionSizeAndDelta(mapAreaInfo, canvasSize);

			// Just for diagnostics.
			var mapAreaInfoV2 = MapJobHelper.Convert(mapAreaInfoV1);
			CompareMapAreaAfterRoundTrip(mapAreaInfo, mapAreaInfoV2, mapAreaInfoV1);

			return mapAreaInfoV1;
		}

		private void OnBitmapUpdate(WriteableBitmap bitmap)
		{
			ImageSource = bitmap;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void CompareMapAreaAfterRoundTrip(MapCenterAndDelta previousValue, MapCenterAndDelta newValue, MapPositionSizeAndDelta middleValue)
		{
			Debug.WriteLine($"MapDisplay is RoundTripping MapAreaInfo" +
				$"\nPrevious Scale: {previousValue.SamplePointDelta.Width}. Pos: {previousValue.MapCenter}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
				$"\nNew Scale     : {newValue.SamplePointDelta.Width}. Pos: {newValue.MapCenter}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}" +
				$"\nIntermediate  : {middleValue.SamplePointDelta.Width}. Pos: {middleValue.Coords}. MapOffset: {middleValue.MapBlockOffset}. ImageOffset: {middleValue.CanvasControlOffset} Size: {middleValue.CanvasSize}.");
		}

		[Conditional("DEBUG2")]
		private void CheckViewportSize()
		{
			if (_useDetailedDebug)
				Debug.WriteLine($"At checkVPSize: ViewportSize: {ViewportSize}, DisplayZoom: {DisplayZoom}, MinZoom: {MinimumDisplayZoom}.");

			if (ViewportSize.Width < 0.1 || ViewportSize.Height < 0.1)
			{
				throw new InvalidOperationException("ViewportSize is zero at CheckVPSize.");
			}
		}

		//[Conditional("NEVER")]
		//private void CheckBlockSize(AreaColorAndCalcSettings newValue)
		//{
		//	if (newValue.MapAreaInfo.Subdivision.BlockSize != BlockSize)
		//	{
		//		throw new ArgumentException("BlockSize mismatch", nameof(AreaColorAndCalcSettings.MapAreaInfo.Subdivision));
		//	}
		//}

		[Conditional("DEBUG2")]
		private void CheckSubdivisions(List<MapSectionRequest> newRequests, List<MapSectionRequest> existingRequests)
		{
			if (newRequests.Count == 0)
			{
				return;
			}

			var subdivisionId = newRequests.First().Subdivision.Id;

			var foundDiffSubInNew = newRequests.Any(x => x.Subdivision.Id != subdivisionId);
			var foundDiffSubInExisting = existingRequests.Any(x => x.Subdivision.Id != subdivisionId);

			Debug.Assert(!(foundDiffSubInNew || foundDiffSubInExisting), "All SubdivisionIds should be the same here.");
		}

		[Conditional("DEBUG2")]
		private void CheckSectionsNotVisible(IBitmapGrid bitmapGrid)
		{
			var sectionsNotVisible = _bitmapGrid.GetSectionsNotVisible();
			Debug.Assert(sectionsNotVisible.Count == 0, "The number of sections not visible should be zero here.");

		}

		[Conditional("DEBUG")]
		private void ReportSubmitJobDetails(AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings? newValue, bool isBound)
		{
			var currentJobId = previousValue?.JobId ?? ObjectId.Empty;
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

				Debug.WriteLine($"MapDisplay is handling SumbitJob. CurrentJobId: {currentJobId}. NewJobId: {newJobId}. SPD: {newValue.MapAreaInfo.SamplePointDelta.Width}, SubId: {newValue.MapAreaInfo.Subdivision.Id}");
			}
		}

		[Conditional("DEBUG2")]
		private void ReportNewRequests(List<MapSectionRequest> newRequests)
		{
			var newRequestsReport = _mapSectionBuilder.GetCountRequestsReport(newRequests);
			Debug.WriteLine(newRequestsReport);
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

		[Conditional("DEBUG")]
		private void ReportNewMapArea(MapPositionSizeAndDelta? previousValue, MapPositionSizeAndDelta newValue, SizeDbl viewportSize)
		{
			if (previousValue != null)
			{
				Debug.WriteLine($"MapSectionDisplayViewModel is handling CurrentJobChanged. The viewport Size is {viewportSize} " +
					$"\nPrevious Size: {previousValue.CanvasSize}. Pos: {previousValue.Coords.Position}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} " +
					$"\nNew Size: {newValue.CanvasSize}. Pos: {newValue.Coords.Position}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset}.");

				Debug.WriteLine($"UpdateSize is moving the pos from {previousValue.Coords.Position} to {newValue.Coords.Position}.");
			}
			else
			{
				Debug.WriteLine($"MapSectionDisplayViewModel is handling CurrentJobChanged. The Previous MapAreaInfo is null.");
			}
		}

		[Conditional("DEBUG2")]
		private void ReportReuseAndLoadedSections(int numberRequested, int numberOfRequestsCancelled, int numberOfMapSections, int numberOfSectionsToRemove, int numberOfSectionsRemovedViaReq, /*int numberOfSectionsRemovedViaReqTest,*/
			int numberOfSectionsNotVisible, int numberOfSectionsRemovedNotVis)
		{
			//if (_useDetailedDebug)
			//{

			var requestsToRemoveNotFound = numberOfSectionsToRemove - numberOfSectionsRemovedViaReq;
			var hiddenSectionsNotFound = numberOfSectionsNotVisible - numberOfSectionsRemovedNotVis;

			Debug.WriteLine(string.Empty); // Insert a blank line in the on-screen output.

			Debug.WriteLine($"Reusing Loaded Sections. "
				+ $"Requesting {numberRequested} new sections, "
				+ $"Cancelling {numberOfRequestsCancelled} pending requests, "
				+ $"returned {numberOfSectionsRemovedViaReq} unneeded sections, "
				//+ $"compare with number returned {numberOfSectionsRemovedViaReqTest} unneeded sections using all, "
				+ $"returned {numberOfSectionsRemovedNotVis} sections off the map, "
				+ (requestsToRemoveNotFound > 0 ? $"{requestsToRemoveNotFound} sectionRequests to remove a MapSection not found, " : string.Empty)
				+ (hiddenSectionsNotFound > 0 ? $"{hiddenSectionsNotFound} sections off the map to be removed but not found " : string.Empty)
				//+ $"{numberOfSectionsNotDrawn} sections had their visibility changed during this method call!! "
				+ $"Keeping {numberOfMapSections} sections. "
				+ $"The MapSection Pool has: {_mapSectionVectorProvider.MapSectionsVectorsInPool} sections. "); ;
			//}

			Debug.WriteLine(string.Empty); // Insert a blank line in the on-screen output.
		}

		[Conditional("DEBUG2")]
		private void ReportClearMapSections(int numberCleared, int numberRemoved)
		{
			// TODO: Move this logic to a conditonal method.
			var numberRequestedToClear = _requestsPendingGeneration.Count;

			if (numberCleared != numberRequestedToClear || numberRemoved != numberRequestedToClear)
			{
				var diff = numberRequestedToClear - numberCleared;
				Debug.WriteLineIf(_useDetailedDebug, $"{diff} MapSections were not cleared out of a total {numberRequestedToClear} requested.");

				diff = numberRequestedToClear - numberRemoved;
				Debug.WriteLineIf(_useDetailedDebug, $"{diff} MapSections were not removed out of a total {numberRequestedToClear} requested.");
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"{numberCleared} MapSections were cleared and removed.");
			}
		}

		private string GetMapSectionDetailsOnePerLine(IList<MapSection> mapSections)
		{
			var sb = new StringBuilder();

			for (var i = 0; i < mapSections.Count; i++)
			{
				var section = mapSections[i];

				if (i > 0)
				{
					sb.Append("\n");
				}

				sb.Append(i).Append("\t")
				.Append(section.ScreenPosition.X).Append("\t")
				.Append(section.ScreenPosition.Y).Append("\t")
				.Append(section.JobNumber).Append("\t")
				.Append(section.SectionBlockOffset.X).Append("\t")
				.Append(section.SectionBlockOffset.Y).Append("\t");

				if (section.IsInverted)
				{
					sb.Append(" (Inverted)");
				}
			}

			return sb.ToString();
		}

		private bool AreMapSectionsTheSame(List<MapSection> list1, List<MapSection> list2)
		{
			foreach (var ms in list1)
			{
				if (!list2.Contains(ms))
				{
					return false;
				}
			}

			return true;
		}

		//private void CheckContentScale(SizeDbl unscaledExtent, SizeDbl contentViewportSize, double contentScale, double baseFactor, double relativeScale) 
		//{
		//	Debug.Assert(UnscaledExtent == BoundedMapArea?.PosterSize, "UnscaledExtent is out of sync.");

		//	var sanityContentScale = CalculateContentScale(UnscaledExtent, contentViewportSize);
		////  var sanityContentScale = contentViewportSize.Divide(unscaledExtent);

		//	if (Math.Abs(sanityContentScale.Width - contentScale) > 0.01 && Math.Abs(sanityContentScale.Height - contentScale) > 0.01)
		//	{
		//		Debug.WriteLine($"Content Scale is Off. SanityCheck vs Value at UpdateViewportSize: {sanityContentScale} vs {contentScale}.");
		//		//throw new InvalidOperationException("Content Scale is OFF!!");
		//	}

		//	Debug.WriteLine($"CHECK THIS: The MapSectionDisplayViewModel is UpdatingViewportSizeAndPos. ViewportSize:{contentViewportSize}, Scale:{contentScale}. BaseFactor: {baseFactor}, RelativeScale: {relativeScale}.");

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
	}
}
