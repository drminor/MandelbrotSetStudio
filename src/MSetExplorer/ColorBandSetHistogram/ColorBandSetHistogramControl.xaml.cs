using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetHistogramControl.xaml
	/// </summary>
	public partial class ColorBandSetHistogramControl : UserControl
	{
		private ColorBandSetHistogramViewModel _vm;
		
		#region Constructor

		public ColorBandSetHistogramControl()
		{

			_vm = (ColorBandSetHistogramViewModel)DataContext;
			Loaded += ColorBandSetHistogramControl_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetHistogramControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandSetHistogram UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (ColorBandSetHistogramViewModel)DataContext;

				_vm.CbshScrollViewModel.CanvasSize = _vm.CbshDisplayViewModel.CanvasSize;

				cbshScroll1.DataContext = _vm.CbshScrollViewModel;
				cbshZoom1.DataContext = _vm.CbshScrollViewModel;

				Debug.WriteLine("The ColorBandSetHistogram UserControl is now loaded.");
			}
		}

		#endregion


	}
}
