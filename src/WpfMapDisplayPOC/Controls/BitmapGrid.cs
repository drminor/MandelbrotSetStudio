using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfMapDisplayPOC
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
			_canvasClearColor = SKColor.Parse(new SolidColorBrush(Colors.Transparent).ToString());
			
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

		//public BitmapSource? BitmapSource { get; set; }

		#endregion

		#region Public Methods

		public void ClearCanvas()
		{
			if (_bitmap == null)
			{
				throw new InvalidOperationException("Cannot Place a Bitmap before the BitmapGrid is initialized.");
			}

			_bitmap.Lock();

			var imgInfo = new SKImageInfo((int)_bitmap.Width, (int)_bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

			using (var surface = SKSurface.Create(imgInfo, _bitmap.BackBuffer, _bitmap.BackBufferStride))
			{
				surface.Canvas.Clear(_canvasClearColor);
			}

			_bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)_bitmap.Width, (int)_bitmap.Height));
			_bitmap.Unlock();
		}

		public void PlaceBitmap(SKBitmap sKBitmap, SKPoint sKPoint)
		{
			if (_bitmap == null)
			{
				throw new InvalidOperationException("Cannot Place a Bitmap before the BitmapGrid is initialized.");
			}

			_bitmap.Lock();

			var imgInfo = new SKImageInfo((int)_bitmap.Width, (int)_bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
			var paint = new SKPaint();
			paint.BlendMode = SKBlendMode.Multiply;

			using (var surface = SKSurface.Create(imgInfo, _bitmap.BackBuffer, _bitmap.BackBufferStride))
			{
				surface.Canvas.DrawBitmap(sKBitmap, sKPoint, paint);
			}

			_bitmap.AddDirtyRect(new Int32Rect((int)sKPoint.X, (int)sKPoint.Y, sKBitmap.Width, sKBitmap.Height));
			_bitmap.Unlock();
		}

		#endregion

		#region Private and Protected Methods

		protected override void OnRender(DrawingContext dc)
		{
			if (_bitmap != null)
			{
				dc.DrawImage(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
			}

			//if (BitmapSource != null)
			//{
			//	dc.DrawImage(BitmapSource, new Rect(0, 0, BitmapSource.Width, BitmapSource.Height));
			//}
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
