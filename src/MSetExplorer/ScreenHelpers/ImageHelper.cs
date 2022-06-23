using ImageBuilder;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class ImageHelper
	{
		public static ImageSource GetPosterPreview(Poster poster, SizeInt previewImagesize, BitmapBuilder bitmapBuilder, IProjectAdapter projectAdapter, CancellationToken ct)
		{
			var posterAreaInfo = poster.MapAreaInfo;

			var coords = posterAreaInfo.Coords;
			var blockSize = posterAreaInfo.Subdivision.BlockSize;

			var previewMapArea = MapJobHelper.GetJobAreaInfo(coords, previewImagesize, blockSize, projectAdapter);

			Debug.WriteLine($"Creating Poster Preview Image. The coords are {coords}, size is {previewImagesize}.");

			var task = Task.Run(async () => await bitmapBuilder.BuildAsync(previewMapArea, poster.ColorBandSet, poster.MapCalcSettings, ct));

			ImageSource result;

			try
			{
				byte[] pixels = task.Result;
				result = CreateImageSource(pixels, previewImagesize);
			}
			catch (System.AggregateException)
			{
				result = CreateGenericImageSource(Colors.LightSeaGreen, previewImagesize);				
			}

			return result;
		}

		public static ImageSource CreateGenericImageSource(Color color, SizeInt previewImageSize)
		{
			byte r = color.R;
			byte g = color.G;
			byte b = color.B;

			byte[] pixels = new byte[previewImageSize.NumberOfCells * 4];
			
			for(var i = 0; i < previewImageSize.NumberOfCells; i++)
			{
				var offSet = i * 4;
				pixels[offSet] = b;
				pixels[offSet + 1] = g;
				pixels[offSet + 2] = r;
				pixels[offSet + 3] = 255;
			}

			var result = CreateImageSource(pixels, previewImageSize);
			return result;
		}

		public static ImageSource CreateImageSource(byte[] pixels, SizeInt size)
		{
			var w = size.Width;
			var h = size.Height;

			var bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);

			var rect = new Int32Rect(0, 0, w, h);
			var stride = 4 * w;
			bitmap.WritePixels(rect, pixels, stride, 0);

			return bitmap;
		}




	}
}
