using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapCoordsDetailControl.xaml
	/// </summary>
	public partial class MapCoordsDetailControl : UserControl
	{
		private MapCoordsDetailViewModel _vm;

		public MapCoordsDetailControl()
		{
			_vm = (MapCoordsDetailViewModel)DataContext;
			Loaded += MapCoordsDetailControl_Loaded;
			InitializeComponent();
		}

		private void MapCoordsDetailControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapCoordsDetail Control is being loaded.");
				return;
			}
			else
			{
				_vm = (MapCoordsDetailViewModel)DataContext;
				if (!_vm.HaveMapAreaInfo)
				{
					stkPanBlockOffsetX.Visibility = Visibility.Hidden;
					stkPanBlockOffsetY.Visibility = Visibility.Hidden;

					stkPanSamplePointDelta.Visibility = Visibility.Hidden;
					stkPanZoom.Visibility = Visibility.Hidden;
				}

				Debug.WriteLine("The MapCoordsDetail Control is now loaded");


			}
		}

	}
}
