using MSetExplorer.MapDisplay.Support;
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

		private bool DRAW_OUTLINE = false;
		private Rectangle _outline;

		private IMapDisplayViewModel _vm;

		//private bool _unscaledExtentWasZeroOnlastViewportUpdate;

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
			//_unscaledExtentWasZeroOnlastViewportUpdate = false;
			
			_vm = (IMapDisplayViewModel)DataContext;
			_outline = new Rectangle();

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
				_vm.ViewportSize = PanAndZoomControl1.ViewportSize;

				if (_vm.ZoomSliderFactory != null)
				{
					PanAndZoomControl1.ZoomSliderOwner = _vm.ZoomSliderFactory(PanAndZoomControl1);
				}

				//PanAndZoomControl1.ViewportChanged += PanAndZoomControl1_ViewportChanged;
				PanAndZoomControl1.ContentOffsetXChanged += PanAndZoomControl1_ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += PanAndZoomControl1_ContentOffsetYChanged;
				PanAndZoomControl1.ScrollbarVisibilityChanged += PanAndZoomControl1_ScrollbarVisibilityChanged;

				_vm.InitializeDisplaySettings += MapSectionDisplayViewModel_InitializeDisplaySettings;

				PanAndZoomControl1.ContentScaleChanged += PanAndZoomControl1_ContentScaleChanged;


				_outline = BuildOutline(BitmapGridControl1.Canvas);

				Debug.WriteLine("The MapSectionPzControl is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			PanAndZoomControl1.ZoomSliderOwner = null;

			//PanAndZoomControl1.ViewportChanged -= PanAndZoomControl1_ViewportChanged;
			PanAndZoomControl1.ContentOffsetXChanged -= PanAndZoomControl1_ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= PanAndZoomControl1_ContentOffsetYChanged;
			PanAndZoomControl1.ScrollbarVisibilityChanged -= PanAndZoomControl1_ScrollbarVisibilityChanged;

			_vm.InitializeDisplaySettings -= MapSectionDisplayViewModel_InitializeDisplaySettings;

			PanAndZoomControl1.ContentScaleChanged -= PanAndZoomControl1_ContentScaleChanged;
		}

		#endregion

		#region Event Handlers

		//private void PanAndZoomControl1_ViewportChanged(object? sender, ScaledImageViewInfo e)
		//{
		//	CheckForStaleContentValues(e);

		//	//if (_unscaledExtentWasZeroOnlastViewportUpdate)
		//	//{
		//	//	_unscaledExtentWasZeroOnlastViewportUpdate = false;
		//	//	if (e.ContentOffset.X != 0 || e.ContentOffset.Y != 0)
		//	//	{
		//	//		Debug.WriteLine("The ContentOffset is non-zero on first call to UpdateViewportSizeAndPos after the UnscaledExtent was reset.");
		//	//	}
		//	//}

		//	_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale);
		//	CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);

		//	//_unscaledExtentWasZeroOnlastViewportUpdate = PanAndZoomControl1.UnscaledExtent.IsNearZero();
		//}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			var contentOffset = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);

			//if (_unscaledExtentWasZeroOnlastViewportUpdate)
			//{
			//	_unscaledExtentWasZeroOnlastViewportUpdate = false;
			//	if (contentOffset.X != 0 || contentOffset.Y != 0)
			//	{
			//		Debug.WriteLine("The ContentOffset is non-zero on first call to MoveTo after the UnscaledExtent was reset.");
			//	}
			//}

			_ = _vm.MoveTo(contentOffset);
			CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);

			//_unscaledExtentWasZeroOnlastViewportUpdate = PanAndZoomControl1.UnscaledExtent.IsNearZero();
		}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			//HideOutline();

			var contentOffset = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);

			//if (_unscaledExtentWasZeroOnlastViewportUpdate)
			//{
			//	_unscaledExtentWasZeroOnlastViewportUpdate = false;
			//	if (contentOffset.X != 0 || contentOffset.Y != 0)
			//	{
			//		Debug.WriteLine("The ContentOffset is non-zero on first call to MoveTo after the UnscaledExtent was reset.");
			//	}
			//}

			_ = _vm.MoveTo(contentOffset);
			CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);

			//_unscaledExtentWasZeroOnlastViewportUpdate = PanAndZoomControl1.UnscaledExtent.IsNearZero();
		}

		private void PanAndZoomControl1_ScrollbarVisibilityChanged(object? sender, EventArgs e)
		{
			CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);

			//_unscaledExtentWasZeroOnlastViewportUpdate = PanAndZoomControl1.UnscaledExtent.IsNearZero();
		}

		private void MapSectionDisplayViewModel_InitializeDisplaySettings(object? sender, InitialDisplaySettingsEventArgs e)
		{
			PanAndZoomControl1.ResetExtentWithPositionAndScale(e.ContentOffset, e.MinContentScale, e.MaxContentScale, e.ContentScale);
		}

		private void PanAndZoomControl1_ContentScaleChanged(object? sender, EventArgs e)
		{
			var contentScaleFromPanAndZoomControl = PanAndZoomControl1.ContentScale;
			var contentScaleFromBitmapGridControl = BitmapGridControl1.ContentScale;

			CheckForOutofSyncScaleFactor(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl);

			_vm.ReceiveAdjustedContentScale(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl);
		}

		#endregion

		#region Private Methods

		// TODO: Consider moving this to the MapSectionDisplayControl and/or using the PanAndZoomControl's _contentScaler.TranslateTransform
		// TODO: Consider creating Dependency Properties on the BitmapGridControl so that it can 
		// bind to the PanAndZoomControl's OffsetX and OffsetY properties.
		private void CenterContent(SizeDbl unscaledExtent, SizeDbl viewportSize, double contentScale)
		{
			// The display area is a Vector + Size specfing the bounding box of the contents in screen coordinates,
			// relative to the Top, Left-hand corner.
			var displayArea = GetContentDispayAreaInScreenCoordinates(unscaledExtent, viewportSize, contentScale);

			var isUnscaledExtentIsNearZero = unscaledExtent.IsNearZero();
			var displayAreaSizeIsNearZero = displayArea.Size.IsNearZero();
			var displayAreaPositionIsPositive = displayArea.Point1.X > 0 || displayArea.Point1.Y > 0;

			if ( !isUnscaledExtentIsNearZero && !displayAreaSizeIsNearZero && displayAreaPositionIsPositive)
			{
				Debug.WriteLine($"The MapSectionPzControl is centering the content. DisplayArea.Point1: {displayArea.Point1}.");

				// The content does not fill the entire control.
				// Move the content to the center of the control
				// and clip so that only the content is visible.

				// The screen is scaled by relativeScale.
				// Convert screen coordinates to 'display' coordinates
				var scaleFactor = ContentScalerHelper.GetScaleFactor(contentScale);
				var screenToRelativeScaleFactor = scaleFactor / contentScale;

				CheckScreenToRelativeScaleFactor(screenToRelativeScaleFactor, contentScale);

				var scaledDisplayArea = displayArea.Scale(screenToRelativeScaleFactor);

				ShowOutline(scaledDisplayArea);

				OffsetAndClip(scaledDisplayArea);

				//Debug.WriteLine($"Scaled Extent is smaller than viewportSize. ScaledExtent: {displayArea.Size} ViewportSize: {viewportSize}. DisplayOffset: {displayArea.Position}. " +
				//	$"Clip Position: {scaledDisplayArea.Position}. Clip Size: {scaledDisplayArea.Size}.");
			}
			else
			{
				OffsetAndClip(null);

				//Debug.WriteLine($"Scaled Extent is NOT smaller than viewportSize. ScaledExtent: {scaledExtent} ViewportSize: {viewportSize}. DisplayOffset: {displayOffset}.");
			}
		}

		private RectangleDbl GetContentDispayAreaInScreenCoordinates(SizeDbl unscaledExtent, SizeDbl viewportSize, double contentScale)
		{
			// Get the number of pixels in unscaled coordinates
			// from the top, left of the control to the top, left of the content
			var scaledExtent = unscaledExtent.Scale(contentScale);

			var x = Math.Max(0, (viewportSize.Width - scaledExtent.Width) / 2);
			var y = Math.Max(0, (viewportSize.Height - scaledExtent.Height) / 2);

			var displayOffset = new PointDbl(x, y);

			// Build rectangle for the position and size on screen
			var result = new RectangleDbl(displayOffset, scaledExtent);

			return result;
		}

		private void OffsetAndClip(RectangleDbl? scaledDisplayArea)
		{
			if (scaledDisplayArea == null)
			{
				BitmapGridControl1.ContentOffset = VectorDbl.Zero;
				BitmapGridControl1.CanvasClip = null;
			}
			else
			{
				// Center the Canvas, using Canvas coordinates
				var offset = new VectorDbl(scaledDisplayArea.Value.Position);
				BitmapGridControl1.ContentOffset = offset;


				// Only show the pixels belonging to the Poster.
				var scaledDisplaySize = ScreenTypeHelper.ConvertToSize(scaledDisplayArea.Value.Size);
				BitmapGridControl1.CanvasClip = new RectangleGeometry(new Rect(scaledDisplaySize));
			}
		}

		private void ShowOutline(RectangleDbl scaledDisplayArea)
		{
			if (DRAW_OUTLINE)
			{
				// Position the outline rectangle.
				_outline.SetValue(Canvas.LeftProperty, scaledDisplayArea.X1);
				_outline.SetValue(Canvas.BottomProperty, scaledDisplayArea.Y1);

				_outline.Width = scaledDisplayArea.Width;
				_outline.Height = scaledDisplayArea.Height;
				_outline.Visibility = Visibility.Visible;
			}
		}

		//private void HideOutline()
		//{
		//	if (DRAW_OUTLINE)
		//	{
		//		_outline.Visibility = Visibility.Hidden;
		//	}
		//}

		private Rectangle BuildOutline(Canvas canvas)
		{
			var result = new Rectangle()
			{
				Width = 1,
				Height = 1,
				Fill = Brushes.Transparent,
				Stroke = BuildDrawingBrush(), // new SolidColorBrush(Colors.DarkSeaGreen), // 
				StrokeThickness = 2,
				Visibility = Visibility.Hidden,
				Focusable = false
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Panel.ZIndexProperty, 10);

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


			//			< DrawingBrush Viewport = "0,0,20,20" ViewportUnits = "Absolute" TileMode = "Tile" >

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

		[Conditional("DEBUG")]
		private void CheckForStaleContentValues(ScaledImageViewInfo scaledImageViewInfo/*VectorDbl contentOffset*/)
		{
			var contentOffsetDirect = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);

			//if (ScreenTypeHelper.IsVectorDblChanged(scaledImageViewInfo.ContentOffset, contentOffsetDirect))
			//{
			//	Debug.WriteLine($"ContentOffset is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentOffset} to {contentOffsetDirect}.");
			//}

			Debug.Assert(!ScreenTypeHelper.IsVectorDblChanged(scaledImageViewInfo.ContentOffset, contentOffsetDirect),
				$"ContentOffset is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentOffset} to {contentOffsetDirect}.");

			var contentViewportSizeDirect = BitmapGridControl1.ContentViewportSize;

			//if (ScreenTypeHelper.IsSizeDblChanged(scaledImageViewInfo.ContentViewportSize, contentViewportSizeDirect))
			//{
			//	Debug.WriteLine($"ContentViewportSize is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentViewportSize} to {contentViewportSizeDirect}.");
			//}

			Debug.Assert(!ScreenTypeHelper.IsSizeDblChanged(scaledImageViewInfo.ContentViewportSize, contentViewportSizeDirect),
				$"ContentViewportSize is stale on MapSectionPzControl event handler. Compare: {scaledImageViewInfo.ContentViewportSize} to {contentViewportSizeDirect}.");
		}

		[Conditional("DEBUG")]
		private void CheckScreenToRelativeScaleFactor(double screenToRelativeScaleFactor, double contentScale)
		{
			var (_, relativeScale) = ContentScalerHelper.GetBaseAndRelative(contentScale);

			var chkRelativeScale = 1 / relativeScale;
			Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(screenToRelativeScaleFactor, chkRelativeScale, 0.1), "ScreenToRelativeScaleFactor maybe incorrect.");
		}

		[Conditional("DEBUG")]
		private void CheckForOutofSyncScaleFactor(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl)
		{
			Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF), "The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control.");
		}

		#endregion
	}
}
