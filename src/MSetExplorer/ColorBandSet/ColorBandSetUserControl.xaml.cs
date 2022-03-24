using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBand.xaml
	/// </summary>
	public partial class ColorBandSetUserControl : UserControl
	{
		private IColorBandSetViewModel _vm;

		#region Constructor 

		public ColorBandSetUserControl()
		{
			InitializeComponent();
			Loaded += ColorBandSetUserControl_Loaded;
		}

		private void ColorBandSetUserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandColorUserControl is being loaded.");
				return;
			}
			else
			{
				//lvColorBandsHdr.Width = lvColorBands.ActualWidth - 25;

				_vm = (IColorBandSetViewModel)DataContext;
				_vm.PropertyChanged += ViewModel_PropertyChanged;

				//_vm.ItemWidth = lvColorBands.ActualWidth - 5;

				//lvColorBands.ItemsSource = _vm.ColorBands;
				//lvColorBands.SelectionChanged += LvColorBands_SelectionChanged;

				//lvColorBands

				Debug.WriteLine("The ColorBandSetUserControl is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_vm.ColorBandSet))
			{
				//Debug.WriteLine("Refreshing the ColorBandSet UserControl's ListView's ItemSource.");
				//lvColorBands.ItemsSource = null;
				//lvColorBands.ItemsSource = _vm.ColorBands;

			}
		}

		//private void LvColorBands_SelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//	Debug.WriteLine($"The current cutoff is {_vm.SelectedColorBand?.CutOff}.");
		//}

		#endregion

		#region Button Handlers

		private void InsertButton_Click(object sender, RoutedEventArgs e)
		{
			foreach(var x in lvColorBands.Items)
			{
				if(x is ListViewItem lvi)
				{
					lvi.IsSelected = false;
				}
			}
			_vm.InsertItem();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.DeleteSelectedItem();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.ApplyChanges();
		}

		//private void Test1Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test1();
		//}

		//private void Test2Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test2();
		//}

		//private void Test3Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test3();
		//}

		//private void Test4Button_Click(object sender, RoutedEventArgs e)
		//{
		//	_vm.Test4();
		//}

		#endregion
	}
}
