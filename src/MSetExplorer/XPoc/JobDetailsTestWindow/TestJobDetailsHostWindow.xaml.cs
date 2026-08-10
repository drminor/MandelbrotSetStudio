using MSetExplorer.ScreenHelpers;
using MSetExplorer.XPoc.PerformanceHarness;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for TestJobDetailsHostWindow.xaml
	/// </summary>
	public partial class TestJobDetailsHostWindow : Window, IHaveAppNavRequestResponse
	{
		private JobDetailsViewModel _vm;

		#region Constructor

		public TestJobDetailsHostWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = _vm = (JobDetailsViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += TestJobDetailsHostWindow_Loaded;

			InitializeComponent();
		}

		private void TestJobDetailsHostWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the TestJobDetailsHost Window is being loaded.");
				return;
			}
			else
			{
				_vm = (JobDetailsViewModel)DataContext;
				_vm.PropertyChanged += _vm_PropertyChanged;

				Debug.WriteLine("The TestJobDetailsHost Window is now loaded");
			}
		}

		private void _vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PerformanceHarnessMainWinViewModel.MathOpCounts))
			{

			}
		}

		#endregion

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

		#region Button Handlers

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			OpenJobDetailsDialog();
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

		private void OpenJobDetailsDialog()
		{
			var jobDetailsDialog = new JobDetailsWindow
			{
				DataContext = _vm
			};

			jobDetailsDialog.ShowDialog();
		}

	}
}
