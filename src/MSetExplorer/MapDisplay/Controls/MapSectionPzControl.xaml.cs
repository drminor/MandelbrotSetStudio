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
				_vm.ViewPortSize = PanAndZoomControl1.ViewPortSize;

				if (_vm.ZoomSliderFactory != null)
				{
					PanAndZoomControl1.ZoomSliderOwner = _vm.ZoomSliderFactory(PanAndZoomControl1);
				}

				BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

				PanAndZoomControl1.ContentOffsetXChanged += PanAndZoomControl1_ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += PanAndZoomControl1_ContentOffsetYChanged;

				Debug.WriteLine("The MapSectionDisplay Control is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			PanAndZoomControl1.ZoomSliderOwner = null;

			BitmapGridControl1.ViewPortSizeChanged -= BitmapGridControl1_ViewPortSizeChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= PanAndZoomControl1_ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= PanAndZoomControl1_ContentOffsetYChanged;

		}

		#endregion

		#region Event Handlers

		private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		{
			_vm.ViewPortSize = e.Item2;
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
