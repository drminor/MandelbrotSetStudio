using MSetExplorer.ScreenHelpers;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer.XPoc
{
	/// <summary>
	/// Interaction logic for SamplePointDeltaTestWindow.xaml
	/// </summary>
	public partial class SamplePointDeltaTestWindow : Window, IHaveAppNavRequestResponse
	{
		private SamplePointDeltaTestViewModel _vm;


		#region Constructor

		public SamplePointDeltaTestWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = (SamplePointDeltaTestViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += SamplePointDeltaTestWindow_Loaded;
			InitializeComponent();
		}

		private void SamplePointDeltaTestWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the SamplePointDeltaTest Window is being loaded.");
				return;
			}
			else
			{
				_vm = (SamplePointDeltaTestViewModel)DataContext;

				Debug.WriteLine("The SamplePointDeltaTest Window is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		//private void SldrCanvasWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		//{
		//	_vm.ScreenSize = new SizeInt((int)Math.Round(e.NewValue));
		//}

		//private void SldrSelectionWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		//{
		//	_vm.SelectionSize = new SizeDbl(e.NewValue);
		//}

		#endregion

		#region Button Handlers

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.ReturnToTopNav);
			Close();
		}

		private void ExitButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.Close);
			Close();
		}

		#endregion

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }
	}
}
