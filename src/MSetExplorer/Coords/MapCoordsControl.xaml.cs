using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapCoordsControl.xaml
	/// </summary>
	public partial class MapCoordsControl : UserControl
	{
		private MapCoordsViewModel _vm;

		public MapCoordsControl()
		{
			_vm = (MapCoordsViewModel)DataContext;
			Loaded += MapCoordsControl_Loaded;
			InitializeComponent();
		}

		private void MapCoordsControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapCoordsControl is being loaded.");
				return;
			}
			else
			{
				_vm = (MapCoordsViewModel)DataContext;
				txtStartX.AcceptsReturn = true;

				//Debug.WriteLine("The MapCoordsControl is now loaded");
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			//if (!int.TryParse(txtStartX.Text, out var exp))
			//{
			//	exp = 0;
			//}

			//txtStartY.Text = _vm.Test(txtEndX.Text, exp);

			////txtEndY.Text = _vm.Test(txtStartY.Text);

			////_  = _vm.Test(txtStartY.Text);

			//_vm.SaveCoords();

		}
	}
}
