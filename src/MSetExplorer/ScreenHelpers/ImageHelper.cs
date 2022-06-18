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
		public static ImageSource GetPosterPreview(IProjectAdapter projectAdapter, BitmapBuilder bitmapBuilder, Poster poster, SizeInt size)
		{
			var cts = new CancellationTokenSource();
			var posterAreaInfo = poster.MapAreaInfo;

			var coords = posterAreaInfo.Coords;
			var blockSize = posterAreaInfo.Subdivision.BlockSize;

			var previewMapArea = MapJobHelper.GetJobAreaInfo(coords, size, blockSize, projectAdapter);

			Debug.WriteLine($"Creating Poster Preview Image. The coords are {coords}, size is {size}.");

			var task = Task.Run(async () => await bitmapBuilder.BuildAsync(previewMapArea, poster.ColorBandSet, poster.MapCalcSettings, cts.Token));

			var imageData = task.Result;

			var result = CreateImageSource(imageData, size);

			//var result = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgra32, null);

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
