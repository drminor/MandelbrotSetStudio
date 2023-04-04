using MSetExplorer.ScreenHelpers;
using MSetExplorer.XPoc;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for TestPanelTestWindow.xaml
	/// </summary>
	public partial class TestPanelTestWindow : Window, IHaveAppNavRequestResponse
	{
		//.private XSamplingEditorViewModel _vm;

		public TestPanelTestWindow(AppNavRequestResponse appNavRequestResponse)
		{
			//_vm = _vm = (XSamplingEditorViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += TestPanelTestWindow_Loaded;
			InitializeComponent();
		}

		private void TestPanelTestWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the TestPanelTestWindow Window is being loaded.");
				return;
			}
			else
			{
				//_vm = (XSamplingEditorViewModel)DataContext;

				Debug.WriteLine("The TestPanelTestWindow Window is now loaded");
			}
		}

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
