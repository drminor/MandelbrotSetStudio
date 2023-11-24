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
	public class ImageData : IDisposable
	{
		#region Private Fields

		private const int BYTES_PER_PIXEL = 4;

		private readonly static PixelFormat PIXEL_FORMAT = PixelFormats.Pbgra32;
		private const int DOTS_PER_INCH = 96;

		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private WriteableBitmap _bitmap;

		#endregion

		#region Constructors

		public ImageData(int width, int height, SynchronizationContext synchronizationContext, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_synchronizationContext = synchronizationContext;
			_mapSectionVectorProvider = mapSectionVectorProvider;

			_bitmap = new WriteableBitmap(1, 1, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);

			_synchronizationContext.Post((o) => CreateBitmap(width, height), null);
		}

		#endregion

		#region Public Properties

		public int BytesPerPixel => BYTES_PER_PIXEL;

		public int PixelBufferSize => _bitmap.PixelWidth * _bitmap.PixelHeight * BYTES_PER_PIXEL;

		public void FillPixelBuffer(byte[] pixelArray)
		{
			var stride = _bitmap.PixelWidth * BYTES_PER_PIXEL;
			_bitmap.CopyPixels(pixelArray, stride, 0);
		}

		#endregion

		#region Public Methods

		public void WriteBlock(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int destX, int destY)
		{
			var sourceStride = mapSectionVectors.BlockSize.Width * BYTES_PER_PIXEL;
			_synchronizationContext.Post((o) => WriteBlockInternal(sourceRect, mapSectionVectors, imageBuffer, sourceStride, destX, destY), null);
		}

		public void End()
		{
		}

		public void Abort()
		{
			_bitmap = new WriteableBitmap(1, 1, 0, 0, PIXEL_FORMAT, null);
		}

		#endregion

		#region Private Methods

		private void WriteBlockInternal(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int sourceStride, int destX, int destY)
		{
			_bitmap?.WritePixels(sourceRect, imageBuffer, sourceStride, destX, destY);

			mapSectionVectors.DecreaseRefCount();
			_mapSectionVectorProvider.ReturnMapSectionVectors(mapSectionVectors);
		}

		private void CreateBitmap(int width, int height)
		{
			_bitmap = new WriteableBitmap(width, height, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue; // To detect redundant calls

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Abort();
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
