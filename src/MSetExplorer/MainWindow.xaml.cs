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

            var progress = new Progress<MapSection>(HandleMapSectionReady);

            var mSetInfo = MSetInfoHelper.BuildInitialMSetInfo();
            _vm.LoadMap(mSetInfo, progress);
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			var image = BuildImage(mapSection);
			Debug.WriteLine($"Drawing a bit map at {mapSection.CanvasPosition}.");

			var cIndex = MainCanvas.Children.Add(image);
            MainCanvas.Children[cIndex].SetValue(Canvas.LeftProperty, mapSection.CanvasPosition.X);
            MainCanvas.Children[cIndex].SetValue(Canvas.BottomProperty, mapSection.CanvasPosition.Y);
        }

		private Image BuildImage(MapSection mapSection)
		{
			var result = new Image
			{
				Width = mapSection.Subdivision.BlockSize.Width,
				Height = mapSection.Subdivision.BlockSize.Height,
				Stretch = Stretch.None,
				Margin = new Thickness(0),
				Source = GetBitMap(mapSection, mapSection.Subdivision.BlockSize)
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
