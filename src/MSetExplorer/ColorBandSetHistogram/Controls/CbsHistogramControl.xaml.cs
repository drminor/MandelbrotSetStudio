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
	/// Interaction logic for CbsHistogramControl.xaml
	/// </summary>
	public partial class CbsHistogramControl : UserControl
	{
		#region Private Fields

		//private bool DRAW_OUTLINE = false;
		//private Rectangle _outline;
		private ICbsHistogramViewModel _vm;

		private readonly bool _useDetailedDebug;


		#endregion

		#region Constructor

		public CbsHistogramControl()
		{
			_useDetailedDebug = false;

			_vm = (CbsHistogramViewModel)DataContext;
			//_outline = new Rectangle();

			Loaded += CbsHistogramControl_Loaded;
			Unloaded += CbsHistogramControl_Unloaded;
			SizeChanged += CbsHistogramControl_SizeChanged;

			InitializeComponent();
		}

		private void CbsHistogramControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_vm != null)
			{
				var cntrlSize = new SizeDbl(ActualWidth, ActualHeight);
				Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogram_Control_SizeChanged. Control: {cntrlSize}, Canvas:{_vm.CanvasSize}, ViewPort: {_vm.ViewportSize}, Unscaled: {_vm.UnscaledExtent}.");
			}
		}

		private void CbsHistogramControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandSetHistogram UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (CbsHistogramViewModel)DataContext;

				_vm.UpdateViewportSize(PanAndZoomControl1.UnscaledViewportSize);

				PanAndZoomControl1.MaxContentScale = 10;
				PanAndZoomControl1.MinContentScale = 1;

				PanAndZoomControl1.ZoomOwner = new ZoomSlider(cbshZoom1.scrollBar1, PanAndZoomControl1);

				PanAndZoomControl1.ViewportChanged += PanAndZoomControl1_ViewportChanged;
				PanAndZoomControl1.ContentOffsetXChanged += PanAndZoomControl1_ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += PanAndZoomControl1_ContentOffsetYChanged;

				//_outline = BuildOutline(HistogramDisplayControl1.Canvas);

				Debug.WriteLine("The ColorBandSetHistogram UserControl is now loaded.");
			}
		}

		private void CbsHistogramControl_Unloaded(object sender, RoutedEventArgs e)
		{
			PanAndZoomControl1.ZoomOwner = null;

			PanAndZoomControl1.ViewportChanged -= PanAndZoomControl1_ViewportChanged;
			PanAndZoomControl1.ContentOffsetXChanged -= PanAndZoomControl1_ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= PanAndZoomControl1_ContentOffsetYChanged;
		}

		#endregion

		#region Event Handlers

		private void PanAndZoomControl1_ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale);
		}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset);
		}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset);
		}

		#endregion

		#region Private Methods

		private void ShowOutline(RectangleDbl scaledDisplayArea)
		{
			//if (DRAW_OUTLINE)
			//{
			//	// Position the outline rectangle.
			//	_outline.SetValue(Canvas.LeftProperty, scaledDisplayArea.X1);
			//	_outline.SetValue(Canvas.BottomProperty, scaledDisplayArea.Y1);

			//	_outline.Width = scaledDisplayArea.Width;
			//	_outline.Height = scaledDisplayArea.Height;
			//	_outline.Visibility = Visibility.Visible;
			//}
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
				Stroke = new SolidColorBrush(Colors.DarkSeaGreen),  //BuildDrawingBrush(), // 
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
		}

		/*		< DrawingBrush Viewport = "0,0,20,20" ViewportUnits = "Absolute" TileMode = "Tile" >
					< DrawingBrush.Drawing >
						< DrawingGroup >
							< GeometryDrawing Brush = "Black" >
								< GeometryDrawing.Geometry >
									< GeometryGroup >
										< RectangleGeometry Rect = "0,0,50,50" />
										< RectangleGeometry Rect = "50,50,50,50" />
									</ GeometryGroup >
								</ GeometryDrawing.Geometry >
							</ GeometryDrawing >
						</ DrawingGroup >
					</ DrawingBrush.Drawing >
				</ DrawingBrush >

		*/

		#endregion
	}
}
