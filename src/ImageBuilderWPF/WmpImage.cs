using MSS.Common;
using MSS.Types;
using System;
using System.IO;
using System.Threading;
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

		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private readonly Stream _outputStream;
		private bool _weOwnTheStream;

		public string Path { get; }

		private WriteableBitmap _bitmap;

		#endregion

		#region Constructors

		public WmpImage(string path, int width, int height, SynchronizationContext synchronizationContext, MapSectionVectorProvider mapSectionVectorProvider) : 
			this(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read), path, width, height, synchronizationContext, mapSectionVectorProvider)
		{
			_weOwnTheStream = true;
		}

		public WmpImage(Stream outputStream, string path, int width, int height, SynchronizationContext synchronizationContext, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_synchronizationContext = synchronizationContext;
			_mapSectionVectorProvider = mapSectionVectorProvider;

			_outputStream = outputStream;
			_weOwnTheStream = false;
			Path = path;

			_bitmap = new WriteableBitmap(1, 1, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);

			_synchronizationContext.Post((o) => CreateBitmap(width, height), null);
		}

		#endregion

		#region Public Methods

		public void WriteBlock(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, int sourceStride, int destX, int destY)
		{
			_synchronizationContext.Post((o) => WriteBlockInternal(sourceRect, mapSectionVectors, sourceStride, destX, destY), null);
		}

		public void End()
		{
			_synchronizationContext.Post((o) => SaveBitmap(), null);
		}

		public void Abort()
		{
			if (_weOwnTheStream)
			{
				_outputStream.Flush();
				_outputStream.Close();
				_outputStream.Dispose();
			}
		}

		#endregion

		#region Private Methods

		private void WriteBlockInternal(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, int sourceStride, int destX, int destY)
		{
			_bitmap.WritePixels(sourceRect, mapSectionVectors.BackBuffer, sourceStride, destX, destY);
			mapSectionVectors.DecreaseRefCount();
			_mapSectionVectorProvider.ReturnMapSectionVectors(mapSectionVectors);
		}

		private void CreateBitmap(int width, int height)
		{
			_bitmap = new WriteableBitmap(width, height, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);
		}

		private void SaveBitmap()
		{
			var bitmapFrame = BitmapFrame.Create(_bitmap);

			var encoder = new WmpBitmapEncoder();
			encoder.ImageQualityLevel = 1.0f;

			encoder.Frames.Add(bitmapFrame);
			encoder.Save(_outputStream);

			if (_weOwnTheStream)
			{
				_outputStream.Flush();
				_outputStream.Close();
				_outputStream.Dispose();
			}
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
