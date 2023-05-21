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

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
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
				_vm.UpdateViewportSize(PanAndZoomControl1.ViewportSize);

				if (_vm.ZoomSliderFactory != null)
				{
					PanAndZoomControl1.ZoomSliderOwner = _vm.ZoomSliderFactory(PanAndZoomControl1);
				}

				PanAndZoomControl1.ViewportChanged += PanAndZoomControl1_ViewportChanged;
				PanAndZoomControl1.ContentOffsetXChanged += PanAndZoomControl1_ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += PanAndZoomControl1_ContentOffsetYChanged;

				_outline = BuildOutline(BitmapGridControl1.Canvas);

				Debug.WriteLine("The MapSectionDisplay Control is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			PanAndZoomControl1.ZoomSliderOwner = null;

			PanAndZoomControl1.ViewportChanged -= PanAndZoomControl1_ViewportChanged;
			PanAndZoomControl1.ContentOffsetXChanged -= PanAndZoomControl1_ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= PanAndZoomControl1_ContentOffsetYChanged;
		}

		#endregion

		#region Event Handlers

		private void PanAndZoomControl1_ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			CheckForStaleContentOffset(e.ContentOffset);

			// TODO: Consider adding this to the IContentScaler interface
			BitmapGridControl1.ContentViewportSize = e.ContentViewportSize;

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale);
			CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);
		}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			var displayPosition = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			_ = _vm.MoveTo(displayPosition);
			CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);
		}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			//HideOutline();

			var displayPosition = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			_ = _vm.MoveTo(displayPosition);
			CenterContent(PanAndZoomControl1.UnscaledExtent, PanAndZoomControl1.ViewportSize, PanAndZoomControl1.ContentScale);
		}

		#endregion

		#region Private Methods

		private void CenterContent(SizeDbl unscaledExtent, SizeDbl viewportSize, double contentScale)
		{
			// The display area is a Vector + Size specfing the bounding box of the contents in screen coordinates,
			// relative to the Top, Left-hand corner.
			var displayArea = GetContentDispayAreaInScreenCoordinates(unscaledExtent, viewportSize, contentScale);

			if (displayArea.Point1.X > 0 || displayArea.Point1.Y > 0)
			{
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

				Debug.WriteLine($"Scaled Extent is smaller than viewportSize. ScaledExtent: {displayArea.Size} ViewportSize: {viewportSize}. DisplayOffset: {displayArea.Position}. " +
					$"Clip Position: {scaledDisplayArea.Position}. Clip Size: {scaledDisplayArea.Size}.");
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
			// from the top, right of the control to the top, right of the content
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
		private void CheckForStaleContentOffset(VectorDbl contentOffset)
		{
			var contentOffsetDirect = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);

			if (ScreenTypeHelper.IsVectorDblChanged(contentOffset, contentOffsetDirect))
			{
				Debug.WriteLine($"ContentOffset is stale on MapSectionPzControl event handler. Compare: {contentOffset} to {contentOffsetDirect}.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckScreenToRelativeScaleFactor(double screenToRelativeScaleFactor, double contentScale)
		{
			var (_, relativeScale) = ContentScalerHelper.GetBaseAndRelative(contentScale);

			var chkRelativeScale = 1 / relativeScale;
			Debug.Assert(Math.Abs(screenToRelativeScaleFactor - chkRelativeScale) < 0.1, "ScreenToRelativeScaleFactor maybe incorrect.");
		}

		#endregion
	}
}
