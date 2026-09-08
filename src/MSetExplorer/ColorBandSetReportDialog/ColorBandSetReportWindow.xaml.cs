using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetReportWindow.xaml
	/// </summary>
	public partial class ColorBandSetReportWindow : Window
	{
		private ColorBandSetReportViewModel _vm;

		#region Constructor

		public ColorBandSetReportWindow()
		{
			_vm = (ColorBandSetReportViewModel)DataContext;

			Loaded += ColorBandSetReportWindow_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetReportWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ColorBandSetReportWindow is being loaded.");
				return;
			}
			else
			{
				_vm = (ColorBandSetReportViewModel)DataContext;
				//borderTitle.DataContext = _vm.ColorBandSet;
				borderReport.DataContext = DataContext;

				lvColorMapCountInfos.ItemsSource = _vm.ColorMapCountInfos;
				txtName.Focus();

				Debug.WriteLine("The ColorBandSetReportWindow is now loaded");
			}
		}

		#endregion

		#region Button Handlers

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion

	}
}
