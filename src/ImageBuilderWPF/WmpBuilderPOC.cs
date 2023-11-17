using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace ImageBuilderWPF
{
	public class WmpBuilderPOC
	{
		public void Test1(string filePath)
		{
			int width = 128;
			int height = width;
			int stride = width / 8;
			byte[] pixels = new byte[height * stride];

			// Try creating a new image with a custom palette.
			List<Color> colors = new List<Color>();
			colors.Add(Colors.Red);
			colors.Add(Colors.Blue);
			colors.Add(Colors.Green);
			BitmapPalette myPalette = new BitmapPalette(colors);

			// Creates a new empty image with the pre-defined palette

			BitmapSource image = BitmapSource.Create(
				width,
				height,
				96,
				96,
				PixelFormats.Indexed1,
				myPalette,
				pixels,
				stride);

			FileStream stream = new FileStream(filePath, FileMode.Create);

			TiffBitmapEncoder encoder = new TiffBitmapEncoder();

			TextBlock myTextBlock = new TextBlock();
			myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();

			encoder.Frames.Add(BitmapFrame.Create(image));

			MessageBox.Show(myPalette.Colors.Count.ToString());

			encoder.Save(stream);
		}

		public void Wmp8(string filePath)
		{
			const int width = 256;
			const int height = 256;
			const int bytesPerPixel = 3;
			var stride = width * bytesPerPixel;

			var imageData = new byte[width * height * bytesPerPixel];

			// create a RGB gradient image
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					imageData[(y * width + x) * bytesPerPixel + 0] = (byte)x;           // blue
					imageData[(y * width + x) * bytesPerPixel + 1] = (byte)y;           // green
					imageData[(y * width + x) * bytesPerPixel + 2] = (byte)(255 - y);   // red
				}
			}

			var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);

			var destArea = new Int32Rect(0, 0, width, height);
			writeableBitmap.WritePixels(destArea, imageData, stride, 0);

			var bitmapFrame = BitmapFrame.Create(writeableBitmap);

			FileStream stream = new FileStream(filePath, FileMode.Create);
			var encoder = new WmpBitmapEncoder();
			encoder.ImageQualityLevel = 1.0f;

			encoder.Frames.Add(bitmapFrame);

			encoder.Save(stream);
		}

		public void Wmp16(string filePath)
		{
			const int width = 256;
			const int height = 256;
			const int samplesPerPixel = 3;
			var stride = width * samplesPerPixel;

			var imageData = new ushort[width * height * samplesPerPixel];

			// create a RGB gradient image
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					imageData[(y * width + x) * samplesPerPixel + 0] = (ushort)(65535 - (y * 256));   // red
					imageData[(y * width + x) * samplesPerPixel + 1] = (ushort)(y * 256);           // green
					imageData[(y * width + x) * samplesPerPixel + 2] = (ushort)(x * 256);           // blue
				}
			}

			var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb48, null);

			var destArea = new Int32Rect(0, 0, width, height);
			writeableBitmap.WritePixels(destArea, imageData, stride * 2, 0);

			var bitmapFrame = BitmapFrame.Create(writeableBitmap);

			FileStream stream = new FileStream(filePath, FileMode.Create);
			var encoder = new WmpBitmapEncoder();
			encoder.ImageQualityLevel = 1.0f;

			encoder.Frames.Add(bitmapFrame);

			encoder.Save(stream);
		}

		public void Wmp16Huge(string filePath)
		{
			const int width = 16384;
			const int height = 16384;
			const long samplesPerPixel = 3;
			var stride = (int)(width * samplesPerPixel);

			long len = height * (long)width * samplesPerPixel;

			//var imageData = Array.CreateInstance(typeof(ushort), new long[] { len});

			var imageData = new ushort[len];

			// create a RGB gradient image
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					imageData[(y * width + x) * samplesPerPixel + 0] = (ushort)(65535 - (y * 4));   // red
					imageData[(y * width + x) * samplesPerPixel + 1] = (ushort)(y * 4);           // green
					imageData[(y * width + x) * samplesPerPixel + 2] = (ushort)(x * 4);           // blue
				}
			}

			var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb48, null);

			var destArea = new Int32Rect(0, 0, width, height);
			writeableBitmap.WritePixels(destArea, imageData, stride * 2, 0);

			var bitmapFrame = BitmapFrame.Create(writeableBitmap);

			FileStream stream = new FileStream(filePath, FileMode.Create);
			var encoder = new WmpBitmapEncoder();
			encoder.ImageQualityLevel = 1.0f;

			encoder.Frames.Add(bitmapFrame);

			encoder.Save(stream);
		}
	}
}
