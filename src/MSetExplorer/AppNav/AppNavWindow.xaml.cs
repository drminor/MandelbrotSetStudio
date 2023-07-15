using MSetExplorer.ScreenHelpers;
using MSetExplorer.XPoc;
using MSetExplorer.XPoc.BitmapGridControl;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for AppNavWindow.xaml
	/// </summary>
	public partial class AppNavWindow : Window
	{
		private AppNavViewModel _vm;
		private Window? _lastWindow;

		#region Constructor

		public AppNavWindow()
		{
			_lastWindow = null;

			_vm = (AppNavViewModel)DataContext;
			Loaded += AppNavWindow_Loaded;
			Closing += AppNavWindow_Closing;
			InitializeComponent();
		}

		private void AppNavWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			ExitApp();
		}

		private void AppNavWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the AppNav Window is being loaded.");
				return;
			}
			else
			{
				_vm = (AppNavViewModel)DataContext;
				btnFirstButton.Focus();

				if (Properties.Settings.Default.ShowTopNav)
				{
					WindowState = WindowState.Normal;
				}
				else
				{
					//var lastWindowName = "Designer";
					//var initialCommand = new AppNavRequestResponse(OnCloseBehavior.Close, RequestResponseCommand.OpenPoster, new string[] { "Test" });

					//var lastWindowName = "Explorer";
					//var initialCommand = new AppNavRequestResponse(OnCloseBehavior.Close, RequestResponseCommand.OpenProject, new string[] { "CloserA1" });

					AppNavRequestResponse? initialCommand = null;
					var lastWindowName = Properties.Settings.Default.LastWindowName;

					var route = GetRoute(lastWindowName);
					route(initialCommand); 
				}

				Debug.WriteLine("The AppNav Window is now loaded");
			}
		}

		#endregion

		#region Button Handlers

		private void ExploreButton_Click(object sender, RoutedEventArgs e)
		{
			GoToExplorer();
		}

		private void DesignPosterButton_Click(object sender, RoutedEventArgs e)
		{
			GoToDesigner();
		}

		private void ShowPerformanceHarnessMainWin_Click(object sender, RoutedEventArgs e)
		{
			GoToPerformanceHarnessMainWindow();
		}

		private void ShowBitmapGridTestWindow_Click(object sender, RoutedEventArgs e)
		{
			GoToBitmapGridTestWindow();
		}

		private void SampleTestButton_Click(object sender, RoutedEventArgs e)
		{
			GoToSampleTest();
		}

		private void ShowSystemColorsButton_Click(object sender, RoutedEventArgs e)
		{
			GoToSystemColors();
		}

		private void ModelStorage_Click(object sender, RoutedEventArgs e)
		{
			GoToModelStorage();
		}

		private void ExitAppButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void RemoveMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			_ = MessageBox.Show("This command is currently coded to perform no action.");
			//var createdDate = DateTime.Parse("2022-05-29");
			//var numberOfRecordsProcessed = _vm.DeleteMapSectionsCreatedSince(createdDate);

			//var numberOfRecordsProcessed = _vm.DoSchemaUpdates();
			//_ = MessageBox.Show($"{numberOfRecordsProcessed} MapSections processed.");
		}

		#endregion

		private void ExitApp()
		{
			if (_lastWindow != null)
			{
				Properties.Settings.Default.LastWindowName = _lastWindow.Name;
				Properties.Settings.Default.Save();
			}

			Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
		}

		private void GoToExplorer(AppNavRequestResponse? appNavRequestResponse = null)
		{
			Hide();

			var explorerViewModel = _vm.GetExplorerViewModel();
			var explorerWindow = new ExplorerWindow(explorerViewModel, appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest());

			_lastWindow = explorerWindow;
			_lastWindow.Name = "Explorer";
			_lastWindow.Closed += LastWindow_Closed;

			explorerWindow.Owner = Application.Current.MainWindow;
			explorerWindow.Show();
			_ = explorerWindow.Focus();
		}

		private void GoToDesigner(AppNavRequestResponse? appNavRequestResponse = null)
		{
			Hide();

			var posterDesignerViewModel = _vm.GetPosterDesignerViewModel();
			var designerWindow = new PosterDesignerWindow(posterDesignerViewModel, appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest());

			_lastWindow = designerWindow;
			_lastWindow.Name = "Designer";
			_lastWindow.Closed += LastWindow_Closed;

			designerWindow.Owner = Application.Current.MainWindow;
			designerWindow.Show();
			_ = designerWindow.Focus();
		}

		private void GoToPerformanceHarnessMainWindow(AppNavRequestResponse? appNavRequestResponse = null)
		{
			Hide();

			var performanceHarnessViewModel = _vm.GetPerformanceHarnessMainWinViewModel();
			var performanceHarnessMainWindow = new PerformanceHarnessMainWindow(appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest())
			{
				DataContext = performanceHarnessViewModel
			};

			_lastWindow = performanceHarnessMainWindow;
			_lastWindow.Name = "PerformanceHarness";
			_lastWindow.Closed += LastWindow_Closed;

			performanceHarnessMainWindow.Owner = Application.Current.MainWindow;
			performanceHarnessMainWindow.Show();
			_ = performanceHarnessMainWindow.Focus();
		}

		private void GoToSampleTest(AppNavRequestResponse? appNavRequestResponse = null)
		{
			Hide();

			var xSamplingEditorViewModel = _vm.GetXSamplingEditorViewModel();
			var xSamplingEditorWindow = new XSamplingEditorWindow(appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest())
			{
				DataContext = xSamplingEditorViewModel
			};

			_lastWindow = xSamplingEditorWindow;
			_lastWindow.Name = "xSampling";
			_lastWindow.Closed += LastWindow_Closed;

			xSamplingEditorWindow.Owner = Application.Current.MainWindow;
			xSamplingEditorWindow.Show();
			_ = xSamplingEditorWindow.Focus();
		}

		private void GoToBitmapGridTestWindow(AppNavRequestResponse? appNavRequestResponse = null)
		{
			Hide();

			var bitmapGridTestViewModel = new BitmapGridTestViewModel();
			var bitmapGridTestWindow = new BitmapGridTestWindow(appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest())
			{
				DataContext = bitmapGridTestViewModel
			};

			_lastWindow = bitmapGridTestWindow;
			_lastWindow.Name = "BitmapGridTestWindow";
			_lastWindow.Closed += LastWindow_Closed;

			bitmapGridTestWindow.Owner = Application.Current.MainWindow;
			bitmapGridTestWindow.Show();
			_ = bitmapGridTestWindow.Focus();
		}

		private void GoToSystemColors(AppNavRequestResponse? appNavRequestResponse = null)
		{
			Hide();

			var sysColorsWindow = new SysColorsWindow(appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest(onCloseBehavior: OnCloseBehavior.ReturnToTopNav));

			_lastWindow = sysColorsWindow;
			_lastWindow.Name = "SysColors";
			_lastWindow.Closed += LastWindow_Closed;

			sysColorsWindow.Owner = Application.Current.MainWindow;
			sysColorsWindow.Show();
			_ = sysColorsWindow.Focus();
		}

		private void GoToModelStorage(AppNavRequestResponse? appNavRequestResponse = null)
		{
			//var storageModelPoc = _vm.GetStorageModelPOC();

			//var projectId = new ObjectId("6258fe80712f62b28ce55c15");

			//storageModelPoc.PlayWithStorageModel(projectId);
		}

		#region Nav Window Support

		private Action<AppNavRequestResponse?> GetRoute(string lastWindowName)
		{
			switch (lastWindowName)
			{
				case "Explorer": return GoToExplorer;
				case "Designer": return GoToDesigner;
				case "xSampling": return GoToSampleTest;
				case "SysColors": return GoToSystemColors;
				case "PerformanceHarness": return GoToPerformanceHarnessMainWindow;
				case "BitmapGridTestWindow": return GoToBitmapGridTestWindow;
				default:
					return x => WindowState = WindowState.Normal;
			}
		}

		private void LastWindow_Closed(object? sender, System.EventArgs e)
		{
			if (_lastWindow != null)
			{
				_lastWindow.Closed -= LastWindow_Closed;
			}

			if (_lastWindow is IHaveAppNavRequestResponse navWin)
			{
				HandleNavWinClosing(navWin);
			}
			else
			{
				CloseOrShow(GetOnCloseBehavior(Properties.Settings.Default.ShowTopNav));
			}
		}

		private void HandleNavWinClosing(IHaveAppNavRequestResponse navWin)
		{
			if (navWin.AppNavRequestResponse.ResponseCommand is RequestResponseCommand responseCommand)
			{
				if (responseCommand == RequestResponseCommand.OpenPoster)
				{
					var requestCommand = navWin.AppNavRequestResponse.BuildRequestFromResponse();
					GoToDesigner(requestCommand);
					return;
				}
			}

			CloseOrShow(navWin.AppNavRequestResponse.OnCloseBehavior);
		}

		private void CloseOrShow(OnCloseBehavior onCloseBehavior)
		{
			if (onCloseBehavior == OnCloseBehavior.ReturnToTopNav)
			{
				Show();
				WindowState = WindowState.Normal;
			}
			else
			{
				Close();
			}
		}

		private OnCloseBehavior GetOnCloseBehavior(bool showTopNav)
		{
			return showTopNav ? OnCloseBehavior.ReturnToTopNav : OnCloseBehavior.Close;
		}

		#endregion

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
