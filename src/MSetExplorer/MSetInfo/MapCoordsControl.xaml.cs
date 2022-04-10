using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapCoordsControl.xaml
	/// </summary>
	public partial class MapCoordsControl : UserControl
	{
		private MSetInfoViewModel _vm;

		public MapCoordsControl()
		{
			_vm = (MSetInfoViewModel)DataContext;
			InitializeComponent();
			Loaded += MapCoordsControl_Loaded;
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
				_vm = (MSetInfoViewModel)DataContext;

				txtStartX.LostFocus += TxtStartX_LostFocus;

				//Debug.WriteLine("The MapCoordsControl is now loaded");
			}
		}

		private void TxtStartX_LostFocus(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(txtStartX.Text, out var newValue))
			{
				_vm.StartingX = newValue.ToString();
				_vm.TriggerCoordsUpdate();
			}
		}
	}
}
