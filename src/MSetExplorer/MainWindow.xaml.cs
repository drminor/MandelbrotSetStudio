using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel _vm;
		private MapDisplay _mapDisplay;

		public MainWindow()
		{
			Loaded += MainWindow_Loaded;
			ContentRendered += MainWindow_ContentRendered;
			InitializeComponent();
		}

		private void MainWindow_ContentRendered(object sender, EventArgs e)
		{
			Debug.WriteLine("The MainWindow is handling ContentRendered");

			var maxIterations = 700;
			var mSetInfo = MapWindowHelper.BuildInitialMSetInfo(maxIterations);

			_vm.SetMapInfo(mSetInfo);

			//_vm.LoadProject();
			//btnGoBack.IsEnabled = _vm.CanGoBack;
			//btnGoForward.IsEnabled = _vm.CanGoForward;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (MainWindowViewModel)DataContext;
				_vm.PropertyChanged += VmPropertyChanged;

				_mapDisplay = mapDisplay1;
				_mapDisplay.DataContext = DataContext;
				_mapDisplay.AreaSelected += MapDisplay_AreaSelected;
				_mapDisplay.ScreenPanned += MapDisplay_ScreenPanned;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		private void MapDisplay_AreaSelected(object sender, MapWindow.AreaSelectedEventArgs e)
		{
			_vm.UpdateMapViewZoom(e.Area);
		}

		private void MapDisplay_ScreenPanned(object sender, MapWindow.ScreenPannedEventArgs e)
		{
			_vm.UpdateMapViewPan(e.Offset);
		}

		private void VmPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CanGoBack")
			{
				btnGoBack.IsEnabled = _vm.CanGoBack;
				return;
			}

			if (e.PropertyName == "CanGoForward")
			{
				btnGoForward.IsEnabled = _vm.CanGoForward;
				return;
			}

			if (e.PropertyName == "CanvasSize")
			{
				Debug.WriteLine($"The MapDisplay's canvas size is being updated. The new value is {_vm.CanvasSize}.");
				return;
			}
		}

		#region Button Handlers

		private void GoLeftButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new SizeInt(32, 0));
		}

		private void GoUpButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new SizeInt(0, 32));
		}

		private void GoRightButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new SizeInt(-32, 0));
		}

		private void GoDownButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new SizeInt(0, -32));
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.CanGoBack)
			{
				_vm.GoBack();
			}
		}

		private void GoForwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.CanGoForward)
			{
				_vm.GoForward();
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.SaveProject();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#endregion
	}
}
