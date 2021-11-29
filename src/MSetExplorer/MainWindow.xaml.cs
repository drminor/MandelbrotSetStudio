using MEngineDataContracts;
using MSS.Types.MSet;
using System;
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

            var progress = new Progress<MapSectionResponse>(HandleMapSectionReady);

            var mSetInfo = _vm.BuildInitialMSetInfo();
            _vm.GenerateMapSections(mSetInfo, progress);
		}

		private void HandleMapSectionReady(MapSectionResponse mapSectionResponse)
		{
			//var x = MainCanvas.Children[0];

			//if (x is Image f)
			//{
			//}

			var image = new Image
			{
				Width = 240,
				Height = 240,
				Stretch = Stretch.None,
				Margin = new Thickness(0),
				Source = GetBitMap()
			};

			MainCanvas.Children.Add(image);
		}

		private WriteableBitmap GetBitMap()
		{
            const int width = 240;
            const int height = 240;

            var wbitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            byte[,,] pixels = new byte[height, width, 4];

            // Clear to black.
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    for (int i = 0; i < 3; i++)
                        pixels[row, col, i] = 0;
                    pixels[row, col, 3] = 255;
                }
            }

            // Blue.
            for (int row = 0; row < 80; row++)
            {
                for (int col = 0; col <= row; col++)
                {
                    pixels[row, col, 0] = 255;
                }
            }

            // Green.
            for (int row = 80; row < 160; row++)
            {
                for (int col = 0; col < 80; col++)
                {
                    pixels[row, col, 1] = 255;
                }
            }

            // Red.
            for (int row = 160; row < 240; row++)
            {
                for (int col = 0; col < 80; col++)
                {
                    pixels[row, col, 2] = 255;
                }
            }

            // Copy the data into a one-dimensional array.
            byte[] pixels1d = new byte[height * width * 4];
            int index = 0;
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    for (int i = 0; i < 4; i++)
                        pixels1d[index++] = pixels[row, col, i];
                }
            }

            // Update writeable bitmap with the colorArray to the image.
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            int stride = 4 * width;
            wbitmap.WritePixels(rect, pixels1d, stride, 0);

            return wbitmap;
        }

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
