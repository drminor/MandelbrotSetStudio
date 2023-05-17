using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.Media.Ocr;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionDisplayControl.xaml
	/// </summary>
	public partial class MapSectionPzControl : UserControl
	{
		#region Private Fields

		private const bool DRAW_OUTLINE = true;
		private Rectangle _outline;


		private IMapDisplayViewModel _vm;

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
			_vm = (IMapDisplayViewModel)DataContext;

			Loaded += MapSectionPzControl_Loaded;
			Unloaded += MapSectionPzControl_Unloaded;

			InitializeComponent();

			_outline = BuildOutline(BitmapGridControl1.Canvas);
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

			//BitmapGridControl1.ViewportSizeChanged -= BitmapGridControl1_ViewportSizeChanged;

			PanAndZoomControl1.ViewportChanged -= PanAndZoomControl1_ViewportChanged;
			PanAndZoomControl1.ContentOffsetXChanged -= PanAndZoomControl1_ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= PanAndZoomControl1_ContentOffsetYChanged;
		}

		#endregion

		#region Event Handlers

		private void PanAndZoomControl1_ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			HideOutline();

			CheckForStaleContentOffset(e.ContentOffset);

			// TODO: Consider adding this to the IContentScaler interface
			BitmapGridControl1.ContentViewportSize = e.ContentViewportSize;

			var baseScale = PanAndZoomControl1.ZoomSliderOwner?.BaseValue ?? 1.0;

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale, baseScale);

			//var scaledExtent = _vm.UnscaledExtent.Scale(e.ContentScale);
			//var outline = new RectangleDbl(new PointDbl(), scaledExtent);
			//ShowOutline(scaledExtent, PanAndZoomControl1.ViewportSize);

			ShowOutline(_vm.ScaledExtent, PanAndZoomControl1.ViewportSize);
		}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			HideOutline();

			var displayPosition = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			_ = _vm.MoveTo(displayPosition);

			//var scaledExtent = _vm.UnscaledExtent.Scale(PanAndZoomControl1.ContentScale);
			//var outline = new RectangleDbl(new PointDbl(), scaledExtent);
			//ShowOutline(scaledExtent, PanAndZoomControl1.ViewportSize);

			ShowOutline(_vm.ScaledExtent, PanAndZoomControl1.ViewportSize);
		}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			HideOutline();

			var displayPosition = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			_ = _vm.MoveTo(displayPosition);

			//var scaledExtent = _vm.UnscaledExtent.Scale(PanAndZoomControl1.ContentScale);
			//var outline = new RectangleDbl(new PointDbl(), scaledExtent);
			//ShowOutline(scaledExtent, PanAndZoomControl1.ViewportSize);

			ShowOutline(_vm.ScaledExtent, PanAndZoomControl1.ViewportSize);
		}

		#endregion

		#region Private Methods

		private void ShowOutline(SizeDbl scaledExtent, SizeDbl viewportSize)
		{
			if (DRAW_OUTLINE)
			{
				var x = Math.Max(0, (viewportSize.Width - scaledExtent.Width) / 2);
				var y = Math.Max(0, (viewportSize.Height - scaledExtent.Height) / 2);

				var invertedY = viewportSize.Height - y;


				// Position the outline rectangle.
				_outline.SetValue(Canvas.LeftProperty, x);
				_outline.SetValue(Canvas.BottomProperty, invertedY);
				//_outline.SetValue(Canvas.TopProperty, y);

				_outline.Width = scaledExtent.Width;
				_outline.Height = scaledExtent.Height;
				_outline.Visibility = Visibility.Visible;

				if (scaledExtent.Width < viewportSize.Width || scaledExtent.Height < viewportSize.Height)
				{
					OffsetAndClip(x, y, scaledExtent);

					Debug.WriteLine($"Scaled Extent is smaller than viewportSize. CanvasOffset: {x}, {y} (InvertedY: {invertedY}). Clip Size: {scaledExtent}, Viewport Size: {viewportSize}.");
				}
				else
				{
					Debug.WriteLine($"Scaled Extent is NOT smaller than viewportSize. Drawing rectangle at {x}, {y} (InvertedY: {invertedY}).");

					BitmapGridControl1.CanvasOffset = VectorDbl.Zero;
				}
			}
		}

		private void OffsetAndClip(double x, double y, SizeDbl clipSize) 
		{
			// Center the Canvas
			BitmapGridControl1.CanvasOffset = new VectorDbl(x, -1 * y);

			// Clip the Image
			var clipRect = new Rect(new Point(x, y), ScreenTypeHelper.ConvertToSize(clipSize));
			BitmapGridControl1.Clip = new RectangleGeometry(clipRect);
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

		#endregion
	}
}
