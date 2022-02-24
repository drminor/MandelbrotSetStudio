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
		private IMainWindowViewModel _vm;
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
				_vm = (IMainWindowViewModel)DataContext;
				_vm.PropertyChanged += VmPropertyChanged;
				_vm.MapLoaderJobStack.CurrentJobChanged += MapLoaderJobStack_CurrentJobChanged;

				_mapDisplay = mapDisplay1;
				_mapDisplay.DataContext = DataContext;
				_mapDisplay.AreaSelected += MapDisplay_AreaSelected;
				_mapDisplay.ScreenPanned += MapDisplay_ScreenPanned;

				txtIterations.TextChanged += TxtIterations_TextChanged;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		private void TxtIterations_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			try
			{
				_vm.Iterations = int.Parse(txtIterations.Text);
			}
			catch
			{

			}
		}

		private void MapLoaderJobStack_CurrentJobChanged(object sender, EventArgs e)
		{
			//_vm.Iterations = _vm.MapLoaderJobStack.CurrentJob.MSetInfo.MapCalcSettings.MaxIterations;
			//txtIterations.Text = _vm.Iterations.ToString();

			//_vm.Steps = _vm.MapLoaderJobStack.CurrentJob.MSetInfo.MapCalcSettings.IterationsPerStep;
		}

		private void MapDisplay_AreaSelected(object sender, AreaSelectedEventArgs e)
		{
			_vm.UpdateMapViewZoom(e);
		}

		private void MapDisplay_ScreenPanned(object sender, ScreenPannedEventArgs e)
		{
			_vm.UpdateMapViewPan(e);
		}

		private void VmPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CanGoBack")
			{
				btnGoBack.IsEnabled = _vm.MapLoaderJobStack.CanGoBack;
				return;
			}

			if (e.PropertyName == "CanGoForward")
			{
				btnGoForward.IsEnabled = _vm.MapLoaderJobStack.CurrentJob is null || _vm.MapLoaderJobStack.CanGoForward;
				return;
			}

			//if (e.PropertyName == "CanvasSize")
			//{
			//	Debug.WriteLine($"The MapDisplay's canvas size is being updated. The new value is {_vm.CanvasSize}.");
			//	return;
			//}
		}

		#region Button Handlers

		private void GoLeftButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new ScreenPannedEventArgs(TransformType.Pan, new SizeInt(32, 0)));
		}

		private void GoUpButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new ScreenPannedEventArgs(TransformType.Pan, new SizeInt(0,32)));
		}

		private void GoRightButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new ScreenPannedEventArgs(TransformType.Pan, new SizeInt(-32, 0)));
		}

		private void GoDownButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.UpdateMapViewPan(new ScreenPannedEventArgs(TransformType.Pan, new SizeInt(0, -32)));
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.MapLoaderJobStack.CanGoBack)
			{
				_vm.MapLoaderJobStack.GoBack();
			}
		}

		private void GoForwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.MapLoaderJobStack.CurrentJob is null)
			{
				var maxIterations = 700;
				var mSetInfo = MapWindowHelper.BuildInitialMSetInfo(maxIterations);
				_vm.SetMapInfo(mSetInfo);
			}
			else
			{
				if (_vm.MapLoaderJobStack.CanGoForward)
				{
					_vm.MapLoaderJobStack.GoForward();
				}

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
