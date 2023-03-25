using MSS.Types;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace WpfMapDisplayPOC
{
	internal class SkiaHelper
	{
		public static SKBitmap ArrayToImage(byte[] pixelArray, SizeInt blockSize)
		{
			SKBitmap bitmap = new();
			GCHandle gcHandle = GCHandle.Alloc(pixelArray, GCHandleType.Pinned);
			SKImageInfo info = new(blockSize.Width, blockSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

			IntPtr ptr = gcHandle.AddrOfPinnedObject();
			int rowBytes = info.RowBytes;
			bitmap.InstallPixels(info, ptr, rowBytes, delegate { gcHandle.Free(); });

			return bitmap;
		}
	}
}
