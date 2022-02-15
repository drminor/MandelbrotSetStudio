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
		private readonly Image _image;

		public ScreenSection2(Canvas canvas, SizeInt size)
		{
			_image = CreateImage(size.Width, size.Height);
			_ = canvas.Children.Add(_image);
			_image.SetValue(Panel.ZIndexProperty, 0);
		}

		public void Place(PointInt position)
		{
			_image.SetValue(Canvas.LeftProperty, (double)position.X);
			_image.SetValue(Canvas.BottomProperty, (double)position.Y);
		}

		public void WritePixels(byte[] pixels)
		{
			var bitmap = (WriteableBitmap)_image.Source;

			var w = (int)Math.Round(_image.Width);
			var h = (int)Math.Round(_image.Height);

			var rect = new Int32Rect(0, 0, w, h);
			var stride = 4 * w;
			bitmap.WritePixels(rect, pixels, stride, 0);

			_image.Visibility = Visibility.Visible;
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
