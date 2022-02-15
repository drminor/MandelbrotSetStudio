using MSS.Types;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	public class ScreenSection
	{
		public Image Image { get; init; }
		public Canvas Canvas { get; init; }
		//public int ChildIndex { get; init; }

		public ScreenSection(Canvas canvas, SizeInt size)
		{
			Image = CreateImage(size.Width, size.Height);
			//ChildIndex = canvas.Children.Add(Image);
			_ = canvas.Children.Add(Image);
			Image.SetValue(Panel.ZIndexProperty, 0);
		}

		public void Place(PointInt position)
		{
			Image.SetValue(Canvas.LeftProperty, (double)position.X);
			Image.SetValue(Canvas.BottomProperty, (double)position.Y);
		}

		public void WritePixels(byte[] pixels)
		{
			var bitmap = (WriteableBitmap)Image.Source;

			var w = (int)Math.Round(Image.Width);
			var h = (int)Math.Round(Image.Height);

			var rect = new Int32Rect(0, 0, w, h);
			var stride = 4 * w;
			bitmap.WritePixels(rect, pixels, stride, 0);

			Image.Visibility = Visibility.Visible;
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
