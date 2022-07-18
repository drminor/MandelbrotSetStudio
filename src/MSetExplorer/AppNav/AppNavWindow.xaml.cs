﻿using MSetExplorer.ScreenHelpers;
using MSetExplorer.XPoc;
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

		private void AppNavWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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

				if (Properties.Settings.Default.ShowTopNav)
				{
					WindowState = WindowState.Normal;
				}
				else
				{
					//var initialCommand = new AppNavRequestResponse(OnCloseBehavior.Close, RequestResponseCommand.OpenPoster, new string[] { "Test" });
					//AppNavRequestResponse? initialCommand = null;

					//var lastWindowName = Properties.Settings.Default.LastWindowName;
					var lastWindowName = "Explorer";
					var initialCommand = new AppNavRequestResponse(OnCloseBehavior.Close, RequestResponseCommand.OpenProject, new string[] { "CloserA1" });

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

		private void SampleTestButton_Click(object sender, RoutedEventArgs e)
		{
			GoToSampleTest();
		}

		private void ExitAppButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void RemoveMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			//var createdDate = DateTime.Parse("2022-05-29");
			//var numberOfRecordsProcessed = _vm.DeleteMapSectionsCreatedSince(createdDate);

			var numberOfRecordsProcessed = _vm.DoSchemaUpdates();
			_ = MessageBox.Show($"{numberOfRecordsProcessed} MapSections processed.");
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
			var explorerWindow = new ExplorerWindow(appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest())
			{
				DataContext = explorerViewModel
			};

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
			var designerWindow = new PosterDesignerWindow(appNavRequestResponse ?? AppNavRequestResponse.BuildEmptyRequest())
			{
				DataContext = posterDesignerViewModel
			};

			_lastWindow = designerWindow;
			_lastWindow.Name = "Designer";
			_lastWindow.Closed += LastWindow_Closed;

			designerWindow.Owner = Application.Current.MainWindow;
			designerWindow.Show();
			_ = designerWindow.Focus();
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

		#region Nav Window Support

		private Action<AppNavRequestResponse?> GetRoute(string lastWindowName)
		{
			switch (lastWindowName)
			{
				case "Explorer": return GoToExplorer;
				case "Designer": return GoToDesigner;
				case "xSampling": return GoToSampleTest;
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
	}
}
