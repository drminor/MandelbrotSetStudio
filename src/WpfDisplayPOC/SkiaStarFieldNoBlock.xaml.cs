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
    /// Interaction logic for SkiaStarFieldNoBlock.xaml
    /// </summary>
    public partial class SkiaStarFieldNoBlock : Window
    {
		static bool _stopping = false;
		//static Color starColor = Color.White;

		private static readonly Field _field = new Field(500);

		private static MSectionDispControl? _sectionDispControl;

		private static object _object = new object();

		public SkiaStarFieldNoBlock()
		{
			InitializeComponent();

			_sectionDispControl = MSectionDispControl1;

			Closing += SkiaStarFieldNoBlock_Closing;

			//var renderThread = new Thread(new ThreadStart(RenderForever));
			//renderThread.Start();

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(40);
			timer.Tick += timer1_Tick;
			timer.Start();

			DispatcherTimer timer2 = new DispatcherTimer();
			timer2.Interval = TimeSpan.FromMilliseconds(40);
			timer2.Tick += timer2_Tick;
			timer2.Start();
		}

		private void SkiaStarFieldNoBlock_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
		{
			_stopping = true;
		}

		//Stopwatch stopwatch = new Stopwatch();

		private void timer1_Tick(object? sender, EventArgs e)
		{
			MSectionDispControl1.InvalidateVisual();
		}


		private void timer2_Tick(object? sender, EventArgs e)
		{
			//UpdateBuffer();
			DoStars();
		}


		private void UpdateBuffer()
		{
			ThreadPool.QueueUserWorkItem(
				o =>
				{
					// write data to buffer...
					Dispatcher.BeginInvoke((Action)(() =>
					{
						if (_sectionDispControl == null)
						{
							return;
						}

						double maxFPS = 100;
						//double maxFPS = 10;

						double minFramePeriodMsec = 1000.0 / maxFPS;

						Stopwatch stopwatch = Stopwatch.StartNew();

						if (!_stopping)
						{
							// advance the model
							_field.Advance();

							//bmpLive = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight);

							//bmpLive.Dispose();
							//bmpLive = new Bitmap(780,650);

							//byte alpha = (byte)(mySlider.Value * 255 / 100);
							byte alpha = 250;
							var starColor = Color.FromArgb(alpha, 255, 255, 255);


							// Lock and update the "display" Bitmap
							lock (_object)
							{
								_sectionDispControl.RenderStars(_field.GetStars());
							}

							// FPS limiter
							double msToWait = minFramePeriodMsec - stopwatch.ElapsedMilliseconds;

							if (msToWait > 0)
							{
								Thread.Sleep((int)msToWait);
							}

							stopwatch.Restart();
						}


					}));
				});

			MSectionDispControl1.InvalidateVisual();

		}

		private void DoStars()
		{
			if (_sectionDispControl == null)
			{
				return;
			}

			double maxFPS = 100;
			//double maxFPS = 10;

			double minFramePeriodMsec = 1000.0 / maxFPS;

			Stopwatch stopwatch = Stopwatch.StartNew();

			if (!_stopping)
			{
				// advance the model
				_field.Advance();

				//bmpLive = new Bitmap((int)myCanvas.ActualWidth, (int)myCanvas.ActualHeight);

				//bmpLive.Dispose();
				//bmpLive = new Bitmap(780,650);

				//byte alpha = (byte)(mySlider.Value * 255 / 100);
				byte alpha = 250;
				var starColor = Color.FromArgb(alpha, 255, 255, 255);


				// Lock and update the "display" Bitmap
				lock (_object)
				{
					_sectionDispControl.RenderStars(_field.GetStars());
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

		private static void RenderForever()
		{
			if (_sectionDispControl == null)
			{
				return;
			}

			double maxFPS = 100;
			//double maxFPS = 10;

			double minFramePeriodMsec = 1000.0 / maxFPS;

			Stopwatch stopwatch = Stopwatch.StartNew();

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


				// Lock and update the "display" Bitmap
				lock (_object)
				{
					_sectionDispControl.RenderStars(_field.GetStars());
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
