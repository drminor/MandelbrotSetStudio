using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBand.xaml
	/// </summary>
	public partial class ColorBandView : UserControl
	{
		private IColorBandViewModel _vm;

		#region Constructor 

		public ColorBandView()
		{
			InitializeComponent();
			Loaded += ColorBandView_Loaded;
		}

		private void ColorBandView_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ColorBandView is being loaded.");
				return;
			}
			else
			{
				_vm = (IColorBandViewModel)DataContext;

				lvColorBands.ItemsSource = _vm.ColorBands;
				lvColorBands.SelectionChanged += LvColorBands_SelectionChanged;

				Debug.WriteLine("The ColorBandView is now loaded");
			}
		}

		private void LvColorBands_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Debug.WriteLine($"The current cutoff is {_vm.SelectedColorBand?.CutOff}.");
		}

		#endregion

		#region Button Handlers

		private void TestButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.Test();
		}

		#endregion
	}
}
