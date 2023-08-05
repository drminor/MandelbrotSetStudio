using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;

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

			// Just for diagnostics
			SizeChanged += CbsHistogramControl_SizeChanged;

			InitializeComponent();
		}

		private void CbsHistogramControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the CbsHistogramControl is being loaded.");
				return;
			}
			else
			{
				_vm = (CbsHistogramViewModel)DataContext;

				var ourSize = HistogramDisplayControl1.ViewportSize;

				PanAndZoomControl1.UnscaledViewportSize = ourSize;
				_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;

				//PanAndZoomControl1.MaxContentScale = 10;
				//PanAndZoomControl1.MinContentScale = 1;

				PanAndZoomControl1.ZoomOwner = new ZoomSlider(cbshZoom1.scrollBar1, PanAndZoomControl1);

				_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;

				PanAndZoomControl1.ViewportChanged += ViewportChanged;
				PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetChanged;

				//_outline = BuildOutline(HistogramDisplayControl1.Canvas);

				Debug.WriteLine("The CbsHistogramControl is now loaded.");
			}
		}

		private void CbsHistogramControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.DisplaySettingsInitialized -= _vm_DisplaySettingsInitialzed;

			PanAndZoomControl1.ViewportChanged -= ViewportChanged;
			PanAndZoomControl1.ContentScaleChanged -= ContentScaleChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= ContentOffsetChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= ContentOffsetChanged;

			PanAndZoomControl1.Dispose();
			PanAndZoomControl1.ZoomOwner = null;
		}

		#endregion

		#region Event Handlers

		private void _vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		{
			//var maxContentScale = _vm.MaximumDisplayZoom;
			//var contentViewportSize = UnscaledViewportSize.Divide(contentScale);

			var unscaledViewportWidth = PanAndZoomControl1.UnscaledViewportSize.Width;
			var unscaledExtentWidth = e.UnscaledExtent.Width;

			var extentAtMaxZoom = unscaledExtentWidth * RMapConstants.DEFAULT_CBS_HIST_DISPLAY_SCALE_FACTOR; // * 4;
			var maxContentScale = extentAtMaxZoom / unscaledViewportWidth;
			_vm.MaximumDisplayZoom = maxContentScale;

			var minContentScale = unscaledViewportWidth / unscaledExtentWidth;

			//var minContentScale = Math.Min(minScale, maxContentScale);
			//if (minContentScale <= 0)
			//{
			//	minContentScale = RMapConstants.DEFAULT_MINIMUM_DISPLAY_ZOOM;
			//}

			Debug.Assert(minContentScale / maxContentScale > 0.01, "The ratio between the Min and Max scales is suprisingly small.");

			_vm.DisplayZoom = PanAndZoomControl1.ResetExtentWithPositionAndScale(e.UnscaledExtent, e.ContentOffset, e.ContentScale, minContentScale, maxContentScale);
		}


		public static double GetMinDisplayZoom(SizeDbl extent, SizeDbl viewportSize, double margin, double maximumZoom)
		{
			// Calculate the Zoom level at which the poster fills the screen, leaving a pixel border of size margin.

			var framedViewPort = viewportSize.Sub(new SizeDbl(margin));
			var minScale = framedViewPort.Divide(extent);
			var result = Math.Min(minScale.Width, minScale.Height);
			result = Math.Min(result, maximumZoom);

			return result;
		}


		private void ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, "\n========== The CbsHistogramControl is handling the PanAndZoom control's ViewportChanged event.");

			ReportViewportChanged(e);
			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The CbsHistogramControl is returning from UpdatingViewportSizeAndPos. The ImageOffset is {HistogramDisplayControl1.ImageOffset}\n");
		}

		private void ContentScaleChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, "\n========== The CbsHistogramControl is handling the PanAndZoom control's ContentScaleChanged event.");
			ReportViewportChanged(e);

			_vm.UpdateViewportSizePosAndScale(e.ContentViewportSize, e.ContentOffset, e.ContentScale);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The CbsHistogramControl is returning from UpdatingViewportSizePosAndScale. The ImageOffset is {HistogramDisplayControl1.ImageOffset}\n");
		}

		private void ContentOffsetChanged(object? sender, EventArgs e)
		{
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset);
		}

		// Just for diagnostics
		private void CbsHistogramControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_vm != null)
			{
				var cntrlSize = new SizeDbl(ActualWidth, ActualHeight);
				Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogram_Control_SizeChanged. Control: {cntrlSize}, Canvas:{_vm.CanvasSize}, ViewPort: {_vm.ViewportSize}, Unscaled: {_vm.UnscaledExtent}.");
			}
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

		#region Diagnostics

		private void ReportViewportChanged(ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl is UpdatingViewportSizeAndPos. ViewportSize: Scaled:{e.ContentViewportSize} " + //  / Unscaled: {e.UnscaledViewportSize},
				$"Offset:{e.ContentOffset}, Scale:{e.ContentScale}.");
		}

		#endregion
	}
}
