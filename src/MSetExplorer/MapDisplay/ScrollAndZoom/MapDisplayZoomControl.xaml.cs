using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MSetExplorer.MapDisplay.ScrollAndZoom
{
	/// <summary>
	/// Interaction logic for MapDisplayZoomControl.xaml
	/// </summary>
	public partial class MapDisplayZoomControl : UserControl
	{
		private IMapDisplayViewModel _vm;

		#region Constructor

		public MapDisplayZoomControl()
		{
			_vm = (IMapDisplayViewModel)DataContext;

			Loaded += MapDisplayZoomControl_Loaded;
			InitializeComponent();
		}

		private void MapDisplayZoomControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the MapScroll UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IMapDisplayViewModel)DataContext;

				scrBarZoom.Minimum = 1;
				scrBarZoom.Value = 1;

				scrBarZoom.Maximum = 10;
				scrBarZoom.SmallChange = 0.1;
				scrBarZoom.LargeChange = 1;

				scrBarZoom.Value = 1;

				scrBarZoom.Scroll += ScrBarZoom_Scroll;

				_vm.PropertyChanged += ViewModel_PropertyChanged;

				Debug.WriteLine("The MapDisplayZoom Control is now loaded.");
			}
		}

		private void ScrBarZoom_Scroll(object sender, ScrollEventArgs e)
		{
			var zoomValue = GetZoomValue(e.ScrollEventType, e.NewValue);
			if (zoomValue != -1)
			{
				_vm.DisplayZoom = zoomValue;
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapScrollViewModel.MaximumDisplayZoom))
			{
				scrBarZoom.Minimum = 1;
				scrBarZoom.Maximum = 100; // _vm.MaximumDisplayZoom;
				scrBarZoom.LargeChange = scrBarZoom.Maximum / 8;
				scrBarZoom.SmallChange = scrBarZoom.LargeChange / 8;
			}
			else if (e.PropertyName == nameof(IMapScrollViewModel.DisplayZoom))
			{
				txtblkZoomValue.Text = Math.Round(_vm.DisplayZoom, 2).ToString(CultureInfo.InvariantCulture);
			}
		}

		#endregion

		#region DisplayZoom Min Max Button Handlers
		
		private void ButtonSetMaxZoom_Click(object sender, RoutedEventArgs e)
		{
			var max = 100; //_vm.MaximumDisplayZoom;
			scrBarZoom.Value = max;
			_vm.DisplayZoom = max;
		}

		private void ButtonSetMinZoom_Click(object sender, RoutedEventArgs e)
		{
			scrBarZoom.Value = 1;
			_vm.DisplayZoom = 1;
		}

		#endregion

		private double GetZoomValue(ScrollEventType et, double val)
		{
			return et switch
			{
				ScrollEventType.EndScroll => val,
				ScrollEventType.First => val, // _vm.MaximumDisplayZoom,
				ScrollEventType.LargeDecrement => val,
				ScrollEventType.LargeIncrement => val,
				ScrollEventType.Last => 1,
				ScrollEventType.SmallDecrement => val,
				ScrollEventType.SmallIncrement => val,
				ScrollEventType.ThumbPosition => val,
				ScrollEventType.ThumbTrack => val,
				_ => -1,
			};
		}
	}
}
