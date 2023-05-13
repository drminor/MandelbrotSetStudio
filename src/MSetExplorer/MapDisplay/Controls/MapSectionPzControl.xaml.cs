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
		#region Private Properties

		private IMapDisplayViewModel _vm;

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
			CheckForStaleContentOffset(e.ContentOffset);

			// TODO: Consider adding this to the IContentScaler interface
			BitmapGridControl1.ContentViewportSize = e.ContentViewportSize;

			var baseScale = PanAndZoomControl1.ZoomSliderOwner?.BaseValue ?? 1.0;

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.ContentOffset, e.ContentScale, baseScale);
		}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			var displayPosition = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			_ = _vm.MoveTo(displayPosition);
		}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			var displayPosition = new VectorDbl(PanAndZoomControl1.ContentOffsetX, PanAndZoomControl1.ContentOffsetY);
			_ = _vm.MoveTo(displayPosition);
		}

		#endregion

		#region Private Methods

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
