using MSetExplorer.ScreenHelpers;
using MSS.Types.PColor;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorSpaces.xaml
	/// </summary>
	public partial class ColorSpaces : Window, IHaveAppNavRequestResponse
	{
		//private BitmapSrc bitmap1;
		//private BitmapSrc bitmap2;

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

		#region Constructor

		public ColorSpaces(AppNavRequestResponse appNavRequestResponse)
		{
			//bitmap1 = new BitmapSrc(256, 100, PixelFormats.Bgr32);
			//bitmap2 = new BitmapSrc(256, 100, PixelFormats.Bgr32);
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += ColorSpacesWindow_Loaded;

			InitializeComponent();
		}

		private void ColorSpacesWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//Debug.WriteLine("The DataContext is null as the ColorSpaces Window is being loaded.");
			}
			else
			{
				Debug.WriteLine("The ColorSpaces Window is now loaded");
			}

			Loaded -= ColorSpacesWindow_Loaded;

			TestRgbToHcl();
			DisplayGradients2();
		}

		#endregion

		#region Gradients1

		//private void DisplayGradients()
		//{
		//	FillBitmapWithGrays(bitmap1);
		//	bitmap1.Recreate();
		//	Image1.Source = bitmap1.Bitmap;

		//	FillBitmapWithGrays(bitmap2);
		//	bitmap2.Recreate();
		//	Image2.Source = bitmap2.Bitmap;
		//}

		//private void FillBitmapWithGrays(BitmapSrc bms)
		//{
		//	var rawImage = bms.RawImage;
		//	for (int y = 0; y <  bms.Height; y++)
		//	{
		//		var rowOffset = y * bms.Width * 4;
		//		for (int x = 0; x < bms.Width; x++)
		//		{
		//			var colOffset = rowOffset + x * 4;

		//			rawImage[colOffset] = (byte)(255 - x);
		//			rawImage[colOffset + 1] = (byte)(255 - x);
		//			rawImage[colOffset + 2] = (byte)(255 - x);
		//			rawImage[colOffset + 3] = 255;
		//		}
		//	}
		//}

		#endregion

		#region Gradients2

		private void DisplayGradients2()
		{
			//Rgb rgb1 = new Rgb(0.151195, 0.260917, 0.091675);
			//Rgb rgb2 = new Rgb(0.523395, 0.584329, 0.994155);

			//FillBitmapRgb(bitmap1, rgb1, rgb2);
			//bitmap1.Recreate();
			//Image1.Source = bitmap1.Bitmap;

			//Lch lch1 = new Lch(55, 48, 130);
			//Lch lch2 = new Lch(80, 53, 240);

			//FillBitmapLch(bitmap2, lch1, lch2);
			//bitmap2.Recreate();
			//Image2.Source = bitmap2.Bitmap;

			var imageData1 = CreateBWGradient(256);
			var bitmap1 = CreateBitmapSource(256, 100, PixelFormats.Bgr32, imageData1);
			Image1.Source = bitmap1;

			var imageData2 = CreateBWGradient(256);
			var bitmap2 = CreateBitmapSource(256, 100, PixelFormats.Bgr32, imageData2);
			Image2.Source = bitmap2;
		}

		//private void FillBitmapRgb(BitmapSrc bms, Rgb start, Rgb end)
		//{
		//	var rawImage = bms.RawImage;

		//	for (int y = 0; y < bms.Height; y++)
		//	{
		//		var rowOffset = y * bms.Width * 4;
		//		for (int x = 0; x < bms.Width; x++)
		//		{
		//			var colOffset = rowOffset + x * 4;

		//			rawImage[colOffset] = (byte)(255 - x);
		//			rawImage[colOffset + 1] = (byte)(255 - x);
		//			rawImage[colOffset + 2] = (byte)(255 - x);
		//			rawImage[colOffset + 3] = 255;
		//		}
		//	}
		//}

		//private void FillBitmapLch(BitmapSrc bms, Lch start, Lch end)
		//{
		//	var rawImage = bms.RawImage;
		//	for (int y = 0; y < bms.Height; y++)
		//	{
		//		var rowOffset = y * bms.Width * 4;
		//		for (int x = 0; x < bms.Width; x++)
		//		{
		//			var colOffset = rowOffset + x * 4;

		//			rawImage[colOffset] = (byte)(255 - x);
		//			rawImage[colOffset + 1] = (byte)(255 - x);
		//			rawImage[colOffset + 2] = (byte)(255 - x);
		//			rawImage[colOffset + 3] = 255;
		//		}
		//	}
		//}

		#endregion

		#region Bitmap Support

		private BitmapSource CreateBitmapSource(int width, int height, PixelFormat pixelFormat, byte[] imageData) 
		{
			int rawStride = (width * pixelFormat.BitsPerPixel + 7) / 8;
			byte[] rawImage = new byte[rawStride * height];

			FillRawImage(width, height, rawImage, imageData);
			var bitmap = BitmapSource.Create(width, height, 96, 96, pixelFormat, null, rawImage, rawStride);

			return bitmap;
		}

		//private void FillRawImage(int width, int height, byte[] rawImage, byte[] imageData)
		//{
		//	for (int y = 0; y < height; y++)
		//	{
		//		var rowOffset = y * width * 4;
		//		for (int x = 0; x < width; x++)
		//		{
		//			var colOffset = rowOffset + x * 4;

		//			rawImage[colOffset] = (byte)(255 - x);
		//			rawImage[colOffset + 1] = (byte)(255 - x);
		//			rawImage[colOffset + 2] = (byte)(255 - x);
		//			rawImage[colOffset + 3] = 255;
		//		}
		//	}
		//}

		private void FillRawImage(int width, int height, byte[] rawImage, byte[] imageData)
		{
			for (int y = 0; y < height; y++)
			{
				var rowOffset = y * width * 4;
				for (int x = 0; x < width; x++)
				{
					var destColOffset = rowOffset + x * 4;
					var srcColOffset = x * 3;

					rawImage[destColOffset] = imageData[srcColOffset];
					rawImage[destColOffset + 1] = imageData[srcColOffset + 1];
					rawImage[destColOffset + 2] = imageData[srcColOffset + 2];
					rawImage[destColOffset + 3] = 255;
				}
			}
		}

		private byte[] CreateBWGradient(int width)
		{
			var imageData = new byte[width * 3];
			for (int x = 0; x < width; x++)
			{
				var colOffset = x * 3;

				imageData[colOffset] = (byte)(255 - x);
				imageData[colOffset + 1] = (byte)(255 - x);
				imageData[colOffset + 2] = (byte)(255 - x);
			}

			return imageData;
		}

		#endregion

		#region Tests

		private void TestRgbToHcl()
		{
			Rgb redRgb = new Rgb("#3050A0");    // new Rgb("#ff0000");

			var redXyz = LchColorHelper.RgbToXyz(redRgb, LchColorHelper.PRO_PHOTO_TO_XYZ);
			Debug.Print($"The redXyz result is X = {redXyz.X}, u = {redXyz.Y}, v = {redXyz.Z}.");

			var redRgbRt = LchColorHelper.XyzToRgb(redXyz, LchColorHelper.XYZ_TO_PRO_PHOTO);
			Debug.Print($"RoundTrip RGB->XYZ->RGB = {redRgbRt.GetCssColor()}");

			var redLuv = LchColorHelper.XyzToLuv(redXyz, LchColorHelper.D50_WHITE_POINT);
			Debug.Print($"The Luv result is L = {redLuv.L}, u = {redLuv.U}, v = {redLuv.V}.");

			var redXyzRt = LchColorHelper.LuvToXyz(redLuv, LchColorHelper.D50_WHITE_POINT);
			redRgbRt = LchColorHelper.XyzToRgb(redXyzRt, LchColorHelper.XYZ_TO_PRO_PHOTO);
			Debug.Print($"RoundTrip RGB->XYZ->LUV->XYZ-RGB = {redRgbRt.GetCssColor()}");

			var redLch = LchColorHelper.LuvToLch(redLuv);
			Debug.Print($"The redLch result is L = {redLch.L}, C = {redLch.C}, H = {redLch.H}.");

			var redLuvRt = LchColorHelper.LchToLuv(redLch);
			redXyzRt = LchColorHelper.LuvToXyz(redLuvRt, LchColorHelper.D50_WHITE_POINT);
			redRgbRt = LchColorHelper.XyzToRgb(redXyzRt, LchColorHelper.XYZ_TO_PRO_PHOTO);
			Debug.Print($"RoundTrip RGB->XYZ->LUV->LCH->LUV->XYZ->RGB = {redRgbRt.GetCssColor()}");


			var blu_rgb = new Rgb("#3050A0");
			Debug.Print($"The Blu RBG vals are R:{blu_rgb.Get_sRGB_Vals()[0]}, G:{blu_rgb.Get_sRGB_Vals()[1]}, B: {blu_rgb.Get_sRGB_Vals()[2]}.");

			var blu_lch = LchColorHelper.RgbToLch(blu_rgb, LchColorHelper.PRO_PHOTO_TO_XYZ, LchColorHelper.D50_WHITE_POINT);
			Debug.Print($"The Blu LCH result is L = {blu_lch.L}, C = {blu_lch.C}, H = {blu_lch.H}.");

			var rt = LchColorHelper.LchToRgb(blu_lch, LchColorHelper.XYZ_TO_PRO_PHOTO, LchColorHelper.D50_WHITE_POINT);
			Debug.Print($"RoundTrip Rgb->Lch->Rgb = {rt.GetCssColor()}");
		}

		#endregion

		#region Button Handlers

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.ReturnToTopNav);
			Close();
		}

		private void ExitButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.Close);
			Close();
		}

		#endregion
	}

	//internal class BitmapSrc
	//{
	//	public int Width { get; init; }
	//	public int Height { get; init; }

	//	public PixelFormat PixelFormat { get; init; }
	//	public int RawStride { get; init; }

	//	public byte[] RawImage { get; init; }
	//	public BitmapSource Bitmap { get; set; }

	//	public BitmapSrc(int width, int height, PixelFormat pixelFormat)
	//	{
	//		Width = width;
	//		Height = height;
	//		PixelFormat = pixelFormat;

	//		RawStride = (width * pixelFormat.BitsPerPixel + 7) / 8;
	//		RawImage = new byte[RawStride * Height];

	//		// Create a BitmapSource.
	//		Bitmap = BitmapSource.Create(Width, Height, 96, 96, pixelFormat, null, RawImage, RawStride);
	//	}

	//	public void Recreate()
	//	{
	//		Bitmap = BitmapSource.Create(Width, Height, 96, 96, PixelFormat, null, RawImage, RawStride);

	//	}
	//}
}
