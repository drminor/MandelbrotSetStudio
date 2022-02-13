using MSS.Common;
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
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (MainWindowViewModel)DataContext;
				_vm.PropertyChanged += VmPropertyChanged;

				_mapDisplay = mapDisplay1;
				_mapDisplay.DataContext = DataContext;
				_mapDisplay.AreaSelected += MapDisplay_AreaSelected;

				btnGoBack.IsEnabled = _vm.CanGoBack;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		private void MapDisplay_AreaSelected(object sender, MapWindow.AreaSelectedEventArgs e)
		{
			var curJob = _vm.CurrentJob;
			var position = curJob.MSetInfo.Coords.LeftBot;
			var canvasControlOffset = curJob.CanvasControlOffset;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			// Adjust the selected area's origin to account for the portion of the start block that is off screen.
			var canvasOffset = new SizeInt((int)Math.Round(canvasControlOffset.Width), (int)Math.Round(canvasControlOffset.Height));
			var adjArea = e.Area.Translate(canvasOffset);

			var coords = RMapHelper.GetMapCoords(adjArea, position, samplePointDelta);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {e.TransformType}.");
			_vm.UpdateMapView(e.TransformType, e.Area.Size, coords);
		}

		private void VmPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CanGoBack")
			{
				btnGoBack.IsEnabled = _vm.CanGoBack;
				return;
			}

			if (e.PropertyName == "CanvasSize")
			{
				Debug.WriteLine($"The MapDisplay's canvas size is being updated. The new value is {_vm.CanvasSize}.");
				return;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.CanGoBack)
			{
				_vm.GoBack();
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			//_vm.
		}
	}
}
