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
				_vm.ViewportSize = PanAndZoomControl1.ViewportSize;

				if (_vm.ZoomSliderFactory != null)
				{
					PanAndZoomControl1.ZoomSliderOwner = _vm.ZoomSliderFactory(PanAndZoomControl1);
				}

				//BitmapGridControl1.ViewportSizeChanged += BitmapGridControl1_ViewportSizeChanged;

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

		//private void BitmapGridControl1_ViewportSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	_vm.ViewportSize = e.Item2;
		//}

		private void PanAndZoomControl1_ViewportChanged(object? sender, ScaledImageViewInfo e)
		{
			BitmapGridControl1.ContentViewportSize = e.ContentViewportSize;
			
			//_vm.ViewportSize = e.ContentViewportSize;
			//_vm.HorizontalPosition = e.OffsetX;
			//_vm.VerticalPosition = e.OffsetY;

			_vm.UpdateViewportSizeAndPos(e.ContentViewportSize, e.PositionRelativeToPosterMapBlockOffset);
		}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			_vm.VerticalPosition = PanAndZoomControl1.ContentOffsetY;
		}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			_vm.HorizontalPosition = PanAndZoomControl1.ContentOffsetX;
		}

		#endregion
	}
}
