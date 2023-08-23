using MSS.Types;
using System;
using System.ComponentModel;
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
			_useDetailedDebug = true;

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

				// Starting with ContentScale = 1.
				// The (logical) ViewportSize on the VM is the same size as the UnscaledViewportSize on the PanAndZoom control. 
				var ourSize = HistogramColorBandControl1.ViewportSize;
				PanAndZoomControl1.UnscaledViewportSize = ourSize;
				_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;
				_vm.ContentViewportSize = _vm.ViewportSize;

				PlaceTheColorBandControl(HistogramPlotControl1.ViewportOffsetX, HistogramPlotControl1.ViewportWidth);

				PanAndZoomControl1.ZoomOwner = new ZoomSlider(cbshZoom1.scrollBar1, PanAndZoomControl1);

				_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;
				_vm.PropertyChanged += CbsHistogramControl_PropertyChanged;

				PanAndZoomControl1.ViewportChanged += ViewportChanged;
				PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetChanged;

				HistogramPlotControl1.ViewportOffsetXChanged += HistogramPlotControl1_ViewportOffsetXChanged;
				HistogramPlotControl1.ViewportWidthChanged += HistogramPlotControl1_ViewportWidthChanged;

				HistogramColorBandControl1.ColorBandCutoffChanged += HistogramColorBandControl1_ColorBandCutoffChanged;

				Debug.WriteLine("The CbsHistogramControl is now loaded.");
			}
		}

		private void CbsHistogramControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.DisplaySettingsInitialized -= _vm_DisplaySettingsInitialzed;
			_vm.PropertyChanged -= CbsHistogramControl_PropertyChanged;

			PanAndZoomControl1.ViewportChanged -= ViewportChanged;
			PanAndZoomControl1.ContentScaleChanged -= ContentScaleChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= ContentOffsetChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= ContentOffsetChanged;

			HistogramPlotControl1.ViewportOffsetXChanged -= HistogramPlotControl1_ViewportOffsetXChanged;
			HistogramPlotControl1.ViewportWidthChanged -= HistogramPlotControl1_ViewportWidthChanged;

			HistogramColorBandControl1.ColorBandCutoffChanged -= HistogramColorBandControl1_ColorBandCutoffChanged;

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

			//var unscaledViewportWidth = PanAndZoomControl1.UnscaledViewportSize.Width;

			var viewPortWidth = HistogramPlotControl1.PlotWidth;

			var unscaledExtentWidth = e.UnscaledExtent.Width;

			// Instead of using the unscaledViewportWidth, use the physical view port width - to find the ContentScale corresponding to filling the entire display.
			//var minContentScale = unscaledViewportWidth / unscaledExtentWidth;
			var minContentScale = viewPortWidth / unscaledExtentWidth;

			var maxContentScale = minContentScale * 4;

			_vm.MaximumDisplayZoom = maxContentScale;
			var contentScale = minContentScale; // + 1;

			Debug.WriteLine($"");

			//Debug.WriteLineIf(_useDetailedDebug, $"\n ========== The CbsHistogramControl is handling VM.DisplaySettingsInitialzed. Extent: {e.UnscaledExtent}, Offset: {e.ContentOffset}, " +
			//	$"Scale: {contentScale}, MinScale: {minContentScale}, MaxScale: {maxContentScale}.");

			Debug.WriteLine($"\n ========== The CbsHistogramControl is handling VM.DisplaySettingsInitialzed. Extent: {e.UnscaledExtent}, Offset: {e.ContentOffset}, " +
				$"Scale: {contentScale}, MinScale: {minContentScale}, MaxScale: {maxContentScale}.");

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

			PlaceTheColorBandControl(HistogramPlotControl1.ViewportOffsetX, newValue);
		}

		private void HistogramPlotControl1_ViewportOffsetXChanged(object? sender, (double, double) e)
		{
			var previousValue = e.Item1;
			var newValue = e.Item2;

			Debug.WriteLine($"The CbsHistogramControl is handling the HistogramPlotControl's ViewportOffsetXChanged event. The ColorBandControl's OffsetX is being updated from {previousValue} to {newValue}.");

			PlaceTheColorBandControl(newValue, HistogramPlotControl1.ViewportWidth);
		}

		private void CbsHistogramControl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ICbsHistogramViewModel.ColorBandsView))
			{
				HistogramColorBandControl1.ColorBandsView = _vm.ColorBandsView;
			}
		}

		private void HistogramColorBandControl1_ColorBandCutoffChanged(object? sender, (int, int) e)
		{
			_vm.UpdateColorBandCutoff(e.Item1, e.Item2);
		}

		#endregion

		#region Private Methods

		private void PlaceTheColorBandControl(double viewportOffsetX, double viewportWidth)
		{
			var column2Width = PlotAreaBorder.ActualWidth;

			if (double.IsNaN(column2Width) || double.IsNaN(viewportOffsetX) || double.IsNaN(viewportWidth) || viewportWidth < 100)
			{
				return;
			}

			var leftMargin = viewportOffsetX;
			var rightMargin = column2Width - (viewportWidth + leftMargin);

			if (rightMargin < 0)
			{
				Debug.WriteLine($"The CbsHistogramControl found the Right Margin to be {rightMargin}, setting this to zero instead. LeftMargin: {leftMargin}, ViewportWidth: {viewportWidth}, Control Width: {column2Width}.");
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
