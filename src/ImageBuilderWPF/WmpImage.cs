using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageBuilderWPF
{
	public class WmpImage : IDisposable
	{
		#region Private Fields

		private readonly static PixelFormat PIXEL_FORMAT = PixelFormats.Pbgra32;
		private const int DOTS_PER_INCH = 96;

		private readonly Stream _outputStream;
		private bool _weOwnTheStream;

		public string Path { get; }

		//private readonly WmpBitmapEncoder _encoder;
		private readonly WriteableBitmap _bitmap;

		#endregion

		#region Constructors

		public WmpImage(string path, int width, int height) : 
			this(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read), path, width, height)
		{
			_weOwnTheStream = true;
		}

		public WmpImage(Stream outputStream, string path, int width, int height)
		{
			_outputStream = outputStream;
			_weOwnTheStream = false;
			Path = path;

			_bitmap = new WriteableBitmap(width, height, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);


		}

		#endregion

		#region Public Methods

		public void WriteBlock(Int32Rect sourceRect, byte[] imageData, int sourceStride, int destX, int destY)
		{
			//var destArea = new Int32Rect(0, 0, width, height);
			_bitmap.WritePixels(sourceRect, imageData, sourceStride, destX, destY);
		}

		public void End()
		{
			var bitmapFrame = BitmapFrame.Create(_bitmap);

			var encoder = new WmpBitmapEncoder();
			encoder.ImageQualityLevel = 1.0f;

			encoder.Frames.Add(bitmapFrame);
			encoder.Save(_outputStream);

			if(_weOwnTheStream)
			{
				_outputStream.Flush();
				_outputStream.Close();
			}

		}

		public void Abort()
		{
			//png.Abort();
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue; // To detect redundant calls

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing && _weOwnTheStream)
				{
					//png.End();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}
