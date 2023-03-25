using MSS.Types;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfDisplayPOC
{
    /// <summary>
    /// Interaction logic for SkiaStarFieldNoBlock.xaml
    /// </summary>
    public partial class SkiaStarFieldNoBlock : Window
    {
		static bool _stopping = false;
		//static Color starColor = Color.White;

		private static readonly Field _field = new Field(500);
		private readonly double _minFramePeriodMsec;


		private static Bitmap _bmpLive = new Bitmap(10, 10);
		private static Bitmap _bmpLast = new Bitmap(10, 10);

		private static MSectionDispControl? _sectionDispControl;

		private Stopwatch _stopwatch;


		public SkiaStarFieldNoBlock()
		{
			InitializeComponent();

			double maxFPS = 40;
			_minFramePeriodMsec = 1000.0 / maxFPS;

			_stopwatch = new Stopwatch();	

			_sectionDispControl = MSectionDispControl1;

			Closing += SkiaStarFieldNoBlock_Closing;


			var renderThread = new Thread(new ThreadStart(RenderForever));
			renderThread.Start();

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(40);
			timer.Tick += timer1_Tick;
			timer.Start();
		}

		private void SkiaStarFieldNoBlock_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			_stopping = true;
		}


		private void timer1_Tick(object? sender, EventArgs e)
		{
			//lock (_bmpLast)
			//{
			//	var x = BmpImageFromBmp((Bitmap)_bmpLast.Clone());

			//	MSectionDispControl1.BitmapSource = x;
			//	MSectionDispControl1.InvalidateVisual();
			//}

			_stopwatch.Restart();

			lock (_bmpLast)
			{
				//var x = BmpImageFromBmp((Bitmap)_bmpLast.Clone());
				var y = ToBitmapSource((Bitmap)_bmpLast.Clone());

				MSectionDispControl1.BitmapSource = y;
				//MSectionDispControl1.InvalidateVisual();
			}

			// FPS limiter
			double msToWait = _minFramePeriodMsec - _stopwatch.ElapsedMilliseconds;

			if (msToWait > 0)
			{
				Thread.Sleep((int)msToWait);
			}

			double elapsedSec = (double)_stopwatch.ElapsedTicks / Stopwatch.Frequency;
			Title = $"Starfield in WPF - {elapsedSec * 1000:0.00} ms ({1 / elapsedSec:0.00} FPS)";
		}

		private static void RenderForever()
		{
			double maxFPS = 40;
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
				var starColor = System.Drawing.Color.FromArgb(alpha, 255, 255, 255);


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

		//private static void RenderForeverOld()
		//{
		//	if (_sectionDispControl == null)
		//	{
		//		return;
		//	}

		//	double maxFPS = 100;
		//	//double maxFPS = 10;

		//	double minFramePeriodMsec = 1000.0 / maxFPS;

		//	Stopwatch stopwatch = Stopwatch.StartNew();

		//	while (!_stopping)
		//	{
		//		// advance the model
		//		_field.Advance();

		//		//bmpLive = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight);

		//		//bmpLive.Dispose();
		//		//bmpLive = new Bitmap(780,650);

		//		//byte alpha = (byte)(mySlider.Value * 255 / 100);
		//		//byte alpha = 250;
		//		//var starColor = Color.FromArgb(alpha, 255, 255, 255);

		//		_bmpLive = RenderStars(_field.GetStars(), new SizeInt(512));

		//		var x = BmpImageFromBmp(_bmpLive);

		//		Debug.WriteLine($"W = {x.Width}");

		//		// Lock and update the "display" Bitmap
		//		lock (_bmpLast)
		//		{
		//			_bmpLast.Dispose();
		//			_bmpLast = (Bitmap)_bmpLive.Clone();
		//		}

		//		// FPS limiter
		//		double msToWait = minFramePeriodMsec - stopwatch.ElapsedMilliseconds;

		//		if (msToWait > 0)
		//		{
		//			Thread.Sleep((int)msToWait);
		//		}

		//		stopwatch.Restart();
		//	}
		//}

		//private static Bitmap RenderStars(Field.Star[] stars, SizeInt size)
		//{
		//	var bitmap = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);

		//	var w = (int)bitmap.Width;
		//	var h = (int)bitmap.Height;

		//	bitmap.Lock();

		//	var imgInfo = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
		//	using (var surface = SKSurface.Create(imgInfo, bitmap.BackBuffer, bitmap.BackBufferStride))
		//	{
		//		var canvasClearColor = SKColor.Parse(Colors.AntiqueWhite.ToString());
		//		surface.Canvas.Clear(canvasClearColor);

		//		var starColor = new SKColor(255, 60, 90, 255);
		//		var starPaint = new SKPaint() { IsAntialias = true, Color = starColor };

		//		foreach (var star in stars)
		//		{
		//			float xPixel = (float)star.x * w;
		//			float yPixel = (float)star.y * h;
		//			float radius = (float)star.size - 1;
		//			var point = new SKPoint(xPixel, yPixel);
		//			surface.Canvas.DrawCircle(point, radius, starPaint);
		//		}
		//	}

		//	bitmap.AddDirtyRect(new Int32Rect(0, 0, w, h));
		//	bitmap.Unlock();

		//	var result = BitmapFromWriteableBitmap(bitmap);

		//	return result;
		//}

		private static BitmapImage BmpImageFromBmp(Bitmap bmp)
		{
			using (var memory = new MemoryStream())
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

		public static BitmapSource ToBitmapSource(Bitmap bmp)
		{
			var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
			int bufferSize = bmpData.Stride * bmp.Height;
			var bms = new WriteableBitmap(bmp.Width, bmp.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Bgr32, null);
			bms.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bmpData.Scan0, bufferSize, bmpData.Stride);
			bmp.UnlockBits(bmpData);

			return bms;
		}


		//private static Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
		//{
		//	Bitmap bmp;
		//	using (MemoryStream outStream = new MemoryStream())
		//	{
		//		BitmapEncoder enc = new BmpBitmapEncoder();
		//		enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
		//		enc.Save(outStream);
		//		bmp = new Bitmap(outStream);
		//	}

		//	return bmp;
		//}

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
