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

			// Use the TEST RECTANGLE instead (0,0 -> 1,1)
			var testMSetInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, RMapConstants.TEST_RECTANGLE);
			_vm.LoadMap("initial job", canvasSize, testMSetInfo, TransformType.None, canvasSize);
		}

		private SizeInt GetCanvasControlSize()
		{
			//var width = 1024;
			//var height = 1024;

			var width = 768;
			var height = 768;
			return new SizeInt(width, height);
		}

		private void NavigateButton_Click(object sender, RoutedEventArgs e)
		{
			//var rect = new Rect(new Point(16, 16), new Size(128, 128));
			var rect = new Rect(new Point(12, 12), new Size(96, 96));
			var coords = GetCoords(rect);

			Debug.WriteLine($"Starting Job with new coords: {coords}.");
			var newArea = new SizeInt((int)Math.Round(rect.Width), (int)Math.Round(rect.Height));
			LoadMap(coords, TransformType.Zoom, newArea);
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			var canvasSize = GetCanvasControlSize();
			_vm.GoBack(canvasSize);
			btnGoBack.IsEnabled = _vm.CanGoBack;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private RRectangle GetCoords(Rect rect)
		{
			var curJob = _vm.CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
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

		private void LoadMap(RRectangle coords, TransformType transformType, SizeInt newArea)
		{
			var canvasSize = GetCanvasControlSize();
			var curMSetInfo = _vm.CurrentJob.MSetInfo;
			var mSetInfo = MSetInfo.UpdateWithNewCoords(curMSetInfo, coords);

			var label = "Zoom:" + _jobNameCounter++.ToString();
			_vm.LoadMap(label, canvasSize, mSetInfo, transformType, newArea);
			btnGoBack.IsEnabled = _vm.CanGoBack;
		}

	}
}
