using MSetExplorer.MapWindow;
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
			_vm.LoadMap(canvasSize, mSetInfo);
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

					// BlockPosition = X:1, Y:2
					// X: -1.5 to -1
					// Y: -0.5 to 0

					//x4
					//X: -6 to -4
					//Y: -2 to 0

					//var coords = new RRectangle(-6, -4, -2, 0, -2);
					//LoadMap(coords);  
				}
			}
		}

		private void LoadMap(RRectangle coords)
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

			_vm.LoadMap(canvasSize, mSetInfo);
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
