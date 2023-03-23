using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfDisplayPOC
{
	/// <summary>
	/// Interaction logic for SkiaStarField.xaml
	/// </summary>
	public partial class SkiaStarField : Window
	{

		//private readonly Field _field = new Field(500);
		private readonly double _minFramePeriodMsec;

		public SkiaStarField()
		{
			InitializeComponent();

			double maxFPS = 40;

			_minFramePeriodMsec = 1000.0 / maxFPS;

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(20);
			timer.Tick += timer_Tick;
			timer.Start();
		}

		Stopwatch stopwatch = Stopwatch.StartNew();

		void timer_Tick(object? sender, EventArgs e)
		{
			//stopwatch.Restart();
			//_field.Advance();
			//Bitmap bmp = new Bitmap((int)mySkiaCanvas.ActualWidth, (int)mySkiaCanvas.ActualHeight);
			//byte alpha = (byte)(mySlider.Value * 255 / 100);
			//var starColor = Color.FromArgb(alpha, 255, 255, 255);
			//_field.Render(bmp, starColor);
			////myImage.Source = BmpImageFromBmp(bmp);

			//// FPS limiter
			//double msToWait = _minFramePeriodMsec - stopwatch.ElapsedMilliseconds;

			//if (msToWait > 0)
			//{
			//	Thread.Sleep((int)msToWait);
			//}

			//double elapsedSec = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;
			//Title = $"Starfield in WPF - {elapsedSec * 1000:0.00} ms ({1 / elapsedSec:0.00} FPS)";

			mySkiaCanvas.InvalidateVisual();
		}

		//private BitmapImage BmpImageFromBmp(Bitmap bmp)
		//{
		//	using (var memory = new System.IO.MemoryStream())
		//	{
		//		bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
		//		memory.Position = 0;

		//		var bitmapImage = new BitmapImage();
		//		bitmapImage.BeginInit();
		//		bitmapImage.StreamSource = memory;
		//		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		//		bitmapImage.EndInit();
		//		bitmapImage.Freeze();

		//		return bitmapImage;
		//	}
		//}

		private void Set500Stars(object sender, RoutedEventArgs e)
		{
			//_field.Reset(500);
		}

		private void Set100kStars(object sender, RoutedEventArgs e)
		{
			//_field.Reset(100_000);
		}



	}
}
