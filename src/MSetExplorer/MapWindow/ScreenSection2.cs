using MSS.Types;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class ScreenSection2 : IScreenSection
	{
		public ImageDrawing ImageDrawing { get; }

		public ScreenSection2(RectangleInt rectangle)
		{
			var image = CreateImage(rectangle.Width, rectangle.Height);
			var rect = new Rect(new Point(rectangle.X1, rectangle.Y1), new Size(rectangle.Width, rectangle.Height));
			ImageDrawing = new ImageDrawing(image.Source, rect);
		}

		public void Place(PointInt position)
		{
			throw new NotImplementedException();
		}

		public void WritePixels(byte[] pixels)
		{
			var bitmap = (WriteableBitmap) ImageDrawing.ImageSource;

			var w = (int)Math.Round(bitmap.Width);
			var h = (int)Math.Round(bitmap.Height);

			var rect = new Int32Rect(0, 0, w, h);
			var stride = 4 * w;
			bitmap.WritePixels(rect, pixels, stride, 0);
		}

		private Image CreateImage(int w, int h)
		{
			var result = new Image
			{
				Width = w,
				Height = h,
				Stretch = Stretch.None,
				Margin = new Thickness(0),
				Source = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null)
			};

			return result;
		}

	}
}
