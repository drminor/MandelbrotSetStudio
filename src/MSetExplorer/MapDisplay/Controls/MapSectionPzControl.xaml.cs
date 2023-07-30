using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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

				var ourSize = new SizeDbl(ActualWidth, ActualHeight);
				PanAndZoomControl1.UnscaledViewportSize = ourSize;
				_vm.ViewportSize = PanAndZoomControl1.UnscaledViewportSize;

				_vm.DisplaySettingsInitialized += _vm_DisplaySettingsInitialzed;
				PanAndZoomControl1.ViewportChanged += ViewportChanged;

				PanAndZoomControl1.ContentOffsetXChanged += ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += ContentOffsetYChanged;

				Debug.WriteLineIf(_useDetailedDebug, "The MapSectionPzControl is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.DisplaySettingsInitialized -= _vm_DisplaySettingsInitialzed;

			PanAndZoomControl1.ViewportChanged -= ViewportChanged;

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

			if (unscaledViewportSize.IsNearZero() || unscaledViewportSize.IsNAN())
			{
				unscaledViewportSize = new SizeDbl(ActualWidth, ActualHeight);
			}

			var minContentScale = RMapHelper.GetMinDisplayZoom(e.UnscaledExtent, unscaledViewportSize, POSTER_DISPLAY_MARGIN, maxContentScale);

			if (minContentScale <= 0)
			{
				minContentScale = 0.0001;	
			}

			_vm.MinimumDisplayZoom = minContentScale;
			_vm.DisplayZoom = Math.Min(Math.Max(e.ContentScale, _vm.MinimumDisplayZoom), _vm.MaximumDisplayZoom);

			PanAndZoomControl1.ResetExtentWithPositionAndScale(e.UnscaledExtent, unscaledViewportSize, e.ContentOffset, _vm.DisplayZoom, minContentScale, maxContentScale);
		}

		private void ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			Debug.WriteLineIf(_useDetailedDebug, "\n========== The MapSectionPzControl is handling the PanAndZoom control's ViewportChanged event.");

			var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(e.ContentScale);
			Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionPzControl is UpdatingViewportSizeAndPos. ViewportSize: Scaled:{e.ContentViewportSize} / Unscaled: {e.UnscaledViewportSize}, " +
				$"Offset:{e.ContentOffset}, Scale:{e.ContentScale}. BaseFactor: {baseFactor}, RelativeScale: {relativeScale}.");

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale);

			CheckForOutofSyncLogicalVpSize(BitmapGridControl1.LogicalViewportSize, _vm.LogicalViewportSize);

			Debug.WriteLineIf(_useDetailedDebug, $"========== The MapSectionPzControl is returning from UpdatingViewportSizeAndPos. The ImageOffset is {BitmapGridControl1.ImageOffset}\n");
		}

		private void ContentOffsetXChanged(object? sender, EventArgs e)
		{
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset, PanAndZoomControl1.ContrainedViewportSize);
		}

		private void ContentOffsetYChanged(object? sender, EventArgs e)
		{
			_ = _vm.MoveTo(PanAndZoomControl1.ContentOffset, PanAndZoomControl1.ContrainedViewportSize);
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void CheckForOutofSyncScaleFactor(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl)
		{
			// TODO: As we are using a BaseScale, it might be the case where these are supposed to be different

			//Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF), "The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control.");
			if (ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF))
			{
				Debug.WriteLine($"The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control. " +
					$"PanAndZoomControl's ContentScale: {contentScaleFromPanAndZoomControl}, BitmapGridControl's ContentScale: {contentScaleFromBitmapGridControl}.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckForOutofSyncLogicalVpSize(SizeDbl viewPortsizeBitmapGridControl, SizeDbl viewportSizeVM)
		{
			// TODO: As we are using a BaseScale, it might be the case where these are supposed to be different

			//Debug.Assert(!ScreenTypeHelper.IsDoubleChanged(contentScaleFromPanAndZoomControl, contentScaleFromBitmapGridControl, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF), "The ContentScale from the PanAndZoom control is not the same as the ContentScale from the BitmapGrid control.");
			if (ScreenTypeHelper.IsSizeDblChanged(viewPortsizeBitmapGridControl, viewportSizeVM))
			{
				Debug.WriteLine($"The LogicalViewportSize from the BitmapGridControl is not the same as the value from the MapSectionDisplayViewModel. " +
					$"BitmapGridControl's value: {viewPortsizeBitmapGridControl}, MapSectionDisplayViewModel's value: {viewportSizeVM}.");
			}
		}


		#endregion
	}
}
