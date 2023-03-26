using MSS.Types;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfMapDisplayPOC
{
	internal class SkiaHelper
	{
		public static void ClearCanvas(WriteableBitmap bitmap, SKColor canvasClearColor)
		{
			if (bitmap == null)
			{
				throw new InvalidOperationException("Cannot Place a Bitmap before the BitmapGrid is initialized.");
			}

			bitmap.Lock();

			var imgInfo = new SKImageInfo((int)bitmap.Width, (int)bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

			using (var surface = SKSurface.Create(imgInfo, bitmap.BackBuffer, bitmap.BackBufferStride))
			{
				surface.Canvas.Clear(canvasClearColor);
			}

			bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)bitmap.Width, (int)bitmap.Height));
			bitmap.Unlock();
		}

		public static void PlaceBitmap(WriteableBitmap bitmap, SKBitmap sKBitmap, SKPoint sKPoint)
		{
			if (bitmap == null)
			{
				throw new InvalidOperationException("Cannot Place a Bitmap before the BitmapGrid is initialized.");
			}

			bitmap.Lock();

			var imgInfo = new SKImageInfo((int)bitmap.Width, (int)bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
			//var paint = new SKPaint();
			//paint.BlendMode = SKBlendMode.Multiply;

			using (var surface = SKSurface.Create(imgInfo, bitmap.BackBuffer, bitmap.BackBufferStride))
			{
				surface.Canvas.DrawBitmap(sKBitmap, sKPoint/*, paint*/);
			}

			bitmap.AddDirtyRect(new Int32Rect((int)sKPoint.X, (int)sKPoint.Y, sKBitmap.Width, sKBitmap.Height));
			//bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)bitmap.Width, (int)bitmap.Height));

			bitmap.Unlock();
		}

		public static void PlaceBitmapBuf(IntPtr backBuffer, int width, int height, SKBitmap sKBitmap, SKPoint sKPoint)
		{
			var imgInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			//var paint = new SKPaint();
			//paint.BlendMode = SKBlendMode.Multiply;

			using (var surface = SKSurface.Create(imgInfo, backBuffer, width * 4))
			{
				surface.Canvas.DrawBitmap(sKBitmap, sKPoint/*, paint*/);
			}
		}

		public static void CallForUpdate(WriteableBitmap bitmap, Int32Rect rect)
		{
			bitmap.Lock();
			bitmap.AddDirtyRect(rect);
			bitmap.Unlock();
		}

		public static SKBitmap ArrayToImage(byte[] pixelArray, SizeInt blockSize)
		{
			var bytesAsInts = GetIntArray(pixelArray);
			GCHandle gcHandle = GCHandle.Alloc(pixelArray, GCHandleType.Pinned);
			IntPtr ptr = gcHandle.AddrOfPinnedObject();

			SKBitmap bitmap = new();
			SKImageInfo info = new(blockSize.Width, blockSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul); //PixelFormats.Bgra32
			bitmap.InstallPixels(info, ptr, info.RowBytes, delegate { gcHandle.Free(); });

			return bitmap;
		}

		private static int[] GetIntArray(byte[] bArray)
		{
			var size = bArray.Length / sizeof(int);
			var bytesAsInts = new int[size];

			Buffer.BlockCopy(bArray, 0, bytesAsInts, 0, bArray.Length);

			return bytesAsInts;
		}

		//private static int[] GetIntArray2(byte[] bArray)
		//{
		//	var size = bArray.Length / sizeof(int);
		//	var bytesAsInts = new int[size];

		//	for (var index = 0; index < size; index++)
		//	{
		//		bytesAsInts[index] = BitConverter.ToInt32(bArray, index * sizeof(int));
		//	}

		//	return bytesAsInts;
		//}

		////private int[] GetIntArray3(byte[] bArray)
		////{
		////	var bytesAsInts = bArray.Select(x => (int)x).ToArray();

		////	return bytesAsInts;
		////}

		////private int[] GetIntArray4(byte[] bArray)
		////{
		////	var bytesAsInts = Array.ConvertAll(bArray, c => (int)c);

		////	return bytesAsInts;
		////}

		//private int[] GetIntArray5(byte[] bArray)
		//{
		//	var bytesAsInts = MemoryMarshal.Cast<byte, int>(bArray);

		//	return bytesAsInts.ToArray();
		//}

	}
}
