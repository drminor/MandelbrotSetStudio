using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionDisplayControl.xaml
	/// </summary>
	public partial class MapSectionPzControl : UserControl
	{
		#region Private Fields

		private const double POSTER_DISPLAY_MARGIN = 20;
		//private const double MAX_CONTENT_SCALE = 1;

		//private bool DRAW_OUTLINE = true;
		//private Rectangle _outline;

		private IMapDisplayViewModel _vm;

		//private bool _unscaledExtentWasZeroOnlastViewportUpdate;

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
			//_unscaledExtentWasZeroOnlastViewportUpdate = false;
			
			_vm = (IMapDisplayViewModel)DataContext;
			//_outline = new Rectangle();

			Loaded += MapSectionPzControl_Loaded;
			Unloaded += MapSectionPzControl_Unloaded;

			InitializeComponent();
		}

		private void MapSectionPzControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapSectionDisplayControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IMapDisplayViewModel)DataContext;

				//_vm.UpdateViewportSize(PanAndZoomControl1.ViewportSize);
				_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;

				_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;
				PanAndZoomControl1.ViewportChanged += ViewportChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetYChanged;

				//PanAndZoomControl1.ScrollbarVisibilityChanged += ScrollbarVisibilityChanged;
				//PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				//BitmapGridControl1.ViewportSizeChanged += BitmapGridControl1_ViewportSizeChanged;

				//_outline = BuildOutline(BitmapGridControl1.Canvas);

				//_ = BitmapGridControl1.Canvas.Children.Add(_outline);
				//_outline.SetValue(Panel.ZIndexProperty, 10);

				Debug.WriteLine("The MapSectionPzControl is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.DisplaySettingsInitialized -= _vm_DisplaySettingsInitialzed;

			PanAndZoomControl1.ViewportChanged -= ViewportChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= ContentOffsetYChanged;

			//PanAndZoomControl1.ScrollbarVisibilityChanged -= ScrollbarVisibilityChanged;
			//PanAndZoomControl1.ContentScaleChanged -= ContentScaleChanged;

			PanAndZoomControl1.Dispose();
			PanAndZoomControl1.ZoomOwner = null;
		}

		#endregion

		#region Event Handlers

		private void _vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		{
			var maxContentScale = _vm.MaximumDisplayZoom;
			var unscaledViewportSize = PanAndZoomControl1.UnscaledViewportSize;

			if (unscaledViewportSize.IsNearZero() || unscaledViewportSize.IsNAN())
			{
				unscaledViewportSize = new SizeDbl(ActualWidth, ActualHeight);
			}

			var minContentScale = RMapHelper.GetMinDisplayZoom(e.UnscaledExtent, unscaledViewportSize, POSTER_DISPLAY_MARGIN, maxContentScale);

			//minContentScale *= 0.85; // Provide some head room.

			if (minContentScale <= 0)
			{
				minContentScale = 0.0001;	
			}

			_vm.MinimumDisplayZoom = minContentScale;
			_vm.DisplayZoom = Math.Min(Math.Max(e.ContentScale, _vm.MinimumDisplayZoom), _vm.MaximumDisplayZoom);

			PanAndZoomControl1.ResetExtentWithPositionAndScale(e.UnscaledExtent, unscaledViewportSize, e.ContentOffset, _vm.DisplayZoom, minContentScale, maxContentScale);
		}

		private void ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLine("\n========== The MapSectionPzControl is handling the PanAndZoom control's ViewportChanged event.");

			//CheckForStaleContentValues(e);

			var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(e.ContentScale);
			Debug.WriteLine($"The MapSectionPzControl is UpdatingViewportSizeAndPos. ViewportSize: Scaled:{e.ContentViewportSize} / Unscaled: {e.UnscaledViewportSize}, Offset:{e.ContentOffset}, Scale:{e.ContentScale}. BaseFactor: {baseFactor}, RelativeScale: {relativeScale}.");

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize,  e.ContentOffset, e.ContentScale);

			var imagePos = BitmapGridControl1.ImagePositionYInv;

			Debug.WriteLine($"After setting the Image Offset using the Canvas.BottomProperty, the Canvas.TopProperty is {imagePos.Y}. The ImageOffset is {BitmapGridControl1.ImageOffset}");

			//var scaledDisplayArea = GetScaledDisplayArea();
			//ShowOutline(scaledDisplayArea);

			//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale);

			//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale))
			//{
			//	Debug.WriteLine("Would Center Content = true for ViewportChanged.");
			//}
		}

		private void ContentOffsetXChanged(object? sender, EventArgs e)
		{
			// NOTE: The UpdateViewportSize method is to be used by the 'regular' MapSectionDisplayControl -- that does not use Scaling.
			//var unscaledViewportSize = PanAndZoomControl1.UnscaledViewportSize;
			//_vm.UpdateViewportSize(unscaledViewportSize);

			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset, PanAndZoomControl1.ContentViewportSize);

			//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale);

			//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale))
			//{
			//	Debug.WriteLine("Would Center Content = true for MoveTo - X");
			//}
		}

		private void ContentOffsetYChanged(object? sender, EventArgs e)
		{
			//HideOutline();

			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset, PanAndZoomControl1.ContentViewportSize);

			//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale);

			//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale))
			//{
			//	Debug.WriteLine("Would Center Content = true for MoveTo - Y");
			//}
		}

		//private void ScrollbarVisibilityChanged(object? sender, EventArgs e)
		//{
		//	//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale);

		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for ScrollbarVisibilityChanged.");
		//	//}
		//}

		//private void ContentScaleChanged(object? sender, EventArgs e)
		//{
		//	CheckForOutofSyncScaleFactor(PanAndZoomControl1.ContentScale, BitmapGridControl1.ContentScale.Width);

		//	var scaledDisplayArea = GetScaledDisplayArea();
		//	ShowOutline(scaledDisplayArea);

		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for ContentScaleChanged.");
		//	//}

		//	//_vm.ReceiveAdjustedContentScale(PanAndZoomControl1.ContentScale, BitmapGridControl1.ContentScale.Width);
		//	//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale);
		//}

		//private void BitmapGridControl1_ViewportSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	////var contentScale = PanAndZoomControl1.ContentScale;
		//	////var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale);

		//	////_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;
		//	////_vm.LogicalViewportSize = _vm.ViewportSize.Scale(1 / relativeScale);

		//	//var canvas = BitmapGridControl1.Canvas;

		//	//canvas.Children.Remove(_outline);

		//	//_outline = BuildOutline(canvas);

		//	//_ = canvas.Children.Add(_outline);
		//	//_outline.SetValue(Panel.ZIndexProperty, 10);
		//}

		#endregion

		#region Private Methods

		//private bool WouldCenterContent(SizeDbl unscaledExtent, SizeDbl unscaledViewportSize, double contentScale)
		//{
		//	// The display area is a Vector + Size specfing the bounding box of the contents in screen coordinates,
		//	// relative to the Top, Left-hand corner.
		//	var displayArea = GetContentDispayAreaInScreenCoordinates(unscaledExtent, unscaledViewportSize, contentScale);

		//	var isUnscaledExtentIsNearZero = unscaledExtent.IsNearZero();
		//	var displayAreaSizeIsNearZero = displayArea.Size.IsNearZero();
		//	var displayAreaPositionIsPositive = displayArea.Point1.X > 0 || displayArea.Point1.Y > 0;

		//	var result = !isUnscaledExtentIsNearZero && !displayAreaSizeIsNearZero && displayAreaPositionIsPositive;

		//	return result;
		//}

		////TODO: Consider moving this to the MapSectionDisplayControl and/or using the PanAndZoomControl's _contentScaler.TranslateTransform
		//// TODO: Consider creating Dependency Properties on the BitmapGridControl so that it can
		//// bind to the PanAndZoomControl's OffsetX and OffsetY properties.

		//private void CenterContent(SizeDbl unscaledExtent, SizeDbl unscaledViewportSize, double contentScale)
		//{
		//	if (unscaledExtent.IsNearZero())
		//	{
		//		return;
		//	}

		//	// The display area is a Vector + Size specfing the bounding box of the contents in screen coordinates,
		//	// relative to the Top, Left-hand corner.
		//	var displayArea = GetContentDispayAreaInScreenCoordinates(unscaledExtent, unscaledViewportSize, contentScale);

		//	var displayAreaPositionIsPositive = displayArea.Point1.X > 0 || displayArea.Point1.Y > 0;

		//	if (!displayArea.Size.IsNearZero() && displayAreaPositionIsPositive)
		//	{
		//		//Debug.WriteLine($"The MapSectionPzControl is centering the content. DisplayArea.Point1: {displayArea.Point1}.");

		//		// The content does not fill the entire control. Move the content to the center of the control
		//		// and clip the content to hide any of the BitmapSections that may be 'off canvas'

		//		BitmapGridControl1.ScaledContentArea = displayArea;

		//		Debug.WriteLine($"The MapSectionPzControl is centering the content. Scaled Extent is smaller than viewportSize. ContentPresenterOffset: {displayArea.Position}. ScaledExtent: {displayArea.Size} UnscaledViewportSize: {unscaledViewportSize}. ContentScale: {contentScale}.");
		//	}
		//	else
		//	{
		//		//OffsetAndClip(null);
		//		BitmapGridControl1.ScaledContentArea = null;
		//		Debug.WriteLine($"The MapSectionPzControl is centering the content, Scaled Extent is NOT smaller than viewportSize. ContentPresenterOffset: {displayArea.Position}. ScaledExtent: {displayArea.Size} ViewportSize: {unscaledViewportSize}. ContentScale: {contentScale}.");
		//	}
		//}

		//private RectangleDbl GetContentDispayAreaInScreenCoordinates(SizeDbl unscaledExtent, SizeDbl unscaledViewportSize, double contentScale)
		//{
		//	// Use the unscaledExtent, the actual size of the content, to get the get the number of pixels
		//	// that would be required to view the entire content (aka ScaledExtent) at the current scale.
		//	var scaledExtent = unscaledExtent.Scale(contentScale);


		//	// Calculate the distance from the top, left of the container to the top, left of the content

		//	var x = Math.Max(0, (unscaledViewportSize.Width - scaledExtent.Width) / 2);
		//	var y = Math.Max(0, (unscaledViewportSize.Height - scaledExtent.Height) / 2);

		//	var unscaledOffset = new PointDbl(x, y);

		//	// Build rectangle for the position and size on screen.
		//	// Since we apply the translation before any scaling takes place,
		//	// we use the unscaledOffset for the position.

		//	// Note: If the contentScale is low enough, the scaledExtent will be smaller than the unscaledViewportSize.

		//	var result = new RectangleDbl(unscaledOffset, scaledExtent);

		//	return result;
		//}

		//private void OffsetAndClip(RectangleDbl? scaledDisplayArea)
		//{
		//	if (scaledDisplayArea == null)
		//	{
		//		//Debug.WriteLine("Pz is clearing the Cpo and CC.");
		//		//BitmapGridControl1.ContentPresenterOffset = VectorDbl.Zero;
		//		//BitmapGridControl1.CanvasClip = null;
		//		BitmapGridControl1.ClearValue();

		//	}
		//	else
		//	{
		//		// Center the Canvas, using Canvas coordinates
		//		var offset = new VectorDbl(scaledDisplayArea.Value.Position);
		//		BitmapGridControl1.ContentPresenterOffset = offset;


		//		// Only show the pixels belonging to the Poster.
		//		var scaledDisplaySize = ScreenTypeHelper.ConvertToSize(scaledDisplayArea.Value.Size);
		//		var clipRegion = new Rect(scaledDisplaySize);
		//		BitmapGridControl1.CanvasClip = new RectangleGeometry(clipRegion);

		//		Debug.WriteLine($"Pz is setting the Cpo to {offset}, CC to {clipRegion}.");

		//	}
		//}

		//private bool IsUnscaledExtentBeingInitialized(VectorDbl contentOffset, out bool contentOffsetIsNonZeroUponInitialization)
		//{
		//	contentOffsetIsNonZeroUponInitialization = false;
		//	bool result;

		//	var unscaledExtentIsZero = PanAndZoomControl1.UnscaledExtent.IsNearZero();

		//	if (!unscaledExtentIsZero)
		//	{
		//		if (_unscaledExtentWasZeroOnlastViewportUpdate)
		//		{
		//			// UnscaledExtent was zero, but now has a non-zero value, We are being initialized. 
		//			result = true;


		//			contentOffsetIsNonZeroUponInitialization = contentOffset.X != 0 || contentOffset.Y != 0;
		//		}
		//		else
		//		{
		//			result = false;
		//		}
		//	}
		//	else
		//	{
		//		result = false;
		//	}

		//	if (contentOffsetIsNonZeroUponInitialization)
		//	{
		//		// The last known display position is being restored from the repo
		//		Debug.WriteLine("The last know display position is being restored.");
		//		}

		//	// Update the state for the next time.
		//	_unscaledExtentWasZeroOnlastViewportUpdate = unscaledExtentIsZero;

		//	return result;
		//}

		#endregion

		#region Show Outline Support

		/*
		
		private RectangleDbl GetScaledDisplayArea()
		{
			var contentScale = PanAndZoomControl1.ContentScale;

			//var baseScale = ContentScalerHelper.GetBaseScale(contentScale);
			var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale);
			var baseScale = ContentScalerHelper.GetBaseScaleFromBaseFactor(baseFactor);

			var contentOffset = new PointDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			var contentViewportSize = PanAndZoomControl1.ContentViewportSize;

			var adjustedSize = contentViewportSize.Scale(baseScale);
			var result = new RectangleDbl(contentOffset, adjustedSize);

			var unscaledViewportSize = PanAndZoomControl1.UnscaledViewportSize;
			var chkAdjustedSize = unscaledViewportSize.Divide(relativeScale);

			if (ScreenTypeHelper.IsSizeDblChanged(chkAdjustedSize, adjustedSize))
			{
				Debug.WriteLine($"Adjusted ViewportSizes do not match.");
			}

			//var cSize1 = _vm.ViewportSize;

			//var cSizeMatches = !ScreenTypeHelper.IsSizeDblChanged(cSize1, unscaledViewportSize);
			//Debug.Assert(cSizeMatches, "VM.ViewportSize ne UnscaledViewportSize.");

			//var lSize = _vm.LogicalViewportSize;
			//var lSizeMatches = !ScreenTypeHelper.IsSizeDblChanged(lSize, adjustedSize);
			//Debug.Assert(lSizeMatches, "VM.LogicalViewportSize ne scaled canvasSize.");


			//var result = new RectangleDbl(contentOffset, contentViewportSize);

			return result;
		}

		private void ShowOutline(RectangleDbl scaledDisplayArea)
		{
			if (DRAW_OUTLINE)
			{
				// Position the outline rectangle.

				if (scaledDisplayArea.Width > 10 && scaledDisplayArea.Height > 10)
				{
					var pos = scaledDisplayArea.Position;

					_outline.SetValue(Canvas.LeftProperty, 2d);
					_outline.SetValue(Canvas.BottomProperty, 2d);

					_outline.Width = scaledDisplayArea.Width - 4;
					_outline.Height = scaledDisplayArea.Height - 4;
					_outline.Visibility = Visibility.Visible;
				}
			}
		}

		private void HideOutline()
		{
			if (DRAW_OUTLINE)
			{
				_outline.Visibility = Visibility.Hidden;
			}
		}

		private Rectangle BuildOutline(Canvas canvas)
		{
			var result = new Rectangle()
			{
				Width = 1,
				Height = 1,
				Fill = Brushes.Transparent, // new SolidColorBrush(Colors.LightBlue),
				Stroke = new SolidColorBrush(Colors.DarkSeaGreen), // BuildDrawingBrush(), // 
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = false,
				Opacity = 0.85,
			};

			return result;
		}

		private DrawingBrush BuildDrawingBrush()
		{
			var db = new DrawingBrush();
			db.Viewport = new Rect(0, 0, 20, 20);
			db.ViewboxUnits = BrushMappingMode.Absolute;
			db.TileMode = TileMode.Tile;

			//db.Drawing = new DrawingGroup();

			var geometryDrawing = new GeometryDrawing();
			geometryDrawing.Brush = new SolidColorBrush(Colors.Green);

			var geometryGroup = new GeometryGroup();
			geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 50, 50)));
			geometryGroup.Children.Add(new RectangleGeometry(new Rect(50, 50, 50, 50)));

			geometryDrawing.Geometry = geometryGroup;

			db.Drawing = geometryDrawing;

			return db;


			//	< DrawingBrush Viewport = "0,0,20,20" ViewportUnits = "Absolute" TileMode = "Tile" >

			//	< DrawingBrush.Drawing >

			//		< DrawingGroup >

			//			< GeometryDrawing Brush = "Black" >

			//				< GeometryDrawing.Geometry >

			//					< GeometryGroup >

			//						< RectangleGeometry Rect = "0,0,50,50" />

			//						< RectangleGeometry Rect = "50,50,50,50" />

			//					</ GeometryGroup >

			//				</ GeometryDrawing.Geometry >

			//			</ GeometryDrawing >

			//		</ DrawingGroup >

			//	</ DrawingBrush.Drawing >

			//</ DrawingBrush >
		}

		*/

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void CheckForOutofSyncScaleFactor(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl)
		{
			// TODO: As we are using a BaseScale, it might be the case where these are supposed to be different

			//Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF), "The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control.");
			if (ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF))
			{
				Debug.WriteLine($"The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control. " +
					$"PanAndZoomControl's ContentScale: {contentScaleFromPanAndZoomControl}, BitmapGridControl's ContentScale: {contentScaleFromBitmapGridControl}.");
			}
		}

		#endregion
	}
}
