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

namespace MSetExplorer.XPoc
{
	/// <summary>
	/// Interaction logic for MapAreaInfoControl.xaml
	/// </summary>
	public partial class MapAreaInfoControl : UserControl
	{

		private MapAreaInfoViewModel _vm;

		#region Constructor

		public MapAreaInfoControl()
		{
			_vm = _vm = (MapAreaInfoViewModel)DataContext;
			Loaded += MapAreaInfoControl_Loaded;
			InitializeComponent();
		}

		private void MapAreaInfoControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapAreaInfo Control is being loaded.");
				return;
			}
			else
			{
				_vm = (MapAreaInfoViewModel)DataContext;

				Debug.WriteLine("The MapAreaInfo Control is now loaded");
			}
		}

		#endregion





	}
}
