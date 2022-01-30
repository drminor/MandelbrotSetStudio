using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapNavSimWindow.xaml
	/// </summary>
	public partial class MapNavSim : Window
	{
		private MapNavSimViewModel _vm;

		private int _jobNameCounter;


		public MapNavSim()
		{
			Loaded += MapNavSim_Loaded;
			InitializeComponent();
		}

		private void MapNavSim_Loaded(object sender, RoutedEventArgs e)
		{
			_vm = (MapNavSimViewModel)DataContext;

			btnGoBack.IsEnabled = _vm.CanGoBack;
			var canvasSize = GetCanvasControlSize();
			var maxIterations = 700;
			var mSetInfo = MapWindowHelper.BuildInitialMSetInfo(maxIterations);
			var prevSamplePointDelta = new RSize(1, 1, -8);

			_vm.LoadMap2("initial job", canvasSize, mSetInfo, canvasSize, prevSamplePointDelta, clearExistingMapSections: false);
		}

		private void NavigateButton_Click(object sender, RoutedEventArgs e)
		{
			var rect = new Rect(new Point(16, 16), new Size(128, 128));
			var coords = GetCoords(rect);

			Debug.WriteLine($"Starting Job with new coords: {BigIntegerHelper.GetDisplay(coords)}.");
			var newArea = new SizeInt((int)Math.Round(rect.Width), (int)Math.Round(rect.Height));
			LoadMap2(coords, newArea, clearExistingMapSections: false);
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			var canvasSize = GetCanvasControlSize();
			_vm.GoBack(canvasSize, clearExistingMapSections: false);
			btnGoBack.IsEnabled = _vm.CanGoBack;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private RRectangle GetCoords(Rect rect)
		{
			var curJob = _vm.CurrentRequest;
			var position = curJob.MSetInfo.Coords.LeftBot;
			var canvasControlOffset = curJob.CanvasControlOffset;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			// Adjust the selected area's origin to account for the portion of the start block that is off screen.
			var area = new RectangleInt(
				new PointInt((int)Math.Round(rect.X + canvasControlOffset.Width), (int)Math.Round(rect.Y + canvasControlOffset.Height)),
				new SizeInt((int)Math.Round(rect.Width), (int)Math.Round(rect.Height))
				);

			var result = RMapHelper.GetMapCoords(area, position, samplePointDelta);

			return result;
		}

		private void LoadMap(RRectangle coords, bool clearExistingMapSections)
		{
			var canvasSize = GetCanvasControlSize();
			var curMSetInfo = _vm.CurrentRequest.MSetInfo;
			var mSetInfo = MSetInfo.UpdateWithNewCoords(coords, curMSetInfo);

			var label = "Zoom:" + _jobNameCounter++.ToString();
			_vm.LoadMap(label, canvasSize, mSetInfo, clearExistingMapSections);
			btnGoBack.IsEnabled = _vm.CanGoBack;
		}

		private void LoadMap2(RRectangle coords, SizeInt newArea, bool clearExistingMapSections)
		{
			var canvasSize = GetCanvasControlSize();
			var curMSetInfo = _vm.CurrentRequest.MSetInfo;
			var mSetInfo = MSetInfo.UpdateWithNewCoords(coords, curMSetInfo);

			var screenSizeToMapRat = _vm.CurrentRequest.Subdivision.SamplePointDelta;

			var label = "Zoom:" + _jobNameCounter++.ToString();
			_vm.LoadMap2(label, canvasSize, mSetInfo, newArea, screenSizeToMapRat, clearExistingMapSections);
			btnGoBack.IsEnabled = _vm.CanGoBack;
		}

		private SizeInt GetCanvasControlSize()
		{
			//var width = 1024;
			//var height = 1024;

			var width = 768;
			var height = 768;
			return new SizeInt(width, height);
		}



	}
}
