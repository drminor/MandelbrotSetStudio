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
		private Rectangle _selectedArea;
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
			_screenSections = new Dictionary<PointInt, ScreenSection>();

			SetupSelectionRect();

			var maxIterations = 700;
			var refreshMapSections = false;

			var canvasSize = GetCanvasControlSize(MainCanvas);
			var mSetInfo = MSetInfoHelper.BuildInitialMSetInfo(maxIterations);
			var progress = new Progress<MapSection>(HandleMapSectionReady);
			_vm.LoadMap(canvasSize, mSetInfo, refreshMapSections, progress);
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

			var rect = new Int32Rect(0, 0, mapSection.Size.Width, mapSection.Size.Height);
			var stride = 4 * mapSection.Size.Width;
			var bitMap = (WriteableBitmap)screenSection.Image.Source;
			bitMap.WritePixels(rect, mapSection.Pixels1d, stride, 0);
		}

		private ScreenSection GetScreenSection(MapSection mapSection)
		{
			if (!_screenSections.TryGetValue(mapSection.CanvasPosition, out ScreenSection screenSection))
			{
				var image = CreateImage(mapSection.Size);
				var cIndex = MainCanvas.Children.Add(image);
				screenSection = new ScreenSection(image);

				MainCanvas.Children[cIndex].SetValue(Canvas.LeftProperty, (double)mapSection.CanvasPosition.X);
				MainCanvas.Children[cIndex].SetValue(Canvas.BottomProperty, (double)mapSection.CanvasPosition.Y);
				MainCanvas.Children[cIndex].SetValue(Panel.ZIndexProperty, 0);
			}

			return screenSection;
		}

		private Image CreateImage(SizeInt size)
		{
			var result = new Image
			{
				Width = size.Width,
				Height = size.Height,
				Stretch = Stretch.None,
				Margin = new Thickness(0),
				//Source = GetBitMap(mapSection, mapSection.Size)
				Source = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgra32, null)
		};

			return result;
		}

		//private WriteableBitmap GetBitMap(MapSection mapSection, SizeInt blockSize)
		//{
		//	var width = blockSize.Width;
		//	var height = blockSize.Height;

		//	var result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

		//	var rect = new Int32Rect(0, 0, width, height);
		//	var stride = 4 * width;
		//	result.WritePixels(rect, mapSection.Pixels1d, stride, 0);

		//	return result;
  //      }

		private void SetupSelectionRect()
		{
			_selectedArea = new Rectangle()
			{
				Width = _vm.BlockSize.Width,
				Height = _vm.BlockSize.Height,
				Stroke = Brushes.Black,
				StrokeThickness = 2,
				Visibility = Visibility.Hidden
			};

			_ = MainCanvas.Children.Add(_selectedArea);
			_selectedArea.SetValue(Panel.ZIndexProperty, 10);

			MoveSelectionRect(new PointInt(0, 0));
		}

		private void MoveSelectionRect(PointInt position)
		{
			Debug.WriteLine($"Moving the sel rec to {position}");

			_selectedArea.SetValue(Canvas.LeftProperty, (double) position.X);
			_selectedArea.SetValue(Canvas.BottomProperty, (double) position.Y);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void MainCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var position = e.GetPosition(relativeTo: MainCanvas);
			var blockPosition = _vm.GetBlockPosition(position);

			if (_selectedArea.Visibility == Visibility.Hidden)
			{
				MoveSelectionRect(blockPosition);
				_selectedArea.Visibility = Visibility.Visible;
			}
			else
			{
				if(IsSelectedRectHere(blockPosition))
				{
					_selectedArea.Visibility = Visibility.Hidden;
				}
				else
				{
					MoveSelectionRect(blockPosition);
				}
			}

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

		private bool IsSelectedRectHere(PointInt position)
		{
			var rPosX = (int)Math.Round((double)_selectedArea.GetValue(Canvas.LeftProperty));
			var rPosY = (int)Math.Round((double)_selectedArea.GetValue(Canvas.BottomProperty));

			var rOffset = new SizeInt(rPosX, rPosY);

			Debug.WriteLine($"Comparing {position} to {rOffset}");

			var diff = position.Diff(rOffset).Abs();
			var result = diff.X < 2 && diff.Y < 2;

			return result;
		}

		private void MainCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
		}

		private void MainCanvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (!(_selectedArea is null))
			{
				_selectedArea.StrokeThickness = 2;
			}
		}

		private void MainCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (!(_selectedArea is null))
			{
				_selectedArea.StrokeThickness = 0;
			}
		}


		class ScreenSection
		{
			public Image Image { get; init; }

			public Histogram Histogram { get; init; }

			public ScreenSection(Image image) : this(image, new Histogram())
			{ }

			public ScreenSection(Image image, Histogram histogram)
			{
				Image = image;
				Histogram = histogram;
			}

		}
	}


}
