using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Windows.UI.WebUI;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionDisplayControl.xaml
	/// </summary>
	public partial class MapSectionPzControl : UserControl
	{
		#region Private Fields

		private const double POSTER_DISPLAY_MARGIN = 20;

		private IMapDisplayViewModel _vm;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
			_vm = (IMapDisplayViewModel)DataContext;

			Loaded += MapSectionPzControl_Loaded;
			Unloaded += MapSectionPzControl_Unloaded;

			InitializeComponent();
		}

		private void MapSectionPzControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLineIf(_useDetailedDebug, "The DataContext is null as the MapSectionDisplayControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IMapDisplayViewModel)DataContext;

				BitmapGridControl1.UseScaling = true;
				var ourSize = BitmapGridControl1.ViewportSize;

				PanAndZoomControl1.UnscaledViewportSize = ourSize;
				_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;

				_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;
				PanAndZoomControl1.ViewportChanged += ViewportChanged;
				PanAndZoomControl1.ContentScaleChanged += ContentScaleChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetYChanged;

				Debug.WriteLineIf(_useDetailedDebug, "The MapSectionPzControl is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.DisplaySettingsInitialized -= _vm_DisplaySettingsInitialzed;

			PanAndZoomControl1.ViewportChanged -= ViewportChanged;
			PanAndZoomControl1.ContentScaleChanged -= ContentScaleChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= ContentOffsetYChanged;

			PanAndZoomControl1.Dispose();
			PanAndZoomControl1.ZoomOwner = null;
		}

		#endregion

		#region Event Handlers

		private void _vm_DisplaySettingsInitialzed(object? sender, DisplaySettingsInitializedEventArgs e)
		{
			var maxContentScale = _vm.MaximumDisplayZoom;
			var unscaledViewportSize = PanAndZoomControl1.UnscaledViewportSize;

			var minContentScale = RMapHelper.GetMinDisplayZoom(e.UnscaledExtent, unscaledViewportSize, POSTER_DISPLAY_MARGIN, maxContentScale);

			if (minContentScale <= 0)
			{
				minContentScale = 0.0001;	
			}

			_vm.DisplayZoom = PanAndZoomControl1.ResetExtentWithPositionAndScale(e.UnscaledExtent, e.ContentOffset, e.ContentScale, minContentScale, maxContentScale);
		}

		private void ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			// TODO: Update how the ViewportChanged event is handled -- since it will never include an updated ContentScale.

			Debug.WriteLineIf(_useDetailedDebug, "\n========== The MapSectionPzControl is handling the PanAndZoom control's ViewportChanged event.");
			ReportViewportChanged(e);

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset);
			CheckForOutofSyncLogicalVpSize(BitmapGridControl1.LogicalViewportSize, _vm.LogicalViewportSize);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The MapSectionPzControl is returning from UpdatingViewportSizeAndPos. The ImageOffset is {BitmapGridControl1.ImageOffset}\n");
		}

		private void ContentScaleChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, "\n========== The MapSectionPzControl is handling the PanAndZoom control's ContentScaleChanged event.");

			ReportViewportChanged(e);

			_vm.UpdateViewportSizePosAndScale(e.ContentViewportSize, e.ContentOffset, e.ContentScale);
			CheckForOutofSyncLogicalVpSize(BitmapGridControl1.LogicalViewportSize, _vm.LogicalViewportSize);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The MapSectionPzControl is returning from UpdatingViewportSizePosAndScale. The ImageOffset is {BitmapGridControl1.ImageOffset}\n");
		}

		private void ContentOffsetXChanged(object? sender, EventArgs e)
		{
			CheckForOutofSyncContentVpSize(PanAndZoomControl1.ContrainedViewportSize, _vm.ContentViewportSize);
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset);
		}

		private void ContentOffsetYChanged(object? sender, EventArgs e)
		{
			CheckForOutofSyncContentVpSize(PanAndZoomControl1.ContrainedViewportSize, _vm.ContentViewportSize);
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset);
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void CheckForOutofSyncLogicalVpSize(SizeDbl viewPortsizeBitmapGridControl, SizeDbl viewportSizeVm)
		{
			if (ScreenTypeHelper.IsSizeDblChanged(viewPortsizeBitmapGridControl, viewportSizeVm))
			{
				Debug.WriteLine($"The LogicalViewportSize from the BitmapGridControl is not the same as the value from the MapSectionDisplayViewModel. " +
					$"BitmapGridControl's value: {viewPortsizeBitmapGridControl}, MapSectionDisplayViewModel's value: {viewportSizeVm}.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckForOutofSyncContentVpSize(SizeDbl contentVpSizeFromPz, SizeDbl? contentVpSizeVm)
		{
			if (contentVpSizeVm == null)
			{
				throw new InvalidOperationException("The VM's ContentViewPortsize is null on call to MoveTo");
			}

			if (ScreenTypeHelper.IsSizeDblChanged(contentVpSizeFromPz, contentVpSizeVm.Value))
			{
				Debug.WriteLine($"The ContentViewportSize from the PanAndZoomControl is not the same as the value from the MapSectionDisplayViewModel. " +
					$"PanAndZoomControl's value: {contentVpSizeFromPz}, MapSectionDisplayViewModel's value: {contentVpSizeVm}.");
			}
		}

		private void ReportViewportChanged(ScaledImageViewInfo e)
		{
			var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(e.ContentScale);
			Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionPzControl is UpdatingViewportSizeAndPos. ViewportSize: Scaled:{e.ContentViewportSize} " + //  / Unscaled: {e.UnscaledViewportSize},
				$"Offset:{e.ContentOffset}, Scale:{e.ContentScale}. BaseFactor: {baseFactor}, RelativeScale: {relativeScale}.");
		}

		#endregion
	}
}
