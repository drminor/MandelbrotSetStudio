using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WpfDisplayPOC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void myCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			Render();
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Render();
		}

		private void myCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Render();
		}

		Random rand = new Random();

		private void Render()
		{
			using (var bmp = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight))
			using (var gfx = Graphics.FromImage(bmp))
			using (var pen = new Pen(Color.White))
			{
				// draw one thousand random white lines on a dark blue background
				gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				gfx.Clear(Color.Navy);
				for (int i = 0; i < 1000; i++)
				{
					var pt1 = new System.Drawing.Point(rand.Next(bmp.Width), rand.Next(bmp.Height));
					var pt2 = new System.Drawing.Point(rand.Next(bmp.Width), rand.Next(bmp.Height));
					gfx.DrawLine(pen, pt1, pt2);
				}

				// copy the bitmap to the picturebox
				myImage.Source = BmpImageFromBmp(bmp);
			}
		}

		private static BitmapImage BmpImageFromBmp(Bitmap bmp)
		{
			using (var memory = new System.IO.MemoryStream())
			{
				bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
				memory.Position = 0;

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();

				return bitmapImage;
			}
		}
	}
}
