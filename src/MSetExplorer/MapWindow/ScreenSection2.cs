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
		//private readonly Image _image;

		public ScreenSection2(PointInt position, SizeInt size)
		{
			var image = CreateImage(size.Width, size.Height);
			var screenPosition = position.Scale(size);
			ImageDrawing = new ImageDrawing(image.Source, new Rect(screenPosition.X, screenPosition.Y, size.Width, size.Height));
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
