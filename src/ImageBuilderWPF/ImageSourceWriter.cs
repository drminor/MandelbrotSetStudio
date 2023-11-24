using MSS.Types;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageBuilderWPF
{
	public class ImageSourceWriter : IImageWriter
	{
		#region Private Fields

		private const int BYTES_PER_PIXEL = 4;

		private readonly SynchronizationContext _synchronizationContext;

		private Action<MapSectionVectors> _returnMapSectionVectorsAction;

		private WriteableBitmap? _bitmap;

		#endregion

		#region Constructors

		public ImageSourceWriter(WriteableBitmap writeableBitmap, SynchronizationContext synchronizationContext)
		{
			_bitmap = writeableBitmap;
			_synchronizationContext = synchronizationContext;
			_returnMapSectionVectorsAction = PlaceHolderAction;
		}

		#endregion

		#region Public Properties

		public int BytesPerPixel => BYTES_PER_PIXEL;

		public Action<MapSectionVectors> ReturnMapSectionVectors { set => _returnMapSectionVectorsAction = value; }

		#endregion

		#region Public Methods

		public void WriteBlock(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int destX, int destY)
		{
			var sourceStride = mapSectionVectors.BlockSize.Width * BYTES_PER_PIXEL;
			_synchronizationContext.Post((o) => WriteBlockInternal(sourceRect, mapSectionVectors, imageBuffer, sourceStride, destX, destY), null);
		}

		public void Save()
		{
		}

		public void Close()
		{
			_bitmap = null;
		}

		#endregion

		#region Private Methods

		private void WriteBlockInternal(Int32Rect sourceRect, MapSectionVectors mapSectionVectors, byte[] imageBuffer, int sourceStride, int destX, int destY)
		{
			_bitmap?.WritePixels(sourceRect, imageBuffer, sourceStride, destX, destY);

			_returnMapSectionVectorsAction(mapSectionVectors);
			//mapSectionVectors.DecreaseRefCount();
			//_mapSectionVectorProvider.ReturnMapSectionVectors(mapSectionVectors);
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
