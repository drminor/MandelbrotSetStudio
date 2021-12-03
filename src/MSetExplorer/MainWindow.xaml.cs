using MEngineDataContracts;
using MSS.Types.MSet;
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
            _vm.CreateJob(mSetInfo, progress);
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			var image = new Image
			{
				Width = _vm.Subdivision.BlockSize.Width,
				Height = _vm.Subdivision.BlockSize.Height,
				Stretch = Stretch.None,
				Margin = new Thickness(0),
				Source = GetBitMap(mapSection, _vm.Subdivision)
		};

			var cIndex = MainCanvas.Children.Add(image);

            var left = (double)mapSection.BlockPosition.X * _vm.Subdivision.BlockSize.Width;
            var bot = (double)mapSection.BlockPosition.Y * _vm.Subdivision.BlockSize.Height;

			Debug.WriteLine($"Drawing a bit map at X: {left}, Y:{bot}.");

            MainCanvas.Children[cIndex].SetValue(Canvas.LeftProperty, left);
            MainCanvas.Children[cIndex].SetValue(Canvas.BottomProperty, bot);
        }

        private WriteableBitmap GetBitMap(MapSection mapSection, Subdivision subdivision)
		{
			var width = subdivision.BlockSize.Width;
			var height = subdivision.BlockSize.Height;

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
