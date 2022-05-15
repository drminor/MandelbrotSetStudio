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
		private MSetInfoViewModel _vm;

		public MapCoordsControl()
		{
			_vm = (MSetInfoViewModel)DataContext;
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
				_vm = (MSetInfoViewModel)DataContext;
				_vm.PropertyChanged += ViewModel_PropertyChanged;
				txtStartX.AcceptsReturn = true;

				//Debug.WriteLine("The MapCoordsControl is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(MSetInfoViewModel.CoordsAreDirty))
			{
				dispSecMapCoordsCommit.Visibility = _vm.CoordsAreDirty ? Visibility.Visible : Visibility.Collapsed;
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
