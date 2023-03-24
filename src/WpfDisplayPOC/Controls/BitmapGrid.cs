using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace WpfDisplayPOC
{
	/// <summary>
	/// 2D collection of Bitmap blocks
	/// </summary>
	public class BitmapGrid : FrameworkElement
	{
		private WriteableBitmap? _bitmap;
		private SKColor _canvasClearColor;

		#region Constructor

		public BitmapGrid()
		{
			_canvasClearColor = SKColor.Parse(new SolidColorBrush(Colors.AntiqueWhite).ToString());
			
			_bitmap = CreateBitmap();

			SizeChanged += BitmapGrid_SizeChanged;
		}

		private void BitmapGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_bitmap = CreateBitmap();
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Capture the most recent control render to an image
		/// </summary>
		/// <returns>An <see cref="ImageSource"/> containing the captured area</returns>
		//[CanBeNull]
		public BitmapSource? SnapshotToBitmapSource() => _bitmap?.Clone();

		#endregion

		#region Public Methods

		public void RenderStars(Field.Star[] stars)
		{
			if (_bitmap == null)
			{
				throw new InvalidOperationException("Cannot Render Starts before the BitmapGrid is initialized.");
			}

			_bitmap.Lock();

			var w = (int)_bitmap.Width;
			var h = (int)_bitmap.Height;

			var imgInfo = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);

			using (var surface = SKSurface.Create(imgInfo, _bitmap.BackBuffer, _bitmap.BackBufferStride))
			{
				surface.Canvas.Clear(_canvasClearColor);

				var starColor = new SKColor(255, 60, 90, 255);
				var starPaint = new SKPaint() { IsAntialias = true, Color = starColor };

				foreach (var star in stars)
				{
					float xPixel = (float)star.x * w;
					float yPixel = (float)star.y * h;
					float radius = (float)star.size - 1;
					var point = new SKPoint(xPixel, yPixel);
					surface.Canvas.DrawCircle(point, radius, starPaint);
				}
			}

			_bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)_bitmap.Width, (int)_bitmap.Height));
			_bitmap.Unlock();
		}

		public void ClearBitmap()
		{
			if (_bitmap == null)
			{
				throw new InvalidOperationException("Cannot Place a Bitmap before the BitmapGrid is initialized.");
			}

			_bitmap.Lock();

			var imgInfo = new SKImageInfo((int)_bitmap.Width, (int)_bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

			using (var surface = SKSurface.Create(imgInfo, _bitmap.BackBuffer, _bitmap.BackBufferStride))
			{
				surface.Canvas.Clear(_canvasClearColor);
			}

			_bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)_bitmap.Width, (int)_bitmap.Height));
			_bitmap.Unlock();
		}

		public void PlaceBitmap(SKBitmap sKBitmap, SKPoint sKPoint, bool clearCanvas)
		{
			if (_bitmap == null)
			{
				throw new InvalidOperationException("Cannot Place a Bitmap before the BitmapGrid is initialized.");
			}

			_bitmap.Lock();

			var imgInfo = new SKImageInfo((int)_bitmap.Width, (int)_bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

			using (var surface = SKSurface.Create(imgInfo, _bitmap.BackBuffer, _bitmap.BackBufferStride))
			{
				if (clearCanvas)
				{
					surface.Canvas.Clear(_canvasClearColor);
				}

				surface.Canvas.DrawBitmap(sKBitmap, sKPoint);
			}

			_bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)_bitmap.Width, (int)_bitmap.Height));
			_bitmap.Unlock();
		}

		#endregion

		#region Private and Protected Methods

		protected override void OnRender(DrawingContext dc)
		{
			if (_bitmap == null)
				return;

			dc.DrawImage(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
		}

		private WriteableBitmap? CreateBitmap()
		{
			WriteableBitmap? result;

			int width = (int)ActualWidth;
			int height = (int)ActualHeight;

			if (height > 0 && width > 0 && Parent != null)
			{
				result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
			}
			else
			{
				result = null;
			}

			return result;
		}

		#endregion
	}
}
