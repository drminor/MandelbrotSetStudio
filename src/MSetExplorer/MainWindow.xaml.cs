using MSetExplorer.MapWindow;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel _vm;
		private SelectionRectangle _selectedArea;
		private IDictionary<PointInt, ScreenSection> _screenSections;

		public MainWindow()
		{
			_selectedArea = null;
			Loaded += MainWindow_Loaded;
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_vm = (MainWindowViewModel)DataContext;
			_selectedArea = new SelectionRectangle(MainCanvas, _vm.BlockSize);
			_vm.Progress = new Progress<MapSection>(HandleMapSectionReady);
			_screenSections = new Dictionary<PointInt, ScreenSection>();

			var canvasSize = GetCanvasControlSize(MainCanvas);
			var maxIterations = 700;
			var mSetInfo = MSetInfoHelper.BuildInitialMSetInfo(maxIterations);
			_vm.LoadMap(canvasSize, mSetInfo, clearExistingMapSections: false);
		}

		private SizeInt GetCanvasControlSize(Canvas canvas)
		{
			var width = (int)Math.Round(canvas.Width);
			var height = (int)Math.Round(canvas.Height);
			return new SizeInt(width, height);
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			Debug.WriteLine($"Drawing a bit map at {mapSection.CanvasPosition}.");

			var screenSection = GetScreenSection(mapSection);
			screenSection.WritePixels(mapSection.Pixels1d);
		}

		private ScreenSection GetScreenSection(MapSection mapSection)
		{
			if (!_screenSections.TryGetValue(mapSection.CanvasPosition, out var screenSection))
			{
				screenSection = new ScreenSection(mapSection.Size);
				var cIndex = MainCanvas.Children.Add(screenSection.Image);

				MainCanvas.Children[cIndex].SetValue(Canvas.LeftProperty, (double)mapSection.CanvasPosition.X);
				MainCanvas.Children[cIndex].SetValue(Canvas.BottomProperty, (double)mapSection.CanvasPosition.Y);
				MainCanvas.Children[cIndex].SetValue(Panel.ZIndexProperty, 0);
			}

			return screenSection;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			//foreach(UIElement c in MainCanvas.Children)
			//{
			//	if(c is Image i)
			//	{
			//		i.Visibility = Visibility.Hidden;
			//	}
			//}

			//var canvasSize = GetCanvasControlSize(MainCanvas);
			//var mSetInfo = _vm.CurrentJob.MSetInfo;

			//if (mSetInfo != null)
			//{
			//	var currentCmEntry6 = mSetInfo.ColorMapEntries[6];
			//	mSetInfo.ColorMapEntries[6] = new ColorMapEntry(currentCmEntry6.CutOff, new ColorMapColor("#DD2050"), ColorMapBlendStyle.None, currentCmEntry6.EndColor);
			//	_vm.LoadMap(canvasSize, mSetInfo);
			//}
		}

		private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Get position of mouse relative to the main canvas and invert the y coordinate.
			var position = e.GetPosition(relativeTo: MainCanvas);
			position = new Point(position.X, MainCanvas.ActualHeight - position.Y);

			Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {position}.");

			if (!_selectedArea.IsActive)
			{
				_selectedArea.Activate(position);
			}
			else
			{
				if (_selectedArea.Contains(position))
				{
					_selectedArea.IsActive = false;

					Debug.WriteLine($"Will start job here with position: {position}.");

					var area = _selectedArea.Area;
					var coords = GetCoords(area);

					var disp = BigIntegerHelper.GetDisplay(coords.Values, coords.Exponent);
					Debug.WriteLine($"Starting Job with new coords: {disp}.");
					LoadMap(coords, clearExistingMapSections: false);
				}
			}
		}

		private RRectangle GetCoords(Rect area)
		{
			var areaInt = new RectangleInt(
				new PointInt((int)Math.Round(area.X), (int)Math.Round(area.Y)),
				new SizeInt((int)Math.Round(area.Width), (int)Math.Round(area.Height))
				);

			var curJob = _vm.CurrentJob;

			var curPos = curJob.MSetInfo.Coords.LeftBot;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var offset = samplePointDelta.Scale(areaInt.Point);
			RMapHelper.NormalizeInPlace(ref curPos, ref offset);

			var newPos = curPos.Translate(offset);
			var newSize = samplePointDelta.Scale(areaInt.Size);

			//var dispP1 = BigIntegerHelper.GetDisplay(newPos);
			//var dispS1 = BigIntegerHelper.GetDisplay(newSize);

			//var newPos2 = newPos.Clone();
			//var newSize2 = newSize.Clone();

			//RMapHelper.NormalizeInPlace(ref newPos2, ref newSize2);
			//var dispP2 = BigIntegerHelper.GetDisplay(newPos2);
			//var dispS2 = BigIntegerHelper.GetDisplay(newSize2);

			//Debug.WriteLine($"Before: {dispP1}, {dispS1} and After: {dispP2}, {dispS2}");
			//var result = new RRectangle(newPos2.X, newPos2.X + newSize2.Width, newPos2.Y, newPos2.Y + newSize2.Height, newPos2.Exponent);

			RMapHelper.NormalizeInPlace(ref newPos, ref newSize);
			var result = new RRectangle(newPos.X, newPos.X + newSize.Width, newPos.Y, newPos.Y + newSize.Height, newPos.Exponent);

			return result;
		}

		private void LoadMap(RRectangle coords, bool clearExistingMapSections)
		{
			var canvasSize = GetCanvasControlSize(MainCanvas);
			var curMSetInfo = _vm.CurrentJob.MSetInfo;

			var mSetInfo = new MSetInfo(curMSetInfo, coords);

			foreach (UIElement c in MainCanvas.Children)
			{
				if (c is Image i)
				{
					i.Visibility = Visibility.Hidden;
				}
			}

			_vm.LoadMap(canvasSize, mSetInfo, clearExistingMapSections);
		}

		private class ScreenSection
		{
			public Image Image { get; init; }
			//public Histogram Histogram { get; init; }

			public ScreenSection(SizeInt size)
			{
				Image = CreateImage(size);
			}

			public void WritePixels(byte[] pixels)
			{
				var bitmap = (WriteableBitmap)Image.Source;

				var w = (int) Math.Round(Image.Width);
				var h = (int) Math.Round(Image.Height);

				var rect = new Int32Rect(0, 0, w, h);
				var stride = 4 * w;
				bitmap.WritePixels(rect, pixels, stride, 0);

				Image.Visibility = Visibility.Visible;
			}

			private Image CreateImage(SizeInt size)
			{
				var result = new Image
				{
					Width = size.Width,
					Height = size.Height,
					Stretch = Stretch.None,
					Margin = new Thickness(0),
					Source = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgra32, null)
				};

				return result;
			}

		}


	}
}
