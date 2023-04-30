using System.Diagnostics;
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
		//private IMapDisplayViewModel2 _vm;

		#region Constructor

		public MapDisplayZoomControl()
		{
			//_vm = (IMapDisplayViewModel2)DataContext;

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
				//_vm = (IMapDisplayViewModel2)DataContext;

				//scrollBarZoomValue.Minimum = 1;
				//scrollBarZoomValue.Value = 1;

				//scrollBarZoomValue.Maximum = 10;
				//scrollBarZoomValue.SmallChange = 0.1;
				//scrollBarZoomValue.LargeChange = 1;

				//scrollBarZoomValue.Value = 1;

				//scrollBarZoomValue.Scroll += scrollBarZoomValue_Scroll;

				//_vm.PropertyChanged += ViewModel_PropertyChanged;

				scrollBarZoomValue.ValueChanged += ScrollBarZoomValue_ValueChanged;

				Debug.WriteLine("The MapDisplayZoom Control is now loaded.");
			}
		}

		private void ScrollBarZoomValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			textBlockZoomValue.Text = e.NewValue.ToString("G2");
		}

		//private void scrollBarZoomValue_Scroll(object sender, ScrollEventArgs e)
		//{
		//	var zoomValue = GetZoomValue(e.ScrollEventType, e.NewValue);
		//	if (zoomValue != -1)
		//	{
		//		_vm.DisplayZoom = zoomValue;
		//	}
		//}

		//private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(IMapDisplayViewModel.MaximumDisplayZoom))
		//	{
		//		scrollBarZoomValue.Minimum = 1;
		//		scrollBarZoomValue.Maximum = 100; // _vm.MaximumDisplayZoom;
		//		scrollBarZoomValue.LargeChange = scrollBarZoomValue.Maximum / 8;
		//		scrollBarZoomValue.SmallChange = scrollBarZoomValue.LargeChange / 8;
		//	}
		//	else if (e.PropertyName == nameof(IMapDisplayViewModel.DisplayZoom))
		//	{
		//		textBlockZoomValue.Text = Math.Round(_vm.DisplayZoom, 2).ToString(CultureInfo.InvariantCulture);
		//	}
		//}

		#endregion

		#region DisplayZoom Min Max Button Handlers

		private void ButtonSetMaxZoom_Click(object sender, RoutedEventArgs e)
		{
			scrollBarZoomValue.Value = scrollBarZoomValue.Maximum;
		}

		private void ButtonSetMinZoom_Click(object sender, RoutedEventArgs e)
		{
			scrollBarZoomValue.Value = scrollBarZoomValue.Minimum;
		}

		#endregion

		//private double GetZoomValue(ScrollEventType et, double val)
		//{
		//	return et switch
		//	{
		//		ScrollEventType.EndScroll => val,
		//		ScrollEventType.First => val, // _vm.MaximumDisplayZoom,
		//		ScrollEventType.LargeDecrement => val,
		//		ScrollEventType.LargeIncrement => val,
		//		ScrollEventType.Last => 1,
		//		ScrollEventType.SmallDecrement => val,
		//		ScrollEventType.SmallIncrement => val,
		//		ScrollEventType.ThumbPosition => val,
		//		ScrollEventType.ThumbTrack => val,
		//		_ => -1,
		//	};
		//}
	}
}
