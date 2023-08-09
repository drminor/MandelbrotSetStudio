using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CbsHistogramControl.xaml
	/// </summary>
	public partial class CbsHistogramControl : UserControl
	{
		#region Private Fields

		private ICbsHistogramViewModel _vm;

		private readonly bool _useDetailedDebug;

		#endregion

		#region Constructor

		public CbsHistogramControl()
		{
			_useDetailedDebug = false;

			_vm = (CbsHistogramViewModel)DataContext;

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

				var ourSize = HistogramColorBandControl1.ViewportSize;

				PanAndZoomControl1.UnscaledViewportSize = ourSize;
				_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;
				_vm.ContentViewportSize = _vm.ViewportSize;						// Starting with ContentScale = 1 

				PanAndZoomControl1.ZoomOwner = new ZoomSlider(cbshZoom1.scrollBar1, PanAndZoomControl1);

				_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;

				PanAndZoomControl1.ViewportChanged += ViewportChanged;
				PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetChanged;

				HistogramPlotControl1.ViewportOffsetXChanged += HistogramPlotControl1_ViewportOffsetXChanged;
				HistogramPlotControl1.ViewportWidthChanged += HistogramPlotControl1_ViewportWidthChanged;


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

			HistogramPlotControl1.ViewportOffsetXChanged -= HistogramPlotControl1_ViewportOffsetXChanged;
			HistogramPlotControl1.ViewportWidthChanged -= HistogramPlotControl1_ViewportWidthChanged;

			PanAndZoomControl1.Dispose();
			PanAndZoomControl1.ZoomOwner = null;
		}

		#endregion

		#region Event Handlers

		private void _vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		{
			// NOTE: 	ContentViewportSize = UnscaledViewportSize.Divide(ContentScale);
			//			ContentScale = UnscaledViewportSize / ContentViewportSize
			//			UnscaledViewportSize = ContentViewportSize * ContentScale

			var unscaledViewportWidth = PanAndZoomControl1.UnscaledViewportSize.Width;
			var unscaledExtentWidth = e.UnscaledExtent.Width;

			//var extentAtMaxZoom = unscaledExtentWidth * RMapConstants.DEFAULT_CBS_HIST_DISPLAY_SCALE_FACTOR; // * 4;

			var minContentScale = unscaledViewportWidth / unscaledExtentWidth;
			var maxContentScale = minContentScale * 4;

			_vm.MaximumDisplayZoom = maxContentScale;
			var contentScale = minContentScale;


			//Debug.Assert(minContentScale == maxContentScale / 4.0, "Check this.");

			//Debug.Assert(minContentScale / maxContentScale > 0.01, "The ratio between the Min and Max scales is suprisingly small.");

			Debug.WriteLine($"");

			Debug.WriteLineIf(_useDetailedDebug, $"\n ========== The CbsHistogramControl is handling VM.DisplaySettingsInitialzed. Extent: {e.UnscaledExtent}, Offset: {e.ContentOffset}, " +
				$"Scale: {contentScale}, MinScale: {minContentScale}, MaxScale: {maxContentScale}. Ratio of min over max: {minContentScale / maxContentScale}");

			_vm.DisplayZoom = PanAndZoomControl1.ResetExtentWithPositionAndScale(e.UnscaledExtent, e.ContentOffset, contentScale, minContentScale, maxContentScale);
		}

		private void ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, "\n========== The CbsHistogramControl is handling the PanAndZoom control's ViewportChanged event.");

			ReportViewportChanged(e);
			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The CbsHistogramControl is returning from UpdatingViewportSizeAndPos.\n");
		}

		private void ContentScaleChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, "\n========== The CbsHistogramControl is handling the PanAndZoom control's ContentScaleChanged event.");
			ReportViewportChanged(e);

			_vm.UpdateViewportSizePosAndScale(e.ContentViewportSize, e.ContentOffset, e.ContentScale);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The CbsHistogramControl is returning from UpdatingViewportSizePosAndScale.\n");
		}

		private void ContentOffsetChanged(object? sender, EventArgs e)
		{
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset);
		}

		private void HistogramPlotControl1_ViewportWidthChanged(object? sender, (double, double) e)
		{
			var previousValue = e.Item1;
			var newValue = e.Item2;

			Debug.WriteLine($"The CbsHistogramControl is handling the HistogramPlotControl's ViewportWidthChanged event. DisplayZoom: {_vm.DisplayZoom}. The ColorBandControl's Width is being updated from {previousValue} to {newValue}.");

			PositionAndSizeTheColorBandControl(PlotAreaBorder.ActualWidth, HistogramPlotControl1.ViewportOffsetX, newValue);
		}

		private void HistogramPlotControl1_ViewportOffsetXChanged(object? sender, (double, double) e)
		{
			var previousValue = e.Item1;
			var newValue = e.Item2;

			Debug.WriteLine($"The CbsHistogramControl is handling the HistogramPlotControl's ViewportOffsetXChanged event. The ColorBandControl's OffsetX is being updated from {previousValue} to {newValue}.");

			PositionAndSizeTheColorBandControl(PlotAreaBorder.ActualWidth, newValue, HistogramPlotControl1.ViewportWidth);
		}

		private void PositionAndSizeTheColorBandControl(double column2Width, double viewportOffsetX, double viewportWidth)
		{
			if (double.IsNaN(column2Width) | double.IsNaN(viewportOffsetX) | double.IsNaN(viewportWidth))
			{
				return;
			}

			var leftMargin = viewportOffsetX;

			var rightMargin = column2Width - (viewportWidth + leftMargin);

			if (rightMargin < 0)
			{
				Debug.WriteLine($"The Right Margin was calculated to be < 0. Setting to 0. LeftMargin: {leftMargin}, ViewportWidth: {viewportWidth}, Control Width: {column2Width}.");
				rightMargin = 0;
			}

			Debug.WriteLine($"The CbsHistogramControl is setting the ColorBandControl Border Margins to L:{leftMargin} and R:{rightMargin}.");

			ColorBandAreaBorder.Margin = new Thickness(leftMargin, 0, rightMargin, 2);
		}

		#endregion

		#region Diagnostics

		private void ReportViewportChanged(ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl is UpdatingViewportSizeAndPos. ViewportSize: Scaled:{e.ContentViewportSize} " + //  / Unscaled: {e.UnscaledViewportSize},
				$"Offset:{e.ContentOffset}, Scale:{e.ContentScale}.");
		}

		// Just for diagnostics
		private void CbsHistogramControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_vm != null)
			{
				var cntrlSize = new SizeDbl(ActualWidth, ActualHeight);
				Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogram_Control_SizeChanged. Control: {cntrlSize}, Canvas:{_vm.ViewportSize}, ContentViewPort: {_vm.ContentViewportSize}, Unscaled: {_vm.UnscaledExtent}.");
			}
		}

		#endregion
	}
}
