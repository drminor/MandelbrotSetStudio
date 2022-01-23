using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel _vm;
		//private Rectangle _selectedArea;
		private IDictionary<PointInt, ScreenSection> _screenSections;

		public MainWindow()
		{
			//_selectedArea = null;
			Loaded += MainWindow_Loaded;
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_vm = (MainWindowViewModel)DataContext;
			_vm.Progress = new Progress<MapSection>(HandleMapSectionReady);

			_screenSections = new Dictionary<PointInt, ScreenSection>();

			//SetupSelectionRect();

			var canvasSize = GetCanvasControlSize(MainCanvas);

			var maxIterations = 700;
			var mSetInfo = MSetInfoHelper.BuildInitialMSetInfo(maxIterations);

			// Uncomment to clear the existing MapSection records for this subdivision.
			//var numberDeleted = _vm.ClearMapSections(canvasSize, mSetInfo);

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
			if (!_screenSections.TryGetValue(mapSection.CanvasPosition, out ScreenSection screenSection))
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
			foreach(UIElement c in MainCanvas.Children)
			{
				if(c is Image i)
				{
					i.Visibility = Visibility.Hidden;
				}
			}

			var canvasSize = GetCanvasControlSize(MainCanvas);
			var mSetInfo = _vm.CurrentJob.MSetInfo;

			if (mSetInfo != null)
			{
				var currentCmEntry6 = mSetInfo.ColorMapEntries[6];
				mSetInfo.ColorMapEntries[6] = new ColorMapEntry(currentCmEntry6.CutOff, new ColorMapColor("#DD2050"), ColorMapBlendStyle.None, currentCmEntry6.EndColor);
				_vm.LoadMap(canvasSize, mSetInfo);
			}
		}

		private void MainCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var position = e.GetPosition(relativeTo: MainCanvas);
			var blockPosition = _vm.GetBlockPosition(position);

			//if (_selectedArea.Visibility == Visibility.Hidden)
			//{
			//	MoveSelectionRect(blockPosition);
			//	_selectedArea.Visibility = Visibility.Visible;
			//}
			//else
			//{
			//	if(IsSelectedRectHere(blockPosition))
			//	{
			//		_selectedArea.Visibility = Visibility.Hidden;
			//	}
			//	else
			//	{
			//		MoveSelectionRect(blockPosition);
			//	}
			//}

			//if (e.ClickCount == 1)
			//{
			//	_selectedArea.Visibility = _selectedArea.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
			//}
			//else
			//{
			//	if (_selectedArea.Visibility == Visibility.Visible)
			//	{
			//		Debug.WriteLine("Will start new job.");
			//	}
			//	else
			//	{
			//		_selectedArea.Visibility = Visibility.Visible;
			//	}
			//}
		}


		//private void SetupSelectionRect()
		//{
		//	_selectedArea = new Rectangle()
		//	{
		//		Width = _vm.BlockSize.Width,
		//		Height = _vm.BlockSize.Height,
		//		Stroke = Brushes.Black,
		//		StrokeThickness = 2,
		//		Visibility = Visibility.Hidden
		//	};

		//	_ = MainCanvas.Children.Add(_selectedArea);
		//	_selectedArea.SetValue(Panel.ZIndexProperty, 10);

		//	MoveSelectionRect(new PointInt(0, 0));
		//}

		//private void MoveSelectionRect(PointInt position)
		//{
		//	Debug.WriteLine($"Moving the sel rec to {position}");

		//	_selectedArea.SetValue(Canvas.LeftProperty, (double) position.X);
		//	_selectedArea.SetValue(Canvas.BottomProperty, (double) position.Y);
		//}

		//private bool IsSelectedRectHere(PointInt position)
		//{
		//	var rPosX = (int)Math.Round((double)_selectedArea.GetValue(Canvas.LeftProperty));
		//	var rPosY = (int)Math.Round((double)_selectedArea.GetValue(Canvas.BottomProperty));

		//	var rOffset = new SizeInt(rPosX, rPosY);

		//	Debug.WriteLine($"Comparing {position} to {rOffset}");

		//	var diff = position.Diff(rOffset).Abs();
		//	var result = diff.X < 2 && diff.Y < 2;

		//	return result;
		//}

		private void MainCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
		}

		private void MainCanvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			//if (!(_selectedArea is null))
			//{
			//	_selectedArea.StrokeThickness = 2;
			//}
		}

		private void MainCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			//if (!(_selectedArea is null))
			//{
			//	_selectedArea.StrokeThickness = 0;
			//}
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
