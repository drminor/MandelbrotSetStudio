using Microsoft.Extensions.Logging.Abstractions;
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
	public class WmpImageWriter : IImageWriter
	{
		#region Private Fields

		private const int BYTES_PER_PIXEL = 4;

		private readonly static PixelFormat PIXEL_FORMAT = PixelFormats.Pbgra32;
		private const int DOTS_PER_INCH = 96;

		private readonly SynchronizationContext _synchronizationContext;
		private Action<MapSectionVectors> _returnMapSectionVectorsAction;


		private readonly Stream _outputStream;
		private bool _weOwnTheStream;

		private WriteableBitmap? _bitmap;

		#endregion

		#region Constructors

		public WmpImageWriter(string path, int width, int height, SynchronizationContext synchronizationContext) :
			this(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read), width, height, synchronizationContext)
		{
			_weOwnTheStream = true;
		}

		#region Public Properties

		public int BytesPerPixel => BYTES_PER_PIXEL;

		public Action<MapSectionVectors> ReturnMapSectionVectors { set => _returnMapSectionVectorsAction = value; }

		#endregion
		
		public WmpImageWriter(Stream outputStream, int width, int height, SynchronizationContext synchronizationContext)
		{
			_synchronizationContext = synchronizationContext;

			_outputStream = outputStream;
			_weOwnTheStream = false;

			_bitmap = null;

			_synchronizationContext.Post((o) =>
			{
				_bitmap = CreateBitmap(width, height);
			},
			null);

			_returnMapSectionVectorsAction = PlaceHolderAction;
		}

		#endregion

		#region Public Methods

		public void WriteBlock(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int destX, int destY)
		{
			var sourceStride = mapSectionVectors.BlockSize.Width * BYTES_PER_PIXEL;
			_synchronizationContext.Post((o) => WriteBlockInternal(sourceRect, mapSectionVectors, imageBuffer, sourceStride, destX, destY), null);
		}

		public void Save()
		{
			if (_bitmap == null)
			{
				throw new InvalidOperationException("WmpImage: Cannot call End when the bitmap is null.");
			}

			_synchronizationContext.Post((o) => SaveBitmap(_bitmap), null);
		}

		public void Close()
		{
			if (_weOwnTheStream)
			{
				_outputStream.Flush();
				_outputStream.Close();
				_outputStream.Dispose();
			}

			_bitmap = null;
		}

		#endregion

		#region Private Methods

		private void WriteBlockInternal(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int sourceStride, int destX, int destY)
		{
			_bitmap?.WritePixels(sourceRect, imageBuffer, sourceStride, destX, destY);
			_returnMapSectionVectorsAction(mapSectionVectors);
		}

		private WriteableBitmap CreateBitmap(int width, int height)
		{
			var result = new WriteableBitmap(width, height, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);
			return result;
		}

		private void SaveBitmap(WriteableBitmap bitmap)
		{
			var bitmapFrame = BitmapFrame.Create(bitmap);

			var encoder = new WmpBitmapEncoder();
			encoder.ImageQualityLevel = 1.0f;

			encoder.Frames.Add(bitmapFrame);
			encoder.Save(_outputStream);
		}

		private void PlaceHolderAction(MapSectionVectors mapSectionVectors)
		{
			throw new InvalidOperationException("Calling the PlaceHolderAction");
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
					Close();
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
