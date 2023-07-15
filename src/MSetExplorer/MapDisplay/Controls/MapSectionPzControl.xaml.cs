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

		//private const double POSTER_DISPLAY_MARGIN = 20;
		//private const double MAX_CONTENT_SCALE = 1;

		//private bool DRAW_OUTLINE = false;
		//private Rectangle _outline;

		/// <summary>
		/// private IMapDisplayViewModel _vm;
		/// </summary>

		//private bool _unscaledExtentWasZeroOnlastViewportUpdate;

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
			//_unscaledExtentWasZeroOnlastViewportUpdate = false;
			
			//_vm = (IMapDisplayViewModel)DataContext;
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
				//_vm = (IMapDisplayViewModel)DataContext;

				//_vm.UpdateViewportSize(PanAndZoomControl1.ViewportSize);
				//_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;

				//PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetXChanged;
				//PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetYChanged;

				//_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;

				//PanAndZoomControl1.ViewportChanged += ViewportChanged;
				//PanAndZoomControl1.ScrollbarVisibilityChanged += ScrollbarVisibilityChanged;
				//PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				//_outline = BuildOutline(BitmapGridControl1.Canvas);

				Debug.WriteLine("The MapSectionPzControl is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			//_vm.DisplaySettingsInitialized -= _vm_DisplaySettingsInitialzed;

			//PanAndZoomControl1.ViewportChanged -= ViewportChanged;

			//PanAndZoomControl1.ContentOffsetXChanged -= ContentOffsetXChanged;
			//PanAndZoomControl1.ContentOffsetYChanged -= ContentOffsetYChanged;

			//PanAndZoomControl1.ScrollbarVisibilityChanged -= ScrollbarVisibilityChanged;
			//PanAndZoomControl1.ContentScaleChanged -= ContentScaleChanged;

			PanAndZoomControl1.Dispose();
			PanAndZoomControl1.ZoomOwner = null;
		}

		#endregion

		#region Event Handlers

		//private void _vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		//{
		//	var unscaledViewportSize = new SizeDbl(1024);

		//	var minContentScale = RMapHelper.GetMinDisplayZoom(e.UnscaledExtent, unscaledViewportSize, POSTER_DISPLAY_MARGIN);

		//	PanAndZoomControl1.ResetExtentWithPositionAndScale(e.ContentOffset, e.ContentScale, minContentScale, MAX_CONTENT_SCALE);
		//}


		//private void ContentOffsetXChanged(object? sender, EventArgs e)
		//{
		//	var contentOffset = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
		//	_ = _vm.MoveTo(contentOffset);

		//	//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale);

		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for MoveTo - X");
		//	//}
		//}

		//private void ContentOffsetYChanged(object? sender, EventArgs e)
		//{
		//	//HideOutline();

		//	var contentOffset = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
		//	_ = _vm.MoveTo(contentOffset);

		//	//CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale);

		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for MoveTo - Y");
		//	//}
		//}


		//private void ViewportChanged(object? sender, ScaledImageViewInfo e)
		//{
		//	CheckForStaleContentValues(e);
		//	//_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale);
		//	CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale);

		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for ViewportChanged.");
		//	//}
		//}

		//private void ScrollbarVisibilityChanged(object? sender, EventArgs e)
		//{
		//	CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale);
			
		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for ScrollbarVisibilityChanged.");
		//	//}
		//}

		//private void ContentScaleChanged(object? sender, EventArgs e)
		//{
		//	CheckForOutofSyncScaleFactor(PanAndZoomControl1.ContentScale, BitmapGridControl1.ContentScale.Width);

		//	//if (WouldCenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnScaledViewportSize, PanAndZoomControl1.ContentScale))
		//	//{
		//	//	Debug.WriteLine("Would Center Content = true for ContentScaleChanged.");
		//	//}

		//	_vm.ReceiveAdjustedContentScale(PanAndZoomControl1.ContentScale, BitmapGridControl1.ContentScale.Width);
		//	CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.UnscaledViewportSize, PanAndZoomControl1.ContentScale);
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

		// TODO: Consider moving this to the MapSectionDisplayControl and/or using the PanAndZoomControl's _contentScaler.TranslateTransform
		// TODO: Consider creating Dependency Properties on the BitmapGridControl so that it can 
		// bind to the PanAndZoomControl's OffsetX and OffsetY properties.

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

		//	if ( !displayArea.Size.IsNearZero() && displayAreaPositionIsPositive)
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

		//private void ShowOutline(RectangleDbl scaledDisplayArea)
		//{
		//	if (DRAW_OUTLINE)
		//	{
		//		// Position the outline rectangle.
		//		_outline.SetValue(Canvas.LeftProperty, scaledDisplayArea.X1);
		//		_outline.SetValue(Canvas.BottomProperty, scaledDisplayArea.Y1);

		//		_outline.Width = scaledDisplayArea.Width;
		//		_outline.Height = scaledDisplayArea.Height;
		//		_outline.Visibility = Visibility.Visible;
		//	}
		//}

		//private void HideOutline()
		//{
		//	if (DRAW_OUTLINE)
		//	{
		//		_outline.Visibility = Visibility.Hidden;
		//	}
		//}

		//private Rectangle BuildOutline(Canvas canvas)
		//{
		//	var result = new Rectangle()
		//	{
		//		Width = 1,
		//		Height = 1,
		//		Fill = Brushes.Transparent,
		//		Stroke = BuildDrawingBrush(), // new SolidColorBrush(Colors.DarkSeaGreen), // 
		//		StrokeThickness = 2,
		//		Visibility = Visibility.Hidden,
		//		Focusable = false
		//	};

		//	_ = canvas.Children.Add(result);
		//	result.SetValue(Panel.ZIndexProperty, 10);

		//	return result;
		//}

		//private DrawingBrush BuildDrawingBrush()
		//{
		//	var db = new DrawingBrush();
		//	db.Viewport = new Rect(0, 0, 20, 20);
		//	db.ViewboxUnits = BrushMappingMode.Absolute;
		//	db.TileMode = TileMode.Tile;

		//	//db.Drawing = new DrawingGroup();

		//	var geometryDrawing = new GeometryDrawing();
		//	geometryDrawing.Brush = new SolidColorBrush(Colors.Green);

		//	var geometryGroup = new GeometryGroup();
		//	geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 50, 50)));
		//	geometryGroup.Children.Add(new RectangleGeometry(new Rect(50, 50, 50, 50)));

		//	geometryDrawing.Geometry = geometryGroup;

		//	db.Drawing = geometryDrawing;

		//	return db;


		//	//	< DrawingBrush Viewport = "0,0,20,20" ViewportUnits = "Absolute" TileMode = "Tile" >

		//	//	< DrawingBrush.Drawing >

		//	//		< DrawingGroup >

		//	//			< GeometryDrawing Brush = "Black" >

		//	//				< GeometryDrawing.Geometry >

		//	//					< GeometryGroup >

		//	//						< RectangleGeometry Rect = "0,0,50,50" />

		//	//						< RectangleGeometry Rect = "50,50,50,50" />

		//	//					</ GeometryGroup >

		//	//				</ GeometryDrawing.Geometry >

		//	//			</ GeometryDrawing >

		//	//		</ DrawingGroup >

		//	//	</ DrawingBrush.Drawing >

		//	//</ DrawingBrush >
		//}

		//[Conditional("DEBUG")]
		//private void CheckForStaleContentValues(ScaledImageViewInfo scaledImageViewInfo/*VectorDbl contentOffset*/)
		//{
		//	var contentOffsetDirect = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);

		//	//if (ScreenTypeHelper.IsVectorDblChanged(scaledImageViewInfo.ContentOffset, contentOffsetDirect))
		//	//{
		//	//	Debug.WriteLine($"ContentOffset is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentOffset} to {contentOffsetDirect}.");
		//	//}

		//	Debug.Assert(!ScreenTypeHelper.IsVectorDblChanged(scaledImageViewInfo.ContentOffset, contentOffsetDirect),
		//		$"ContentOffset is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentOffset} to {contentOffsetDirect}.");

		//	var contentViewportSizeDirect = BitmapGridControl1.ContentViewportSize;

		//	//if (ScreenTypeHelper.IsSizeDblChanged(scaledImageViewInfo.ContentViewportSize, contentViewportSizeDirect))
		//	//{
		//	//	Debug.WriteLine($"ContentViewportSize is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentViewportSize} to {contentViewportSizeDirect}.");
		//	//}

		//	Debug.Assert(!ScreenTypeHelper.IsSizeDblChanged(scaledImageViewInfo.ContentViewportSize, contentViewportSizeDirect),
		//		$"ContentViewportSize is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentViewportSize} to {contentViewportSizeDirect}.");
		//}

		//[Conditional("DEBUG")]
		//private void CheckForOutofSyncScaleFactor(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl)
		//{
		//	// TODO: As we are using a BaseScale, it might be the case where these are supposed to be different

		//	//Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF), "The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control.");
		//	if (ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF))
		//	{
		//		Debug.WriteLine($"The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control. " +
		//			$"PanAndZoomControl's ContentScale: {contentScaleFromPanAndZoomControl}, BitmapGridControl's ContentScale: {contentScaleFromBitmapGridControl}.");
		//	}
		//}

		#endregion
	}
}
