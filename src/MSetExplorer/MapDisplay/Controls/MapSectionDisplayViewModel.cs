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
		private VectorDbl _imageOffset;

		private VectorDbl _displayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;
		private double _maximumDisplayZoom;

		private bool _useDetailedDebug = true;

		//private ScaledImageViewInfo _viewportSizePositionAndScale;

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

			//_viewportSizePositionAndScale = ScaledImageViewInfo.Zero;

			_boundedMapArea = null;
			MapSections = new ObservableCollection<MapSection>();

			_mapSectionsPendingGeneration = new List<MapSection>();
			_latestMapAreaInfo = null;

			_bitmapGrid = new BitmapGrid(MapSections, new SizeDbl(128), DisposeMapSection, OnBitmapUpdate, blockSize);

			ActiveJobNumbers = new List<int>();
			_currentAreaColorAndCalcSettings = null;

			_unscaledExtent = new SizeDbl();
			_viewportSize = new SizeDbl();
			_imageOffset = new VectorDbl();
			_displayPosition = new VectorDbl();

			_minimumDisplayZoom = 0.015625; // 0.0625;
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
		/// Scaled, aka Logical Display Size.
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
						_bitmapGrid.ContentViewportSize = value;

						// TODO: Isn't this required for the 'plain' MapSectionDisplayControl?
						int? newJobNumber = null;
						bool lastSectionWasIncluded = false;

						lock (_paintLocker)
						{
							if (CurrentAreaColorAndCalcSettings != null)
							{
								var screenAreaInfo = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, value);
								newJobNumber = ReuseAndLoad(JobType.FullScale, CurrentAreaColorAndCalcSettings, screenAreaInfo, out lastSectionWasIncluded);
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

		//public ScaledImageViewInfo ViewportSizePositionAndScale
		//{
		//	get => _viewportSizePositionAndScale;
		//	set
		//	{
		//		int? newJobNumber;

		//		lock (_paintLocker)
		//		{
		//			if (CurrentAreaColorAndCalcSettings == null)
		//			{
		//				ViewportSize = value.ContentViewportSize;

		//				_displayZoom = value.ContentScale;
		//				_displayPosition = value.ContentOffset;
		//				newJobNumber = null;
		//			}
		//			else
		//			{
		//				if (_boundedMapArea == null)
		//				{
		//					throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
		//				}

		//				var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(value.ContentScale);

		//				// TODO: Make the CheckContentScale work correctly.
		//				//CheckContentScale(UnscaledExtent, contentViewportSize, contentScale, baseFactor, relativeScale);

		//				if (ScreenTypeHelper.IsSizeDblChanged(value.ContentViewportSize, ViewportSize, 0.0001))
		//				{
		//					Debug.WriteLine("On setting the ViewportSizePositionAndScale, the ViewportSize is not already set.");
		//					ViewportSize = value.ContentViewportSize;
		//				}

		//				if (value.ContentScale != _displayZoom)
		//				{
		//					Debug.WriteLine("On setting the ViewportSizePositionAndScale, the DisplayZoom is not already set.");
		//					_displayZoom = value.ContentScale;
		//				}


		//				if (value.ContentOffset == _displayPosition)
		//				{
		//					Debug.WriteLine("On setting the ViewportSizePositionAndScale, the DisplayPosition is not already set.");
		//					_displayPosition = value.ContentOffset;
		//				}

		//				newJobNumber = LoadNewView(CurrentAreaColorAndCalcSettings, _boundedMapArea, value.ContentViewportSize, value.ContentOffset, baseFactor);
		//			}
		//		}
		//	}
		//}

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

		public VectorDbl ImageOffset
		{
			get => _imageOffset;
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(_imageOffset, value, 0.00001))
				{
					//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_imageOffset = value;

					OnPropertyChanged(nameof(IMapDisplayViewModel.ImageOffset));
				}
			}
		}

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


		public double DisplayPositionX { get; set; }

		public double DisplayPositionY { get; set; }

		public double DisplayPositionYInverted { get; set; }

		public ValueTuple<VectorDbl, double>? ScaledDisplayPositionYInverted { get; set; }		// This is the value of the PanAndZoom control's ContentOffset.


		public VectorDbl GetCurrentDisplayPosition()
		{
			if (_boundedMapArea == null)
			{
				throw new InvalidOperationException("GetCurrentDisplayPosition is only supported when there is a Bounded context.");
			}

			if (!ScaledDisplayPositionYInverted.HasValue)
			{
				throw new InvalidOperationException("There is no current value for the DisplayPosition.");
			}

			var displayPosition = _boundedMapArea.GetUnScaledDisplayPosition(ScaledDisplayPositionYInverted.Value.Item1, ScaledDisplayPositionYInverted.Value.Item2, out var unInvertedY);
			return displayPosition;
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				var previousValue = _displayZoom;

				if (ScreenTypeHelper.IsDoubleChanged(value, _displayZoom, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF))
				{
					_displayZoom = value;

					Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's DisplayZoom is being updated to {DisplayZoom}, the previous value is {previousValue}.");
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
						DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
					}
				}
				else
				{
					newJobNumber = null;
				}
			}

			return newJobNumber;
		}

		//public void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom)
		//{
		//	CheckBlockSize(newValue);

		//	//int? newJobNumber;

		//	lock (_paintLocker)
		//	{
		//		Debug.WriteLine($"\n\n========== A new Job is being submitted: Size: {posterSize}, Display Position: {displayPosition}, Zoom: {displayZoom}.");

		//		CheckViewPortSize();

		//		var previousValue = CurrentAreaColorAndCalcSettings;
		//		ReportSubmitJobDetails(previousValue, newValue, isBound: true);

		//		// Make sure no content is loaded while we reset the PanAndZoom control.
		//		CurrentAreaColorAndCalcSettings = null;

		//		Debug.WriteLine("========== Raising the DisplaySettingsInitialized Event.");
		//		DisplaySettingsInitialized?.Invoke(this, new DisplaySettingsInitializedEventArgs(posterSize, _displayPosition/*, _minimumDisplayZoom, maximumDisplayZoom*/, _displayZoom));

		//		Debug.WriteLine("========== Resetting the Unscaled Extent.");
		//		UnscaledExtent = new SizeDbl();

		//		_displayZoom = 1.0d;

		//		var (b, r) = ContentScalerHelper.GetBaseFactorAndRelativeScale(_displayZoom);

		//		// Save the MapAreaInfo for the entire poster.
		//		_boundedMapArea = new BoundedMapArea(_mapJobHelper, newValue.MapAreaInfo, posterSize, ViewportSize)
		//		{
		//			BaseFactor = b
		//		};

		//		// Get the MapAreaInfo subset for the upper, left-hand corner.
		//		DisplayPosition = new VectorDbl(0, _boundedMapArea.GetInvertedYPos(0));

		//		CurrentAreaColorAndCalcSettings = newValue;

		//		// Trigger a ViewportChanged event on the PanAndZoomControl -- this should result in our UpdateViewportSizeAndPos method being called.


		//		Debug.WriteLine("========== Setting the Unscaled Extent after having set the Display Position and Zoom.");
		//		UnscaledExtent = _boundedMapArea.PosterSize;
		//	}
		//}

		public void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom)
		{
			// NOTE: SubmitJob may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId

			CheckBlockSize(newValue);

			//int? newJobNumber;

			lock (_paintLocker)
			{
				CheckViewPortSize();

				var previousValue = CurrentAreaColorAndCalcSettings;
				ReportSubmitJobDetails(previousValue, newValue, isBound: true);

				// Save the MapAreaInfo for the entire poster.
				_boundedMapArea = new BoundedMapArea(_mapJobHelper, newValue.MapAreaInfo, posterSize, ViewportSize);

				_displayZoom = displayZoom;
				var (b, r) = ContentScalerHelper.GetBaseFactorAndRelativeScale(displayZoom);

				_boundedMapArea.BaseFactor = b;
				ScaledDisplayPositionYInverted = new ValueTuple<VectorDbl, double> (_boundedMapArea.GetScaledDisplayPosition(displayPosition, out var unInvertedY), _boundedMapArea.BaseFactor);

				// Make sure no content is loaded while we reset the PanAndZoom control.
				CurrentAreaColorAndCalcSettings = null;
				
				_displayPosition = ScaledDisplayPositionYInverted.Value.Item1;
				_displayZoom = displayZoom;
				
				DisplaySettingsInitialized?.Invoke(this, new DisplaySettingsInitializedEventArgs(posterSize, _displayPosition, _displayZoom));

				CurrentAreaColorAndCalcSettings = newValue;

				// Trigger a ViewportChanged event on the PanAndZoomControl -- this should result in our UpdateViewportSizeAndPos method being called.
				UnscaledExtent = _boundedMapArea.PosterSize;
			}
		}

		/*	Details regarding what the PanAndZoomControl does as the Extent and DisplayZoom values are updated.

				The PanAndZoom control will update scroll bar extents and positions.
				The PanAndZoom control will update the BitmapGridControl's Scale Transform and Canvas Size.
				 	
			 As the UnscaledExtent is updated...
				1. The PanAndZoomControl will update it's 
					a. ContentOffsetX
					b. ContentOffsetY
					c. If the ContentScale is not 1.0, 
						i. The ContentScale is set to 1.0
							otherwise
						ii. The ContentViewportSize is updated.
					
					d. Raise the ScrollBarVisibilityChanged Event
				
			 As the ContentScale is updated, the following are updated.
				1. The BitmapGridControl's ScaleTransform
				2. The ContentViewportSize
				3. OffsetX
				4. OffsetY
				5. ZoomSlider is updated by calling it's ContentScaleWasUpdated method
			  6. The ScrollViewer is updated via calling it's InvalidateScrollInfo method
				7. The ContentScaleChanged event is raised
				8. The ViewportChanged event is raised
				 
			
			 As the ContentViewportSize is changed, the following are also updated...
				1. The BitmapGridControl's
					a. ContentViewportSize.
					b. The Size of the (main) Canvas -- using the new value for ContentViewportSize and ScaleTransform
				2. The Size of the (main) Canvas
				3. VerticalScrollBarVisibility
				4. HorizontalScrollBarVisibility.
				5. TranslationX
				6. TranslationY

		*/

		// TODO: UpdateViewportSizeAndPos may produce a JobRequest using a Subdivision different than the original Subdivision for the given JobId
		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			int? newJobNumber;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings == null)
				{
					ViewportSize = contentViewportSize;
					//_bitmapGrid.ContentViewportSize = contentViewportSize;
					newJobNumber = null;
				}
				else
				{
					if (_boundedMapArea == null)
					{
						throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
					}

					var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale);
					///var baseScale = ContentScalerHelper.GetBaseScaleFromBaseFactor(baseFactor);

					Debug.WriteLine($"The MapSectionDisplayViewModel is UpdatingViewportSizeAndPos. ViewportSize:{contentViewportSize}, Offset:{contentOffset}, Scale:{contentScale}. BaseFactor: {baseFactor}, RelativeScale: {relativeScale}.");

					//DisplayZoom = contentScale;
					newJobNumber = LoadNewView(CurrentAreaColorAndCalcSettings, _boundedMapArea, contentViewportSize, contentOffset, baseFactor);
				}
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

		public int? MoveToX(double displayPositionX)
		{
			DisplayPositionX = displayPositionX;
			var result = MoveTo(new VectorDbl(DisplayPositionX, DisplayPositionY));
			return result;
		}

		public int? MoveToYInverted(double displayPositionYInverted)
		{
			if (_boundedMapArea == null)
			{
				throw new InvalidOperationException("MoveToYInverted is only supported when there is a bounded context.");
			}

			DisplayPositionY = _boundedMapArea.GetInvertedYPos(displayPositionYInverted);

			var result = MoveTo(new VectorDbl(DisplayPositionX, DisplayPositionY));
			return result;
		}

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

				Debug.WriteLine("========== Executing MoveTo.");


				// Get the MapAreaInfo subset for the given display position
				var mapAreaInfo2Subset = _boundedMapArea.GetView(contentOffset);
				var jobType = _boundedMapArea.BaseFactor == 0 ? JobType.FullScale : JobType.ReducedScale;

				ReportMove(_boundedMapArea, contentOffset);

				newJobNumber = ReuseAndLoad(jobType, CurrentAreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

				//DisplayPosition = contentOffset;

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

				newJobNumber = ReuseAndLoad(jobType, CurrentAreaColorAndCalcSettings, LastMapAreaInfo, out var lastSectionWasIncluded);

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

		//public void ReceiveAdjustedContentScale(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl)
		//{
		//	Debug.WriteLine($"Receiving the adjusted content scale. Current: {_displayZoom}, New: {contentScaleFromPanAndZoomControl}, Check: {contentScaleFromBitmapGridControl}.");

		//	// TODO: The ContentScale properties of PanAndZoom control are in some cases a tiny fraction different -- WHY?

		//	if (ScreenTypeHelper.IsDoubleChanged(_displayZoom, contentScaleFromPanAndZoomControl, 1.001)) // 0.00001
		//	{
		//		Debug.WriteLine($"WARNING: The DisplayZoom field is not being updated via its binding.");
		//		_displayZoom = contentScaleFromPanAndZoomControl;
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"Notice: The DisplayZoom field is being updated via its binding.");
		//	}
		//}

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

		private int? LoadNewView(AreaColorAndCalcSettings areaColorAndCalcSettings, BoundedMapArea boundedMapArea, SizeDbl viewportSize, VectorDbl contentOffset, double baseFactor)
		{
			if (contentOffset.X != 0 || contentOffset.Y != 0)
			{
				Debug.WriteLine("The ContentOffset is non-zero on call to LoadNewView.");
			}

			int? newJobNumber;
			bool lastSectionWasIncluded;

			var currentBaseFactor = boundedMapArea.BaseFactor;

			var mapAreaInfo2Subset = boundedMapArea.GetView(viewportSize, contentOffset, baseFactor);
			var jobType = boundedMapArea.BaseFactor == 0 ? JobType.FullScale : JobType.ReducedScale;

			var scaledViewportSize = viewportSize.Scale(boundedMapArea.BaseScale);
			_bitmapGrid.ContentViewportSize = scaledViewportSize;

			ReportUpdateSizeAndPos(boundedMapArea, viewportSize, contentOffset);

			if (boundedMapArea.BaseFactor != currentBaseFactor)
			{
				newJobNumber = DiscardAndLoad(jobType, areaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
			}
			else
			{
				newJobNumber = ReuseAndLoad(jobType, areaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);
			}

			//newJobNumber = DiscardAndLoad(jobType, areaColorAndCalcSettings, mapAreaInfo2Subset, out lastSectionWasIncluded);


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

			_bitmapGrid.ContentViewportSize = viewportSize;

			lock (_paintLocker)
			{
				if (CurrentAreaColorAndCalcSettings != null)
				{
					var screenAreaInfo = GetScreenAreaInfo(CurrentAreaColorAndCalcSettings.MapAreaInfo, viewportSize);
					newJobNumber = ReuseAndLoad(JobType.FullScale, CurrentAreaColorAndCalcSettings, screenAreaInfo, out lastSectionWasIncluded);
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
					newJobNumber = ReuseAndLoad(JobType.FullScale, newJob, screenAreaInfo, out lastSectionWasIncluded);
				}
				else
				{
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

		private int? ReuseAndLoad(JobType jobType, AreaColorAndCalcSettings newJob, MapAreaInfo screenAreaInfo, out bool lastSectionWasIncluded)
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
				_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);
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

				_mapLoaderManager.CancelRequests(sectionsToCancel);

				_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;
				ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);
				ColorBandSet = newJob.ColorBandSet;

				var numberOfSectionsReturned = _bitmapGrid.ReDrawSections();

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
							Debug.WriteLine($"{diff} MapSections were not cleared out of a total {numberRequestedToClear} requested.");
						}
						else
						{
							Debug.WriteLine($"{numberCleared} MapSections were cleared.");
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
			StopCurrentJobs(clearDisplay: true);

			LastMapAreaInfo = screenAreaInfo;

			var sectionsRequired = _mapSectionBuilder.CreateEmptyMapSections(screenAreaInfo, newJob.MapCalcSettings);

			var newMapSections = _mapLoaderManager.Push(jobType, newJob.JobId, newJob.JobOwnerType, screenAreaInfo, newJob.MapCalcSettings, sectionsRequired, MapSectionReady,
					out var newJobNumber, out var mapSectionsPendingGeneration);

			_mapSectionsPendingGeneration.AddRange(mapSectionsPendingGeneration);

			Debug.WriteLineIf(_useDetailedDebug, $"DiscardAndLoad: {newMapSections.Count} were found in the repo, {mapSectionsPendingGeneration.Count} are being generated.");

			_bitmapGrid.MapBlockOffset = screenAreaInfo.MapBlockOffset;

			ImageOffset = new VectorDbl(screenAreaInfo.CanvasControlOffset);

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

		[Conditional("DEBUG")]
		private void ReportMove(BoundedMapArea boundedMapArea, VectorDbl contentOffset/*, double contentScale, double baseFactor*/)
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

			Debug.WriteLine($"Moving to {x}, {y}. Uninverted Y:{unInvertedY}. Poster Size: {posterSize}. ContentViewportSize: {ViewportSize}. BaseScaleFactor: {boundedMapArea.BaseScale}.");


		}

		[Conditional("DEBUG2")]
		private void ReportUpdateSizeAndPos(BoundedMapArea boundedMapArea, SizeDbl viewportSize, VectorDbl contentOffset/*, double contentScale, double baseFactor*/)
		{
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
