using MSetExplorer.ScreenHelpers;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using MSetExplorer.XPoc.PerformanceHarness;
using System.Diagnostics;

namespace MSetExplorer.XPoc
{
    /// <summary>
    /// Interaction logic for PerformanceHarnessMainWindow.xaml
    /// </summary>
    public partial class PerformanceHarnessMainWindow : Window, IHaveAppNavRequestResponse
	{
		private PerformanceHarnessMainWinViewModel _vm;

		#region Constructor

		public PerformanceHarnessMainWindow(AppNavRequestResponse appNavRequestResponse)
        {
			_vm = _vm = (PerformanceHarnessMainWinViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += PerformanceHarnessMainWindow_Loaded;

			InitializeComponent();
        }

		private void PerformanceHarnessMainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the PerformanceHarness Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (PerformanceHarnessMainWinViewModel)DataContext;

				Debug.WriteLine("The PerformanceHarness Main Window is now loaded");
			}
		}

		#endregion

		#region Button Handlers

		private void OneButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.RunBaseLine();
		}

		private void TwoButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Hi, Im Two.");
		}

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
