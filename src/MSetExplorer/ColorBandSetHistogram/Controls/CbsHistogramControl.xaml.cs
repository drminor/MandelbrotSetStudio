﻿using MSS.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
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

				HistogramColorBandControl1.ContextMenu.PlacementTarget = this;

				HistogramColorBandControl1.ColorBandsView = _vm.ColorBandsView;
				HistogramColorBandControl1.UseRealTimePreview = _vm.UseRealTimePreview;

				MouseEnter += HandleMouseEnter;
				MouseLeave += HandleMouseLeave;

				Focusable = true;

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

			MouseEnter -= HandleMouseEnter;
			MouseLeave -= HandleMouseLeave;
		}

		#endregion

		#region Event Handlers

		private void Vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		{
			// NOTE:
			//	1. ContentViewportSize = UnscaledViewportSize.Divide(ContentScale);
			//	2. ContentScale = UnscaledViewportSize / ContentViewportSize
			//	3. UnscaledViewportSize = ContentViewportSize * ContentScale

			var viewPortWidth = HistogramPlotControl1.PlotDataWidth; // - 25;

			if (viewPortWidth < 100)
			{
				viewPortWidth = 100;
			}

			var unscaledExtentWidth = e.UnscaledExtent.Width;
			//var minContentScale = (viewPortWidth - 2) / unscaledExtentWidth;
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
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl is updating the HistogramColorBandControl's ColorBandView.");

				HistogramColorBandControl1.ColorBandsView = _vm.ColorBandsView;
			}

			// UseRealTimePreview
			else if (e.PropertyName == nameof(ICbsHistogramViewModel.UseRealTimePreview))
			{
				HistogramColorBandControl1.UseRealTimePreview = _vm.UseRealTimePreview;
			}

			//else if (e.PropertyName == nameof(ICbsHistogramViewModel.HorizontalScrollBarVisibility))
			//{
			//	//HistogramColorBandControl1.IsHorizontalScrollBarVisible = _vm.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
			//}
		}

		#endregion

		#region Mouse and Keyboard Event Handlers

		private void HandleMouseLeave(object sender, MouseEventArgs e)
		{
			HistogramColorBandControl1.HideSelectionLines(e.LeftButton == MouseButtonState.Pressed);
		}

		private void HandleMouseEnter(object sender, MouseEventArgs e)
		{
			HistogramColorBandControl1.ShowSelectionLines(e.LeftButton == MouseButtonState.Pressed);
		}

		#endregion

		#region Button Click Event Handlers

		private void MoveLeftButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.TryMoveCurrentColorBandToPrevious();
		}

		private void MoveRightButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.TryMoveCurrentColorBandToNext();
		}

		#endregion

		#region Command Binding Handlers

		// Insert CanExecute
		private void InsertCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			bool canExecute;

			var vm = _vm;

			var currentColorBand = _vm?.CurrentColorBand;

			if (vm == null || currentColorBand == null || vm.ColorBandUserControlHasErrors)
			{
				canExecute = false;
			}
			else
			{
				if (currentColorBand.IsFirst)
				{
					canExecute = false;
				}
				else
				{
					canExecute = true;
				}
			}

			e.CanExecute = canExecute;

		}

		// Insert
		private void InsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var selItem = GetColorBandAtMousePosition();

			if (selItem != null)
			{
				_ = _vm.TryInsertNewItem(selItem, out var index);
				Debug.WriteLine($"CbsHistogramControl. The new ColorBand: {selItem} has been inserted at index: {index}.");
			}
		}

		// Delete CanExecute
		private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			bool canExecute;

			var vm = _vm;

			var currentColorBand = _vm?.CurrentColorBand;

			if (vm == null || currentColorBand == null || vm.ColorBandUserControlHasErrors)
			{
				canExecute = false;
			}
			else
			{
				if (currentColorBand.IsLast)
				{
					canExecute = false;
				}
				else
				{
					if (vm.ColorBandsCount == 2)
					{
						canExecute = false;
					}
					else
					{
						canExecute = true;
					}
				}
			}

			e.CanExecute = canExecute;
		}

		// Delete
		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var selItem = GetColorBandAtMousePosition();

			if (selItem != null)
			{
				_ = _vm.TryDeleteSelectedItem(selItem);
			}
		}

		// Revert CanExecute
		private void RevertCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm != null && _vm.IsDirty && !_vm.ColorBandUserControlHasErrors;
		}

		// Revert
		private void RevertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_vm.ColorBandUserControlHasErrors)
			{
				return;
			}
			else
			{
				_vm.RevertChanges();
			}
		}

		// Apply CanExecute
		private void ApplyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm != null && _vm.IsDirty && !_vm.ColorBandUserControlHasErrors;
		}

		// Apply
		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_vm.ColorBandUserControlHasErrors)
			{
				return;
			}
			else
			{
				_vm.ApplyChanges();
			}
		}

		private void ShowDetails_Click(object sender, RoutedEventArgs e)
		{
			string msg;
			var selItem = GetColorBandAtMousePosition();

			if (selItem != null)
			{
				//msg = $"Percentage: {selItem.Percentage}, Count: {specs.Count}, Exact Count: {specs.ExactCount}";
				msg = $"Percentage: {selItem.Percentage}";

				var index = _vm.ColorBandsView.IndexOf(selItem);

				if (index == -1)
				{
					throw new InvalidOperationException("The ColorBand found at the current mouse position cannot be found in the ColorBandsView.");
				}

				if (index == _vm.ColorBandsCount - 1 && _vm.BeyondTargetSpecs != null)
				{
					var specs = _vm.BeyondTargetSpecs;
					msg += $"\nBeyond Last Info: Percentage: {specs.Percentage}, Count: {specs.Count}, Exact Count: {specs.ExactCount}";
				}

				ReportHistogram(_vm.GetHistogramForColorBand(selItem));
			}
			else
			{
				msg = "No Current Item.";
			}

			_ = MessageBox.Show(msg);
		}

		private ColorBand? GetColorBandAtMousePosition()
		{
			var posOfContextMenu = HistogramColorBandControl1.MousePositionWhenContextMenuWasOpened;
			var result = HistogramColorBandControl1.GetItemUnderMouse(posOfContextMenu);

			if (result == null)
			{
				result = _vm.CurrentColorBand;
			}

			if (result == null)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"GetColorBandAtMousePosition. Could not identify any ColorBand at {posOfContextMenu}.");
			}
			else
			{
				ConfirmColorBandBelongsToView(result);
			}

			return result;
		}

		private void ReportHistogram(IDictionary<int, int> histogram)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Histogram:");

			foreach (KeyValuePair<int, int> kvp in histogram)
			{
				sb.AppendLine($"\t{kvp.Key} : {kvp.Value}");
			}

			Debug.WriteLine(sb.ToString());
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

			//Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramControl is setting the ColorBandControl Border Margins to L:{leftMargin} and R:{rightMargin}.");

			Debug.WriteLine($"The CbsHistogramControl is setting the ColorBandControl Border Margins to L:{leftMargin} and R:{rightMargin}. The Column2Width:{column2Width}, the borderWidth: {borderWidth}, viewportOffsetX: {viewportOffsetX}, viewportWidth: {viewportWidth}");


			ColorBandAreaBorder.Margin = new Thickness(leftMargin, 0, rightMargin, 5);
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

		[Conditional("DEBUG")]
		private void ConfirmColorBandBelongsToView(ColorBand? colorBand)
		{
			var index = _vm.ColorBandsView.IndexOf(colorBand);

			if (index == -1)
			{
				Debug.WriteLine($"Could not the ColorBand: {colorBand} in the VM's ColorBandsView.");
			}
		}

		#endregion

	}
}
