using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfDisplayPOC
{
	/// <summary>
	/// Interaction logic for NoBlockStarfield.xaml
	/// </summary>
	public partial class NoBlockStarfield : Window
	{
		static bool _stopping = false;
		//static Color starColor = Color.White;

		private static readonly Field _field = new Field(500);

		private static Bitmap _bmpLive = new Bitmap(10, 10);
		private static Bitmap _bmpLast = new Bitmap(10, 10);

		public NoBlockStarfield()
		{
			InitializeComponent();

			//bmpLive = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight);
			//bmpLast = (Bitmap)bmpLive.Clone();

			Closing += NoBlockStarfield_Closing;

			var renderThread = new Thread(new ThreadStart(RenderForever));
			renderThread.Start();


			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(20);
			timer.Tick += timer1_Tick;
			timer.Start();
		}

		private void NoBlockStarfield_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			_stopping = true;
		}

		//Stopwatch stopwatch = new Stopwatch();

		private void timer1_Tick(object? sender, EventArgs e)
		{
			lock (_bmpLast)
			{
				//pictureBox1.Image?.Dispose();
				//pictureBox1.Image = (Bitmap)bmpLast.Clone();

				//var oldBitmapImage = myImage.Source as BitmapImage;

				myImage.Source = BmpImageFromBmp((Bitmap)_bmpLast.Clone());
			}
		}

		private static void RenderForever()
		{
			double maxFPS = 100;
			//double maxFPS = 10;

			double minFramePeriodMsec = 1000.0 / maxFPS;

			Stopwatch stopwatch = Stopwatch.StartNew();

			_bmpLive.Dispose();
			_bmpLive = new Bitmap(780, 650);

			while (!_stopping)
			{
				// advance the model
				_field.Advance();

				//bmpLive = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight);

				//bmpLive.Dispose();
				//bmpLive = new Bitmap(780,650);

				//byte alpha = (byte)(mySlider.Value * 255 / 100);
				byte alpha = 250;
				var starColor = Color.FromArgb(alpha, 255, 255, 255);


				// Render on the "live" Bitmap
				_field.Render(_bmpLive, starColor);

				// Lock and update the "display" Bitmap
				lock (_bmpLast)
				{
					_bmpLast.Dispose();
					_bmpLast = (Bitmap)_bmpLive.Clone();
				}

				// FPS limiter
				double msToWait = minFramePeriodMsec - stopwatch.ElapsedMilliseconds;

				if (msToWait > 0)
				{
					Thread.Sleep((int)msToWait);
				}

				stopwatch.Restart();
			}
		}

		//void timer_Tick(object? sender, EventArgs e)
		//{
		//	stopwatch.Restart();
		//	field.Advance();
		//	Bitmap bmp = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight);

		//	byte alpha = (byte)(mySlider.Value * 255 / 100);
		//	var starColor = Color.FromArgb(alpha, 255, 255, 255);

		//	field.Render(bmp, starColor);
		//	myImage.Source = BmpImageFromBmp(bmp);

		//	double elapsedSec = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;
		//	Title = $"Starfield in WPF - {elapsedSec * 1000:0.00} ms ({1 / elapsedSec:0.00} FPS)";
		//}

		private BitmapImage BmpImageFromBmp(Bitmap bmp)
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

		private void Set500Stars(object sender, RoutedEventArgs e)
		{
			_field.Reset(500);
		}

		private void Set100kStars(object sender, RoutedEventArgs e)
		{
			_field.Reset(100_000);
		}

	}
}
