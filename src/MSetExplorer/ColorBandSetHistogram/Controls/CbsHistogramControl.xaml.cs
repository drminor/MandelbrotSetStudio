using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CbsHistogramControl.xaml
	/// </summary>
	public partial class CbsHistogramControl : UserControl
	{
		#region Private Fields

		private ICbsHistogramViewModel _vm;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsHistogramControl()
		{
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

				PlaceTheColorBandControl(HistogramPlotControl1.ViewportOffsetAndWidth);

				PanAndZoomControl1.ZoomOwner = new ZoomSlider(cbshZoom1.scrollBar1, PanAndZoomControl1);

				_vm.DisplaySettingsInitialized += Vm_DisplaySettingsInitialzed;
				_vm.PropertyChanged += Vm_PropertyChanged;

				PanAndZoomControl1.ViewportChanged += ViewportChanged;
				PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetChanged;

				HistogramPlotControl1.ViewportOffsetAndWidthChanged += HistogramPlotControl1_ViewportOffsetAndWidthChanged;

				HistogramColorBandControl1.ColorBandsView = _vm.ColorBandsView;
				HistogramColorBandControl1.UseRealTimePreview = _vm.UseRealTimePreview;

				Debug.WriteLine("The CbsHistogramControl is now loaded.");
			}
		}

		private void CbsHistogramControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.DisplaySettingsInitialized -= Vm_DisplaySettingsInitialzed;
			_vm.PropertyChanged -= Vm_PropertyChanged;

			PanAndZoomControl1.ViewportChanged -= ViewportChanged;
			PanAndZoomControl1.ContentScaleChanged -= ContentScaleChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= ContentOffsetChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= ContentOffsetChanged;

			HistogramPlotControl1.ViewportOffsetAndWidthChanged -= HistogramPlotControl1_ViewportOffsetAndWidthChanged;

			PanAndZoomControl1.Dispose();
			PanAndZoomControl1.ZoomOwner = null;
		}

		#endregion

		#region Event Handlers

		private void Vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		{
			// NOTE:
			//	1. ContentViewportSize = UnscaledViewportSize.Divide(ContentScale);
			//	2. ContentScale = UnscaledViewportSize / ContentViewportSize
			//	3. UnscaledViewportSize = ContentViewportSize * ContentScale

			var viewPortWidth = HistogramPlotControl1.PlotDataWidth - 25;
			var unscaledExtentWidth = e.UnscaledExtent.Width;
			var minContentScale = viewPortWidth / unscaledExtentWidth;
			var contentScale = minContentScale;

			var maxContentScale = 10;
			_vm.MaximumDisplayZoom = maxContentScale;

			Debug.WriteLineIf(_useDetailedDebug, $"\n ========== The CbsHistogramControl is handling VM.DisplaySettingsInitialzed. ViewportWidth: {viewPortWidth}, Extent: {e.UnscaledExtent}, Offset: {e.ContentOffset}, " +
				$"Scale: {contentScale}, MinScale: {minContentScale}, MaxScale: {maxContentScale}.");

			_ = PanAndZoomControl1.ResetExtentWithPositionAndScale(e.UnscaledExtent, e.ContentOffset, contentScale, minContentScale, maxContentScale);
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

		private void HistogramPlotControl1_ViewportOffsetAndWidthChanged(object? sender, (ControlXPositionAndWidth, ControlXPositionAndWidth) e)
		{
			var previousValue = e.Item1;
			var newValue = e.Item2;

			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl is handling the HistogramPlotControl's ViewportOffsetXChanged event. The ColorBandControl's OffsetX is being updated from {previousValue} to {newValue}.");

			PlaceTheColorBandControl(newValue);
		}

		private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// ColorBandsView
			if (e.PropertyName == nameof(ICbsHistogramViewModel.ColorBandsView))
			{
				HistogramColorBandControl1.ColorBandsView = _vm.ColorBandsView;
			}

			// UseRealTimePreview
			else if (e.PropertyName == nameof(ICbsHistogramViewModel.UseRealTimePreview))
			{
				HistogramColorBandControl1.UseRealTimePreview = _vm.UseRealTimePreview;
			}

			else if (e.PropertyName == nameof(ICbsHistogramViewModel.HorizontalScrollBarVisibility))
			{
				HistogramColorBandControl1.IsHorizontalScrollBarVisible = _vm.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
			}
		}

		#endregion

		#region Command Binding Handlers

		// Insert CanExecute
		private void InsertCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.CurrentColorBand != null;
		}

		// Insert
		private void InsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			//if (_vm.TryInsertNewItem(out var index))
			//{
			//	FocusListBoxItem(index);
			//}

			_ = _vm.TryInsertNewItem(out var index);

			Debug.WriteLine($"Will set the HistogramColorBandControl to move to the new item at index: {index}.");
		}

		// Delete CanExecute
		private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.CurrentColorBand != null; // _vm?.ColorBandsView.CurrentPosition < _vm?.ColorBandsView.Count - 1;
		}

		// Delete
		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.TryDeleteSelectedItem();
		}

		// Revert CanExecute
		private void RevertCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.IsDirty ?? false;
		}

		// Revert
		private void RevertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.RevertChanges();
		}

		// Apply CanExecute
		private void ApplyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.IsDirty ?? false;
		}

		// Apply
		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.ApplyChanges();
		}

		#endregion

		#region Private Methods

		private void PlaceTheColorBandControl(ControlXPositionAndWidth controlXPositionAndWidth)
		{
			var column2Width = PlotAreaBorder.ActualWidth;
			var borderWidth = PlotAreaBorder.BorderThickness.Left;

			var viewportOffsetX = controlXPositionAndWidth.XPosition;
			var viewportWidth = controlXPositionAndWidth.Width;

			if (double.IsNaN(borderWidth) || double.IsNaN(column2Width) || double.IsNaN(viewportOffsetX) || double.IsNaN(viewportWidth) || viewportWidth < 100)
			{
				return;
			}

			var leftMargin = viewportOffsetX + borderWidth;
			var rightMargin = column2Width - (viewportWidth + leftMargin);

			if (rightMargin < 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl found the Right Margin to be {rightMargin}, setting this to zero instead. LeftMargin: {leftMargin}, ViewportWidth: {viewportWidth}, Control Width: {column2Width}.");
				rightMargin = 0;
			}

			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl is setting the ColorBandControl Border Margins to L:{leftMargin} and R:{rightMargin}.");
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
				Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogram_Control_SizeChanged. Control: {cntrlSize}, Canvas:{_vm.ViewportSize}, ContentViewport: {_vm.ContentViewportSize}, Unscaled: {_vm.UnscaledExtent}.");
			}
		}

		#endregion

		private void MoveLeftButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.TryMoveCurrentColorBandToPrevious();
		}

		private void MoveRightButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.TryMoveCurrentColorBandToNext();
		}
	}
}
