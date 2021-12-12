using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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

		public MainWindow()
		{
			Loaded += MainWindow_Loaded;
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_vm = (MainWindowViewModel)DataContext;

			var canvasSize = GetCanvasControlSize(MainCanvas);
			var mSetInfo = MSetInfoHelper.BuildInitialMSetInfo();
			var progress = new Progress<MapSection>(HandleMapSectionReady);
			
			_vm.LoadMap(canvasSize, mSetInfo, progress);
		}

		private SizeInt GetCanvasControlSize(Canvas canvas)
		{
			var width = (int) Math.Round(canvas.Width);
			var height = (int)Math.Round(canvas.Height);
			return new SizeInt(width, height);
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			var image = BuildImage(mapSection);
			Debug.WriteLine($"Drawing a bit map at {mapSection.CanvasPosition}.");

			var cIndex = MainCanvas.Children.Add(image);
            MainCanvas.Children[cIndex].SetValue(Canvas.LeftProperty, (double) mapSection.CanvasPosition.X);
            MainCanvas.Children[cIndex].SetValue(Canvas.BottomProperty, (double) mapSection.CanvasPosition.Y);
        }

		private Image BuildImage(MapSection mapSection)
		{
			var result = new Image
			{
				Width = mapSection.Size.Width,
				Height = mapSection.Size.Height,
				Stretch = Stretch.None,
				Margin = new Thickness(0),
				Source = GetBitMap(mapSection, mapSection.Size)
			};

			return result;
		}

		private WriteableBitmap GetBitMap(MapSection mapSection, SizeInt blockSize)
		{
			var width = blockSize.Width;
			var height = blockSize.Height;

			var result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

			var rect = new Int32Rect(0, 0, width, height);
			var stride = 4 * width;
			result.WritePixels(rect, mapSection.Pixels1d, stride, 0);

			return result;
        }

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
