using MSetExplorer.ScreenHelpers;
using MSS.Types.PColor;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
		#region Public Properties

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

		#endregion

		#region Constructor

		public ColorSpaces(AppNavRequestResponse appNavRequestResponse)
		{
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
			
			//DisplayGradients1();
			DisplayGradients2();
			DisplayGradients3();
		}

		#endregion

		#region Gradients1

		private void DisplayGradients1()
		{
			var imageData1 = CreateBWGradient(256);
			var bitmap1 = CreateBitmapSource(256, 100, PixelFormats.Bgr32, imageData1);
			Image1.Source = bitmap1;

			var imageData2 = CreateBWGradient(256);
			var bitmap2 = CreateBitmapSource(256, 100, PixelFormats.Bgr32, imageData2);
			Image2.Source = bitmap2;
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

		#region Gradients2

		private void DisplayGradients2()
		{
			var width = 256;

			//Rgb rgb1 = Rgb.Black;
			//Rgb rgb2 = Rgb.White;

			Rgb rgb1 = new Rgb(0.151195, 0.260917, 0.091675);
			Rgb rgb2 = new Rgb(0.523395, 0.584329, 0.994155);

			//Rgb rgb1 = new Rgb(0.331198, 0.571708, 0.283259);
			//Rgb rgb2 = new Rgb(0.556835, 0.742205, 0.970761);

			var imageData1 = CreateRgbGradient(width, rgb1, rgb2);

			var bitmap1 = CreateBitmapSource(width, 100, PixelFormats.Bgr32, imageData1);
			Image1.Source = bitmap1;
		}

		private byte[] CreateRgbGradient(int width, Rgb start, Rgb end)
		{
			var c1 = CreateChannelData(width, start.red, end.red);
			var c2 = CreateChannelData(width, start.green, end.green);
			var c3 = CreateChannelData(width, start.blue, end.blue);

			var imageData = GetImageData(width, c1, c2, c3);

			return imageData;
		}

		private double[] CreateChannelData(int width, double start, double end)
		{
			double[] result = new double[width];

			var diff = end - start;

			for (int x = 0; x < width; x++)
			{
				result[x] = start + diff * x / width;
			}

			return result;
		}

		private byte[] GetImageData(int width, double[] c1, double[] c2, double[] c3)
		{
			var imageData = new byte[width * 3];

			for (int x = 0; x < width; x++)
			{
				var colOffset = x * 3;

				imageData[colOffset] = GetByteVal(c1[x]);
				imageData[colOffset + 1] = GetByteVal(c2[x]); 
				imageData[colOffset + 2] = GetByteVal(c3[x]);
			}

			return imageData;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte GetByteVal(double f)
		{
			return (byte)Math.Round(f * 255);
		}

		#endregion

		#region Gradients3

		private void DisplayGradients3()
		{
			var width = 256;

			Lch lch1 = new Lch(55, 48, 130);
			Lch lch2 = new Lch(80, 53, 240);

			//Lch lch1 = new Lch(55, 48, 130);
			//Lch lch2 = new Lch(75, 53, 240);

			var imageData2 = CreateLchGradient(width, lch1, lch2);

			var bitmap2 = CreateBitmapSource(width, 100, PixelFormats.Bgr32, imageData2);
			Image2.Source = bitmap2;
		}

		private byte[] CreateLchGradient(int width, Lch start, Lch end)
		{
			var c1 = CreateChannelData(width, start.L, end.L);
			var c2 = CreateChannelData(width, start.C, end.C);
			var c3 = CreateChannelData(width, start.H, end.H);

			var lches = GetLches(width, c1, c2, c3);
			var rgbs = GetRgbs(lches);

			var imageData = GetImageData(width, rgbs);

			return imageData;
		}

		private Lch[] GetLches(int width, double[] c1, double[] c2, double[] c3)
		{
			var result = new Lch[width];

			for (int x = 0; x < width; x++)
			{
				result[x] = new Lch(c1[x], c2[x], c3[x]);
			}

			return result;
		}


		private Rgb[] GetRgbs(Lch[] lches)
		{
			var width = lches.Length;

			var result = new Rgb[width];

			for (int x = 0; x < width; x++)
			{
				result[x] = LchColorHelper.LchToRgb(lches[x]);
			}

			return result;
		}


		private byte[] GetImageData(int width, Rgb[] rgbs)
		{
			var imageData = new byte[width * 3];

			for (int x = 0; x < width; x++)
			{
				var colOffset = x * 3;

				var bVals = rgbs[x].Get_sRGB_Vals();

				imageData[colOffset] = bVals[0];
				imageData[colOffset + 1] = bVals[1];
				imageData[colOffset + 2] = bVals[2];
			}

			return imageData;
		}

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

		/***************
		 
		Test Results

		The redXyz result is X = 0.2122363733333333, u = 0.27760615529411764, v = 0.5177788235294117.
		RoundTrip RGB->XYZ->RGB = #3050a0
		The Luv result is L = 59.671850251336096, u = -51.191056212966984, v = -51.76796846170507.
		RoundTrip RGB->XYZ->LUV->XYZ-RGB = #3050a0
		The redLch result is L = 59.671850251336096, C = 72.80416742777324, H = 225.32104316423187.
		RoundTrip RGB->XYZ->LUV->LCH->LUV->XYZ->RGB = #3050a0
		The Blu RBG vals are R:48, G:80, B: 160.
		The Blu LCH result is L = 59.671850251336096, C = 72.80416742777324, H = 225.32104316423187.
		RoundTrip Rgb->Lch->Rgb = #3050a0


		59.671850251336096, u = -51.19134202998029, v = -51.76078197070966.
		L = 59.671850251336096, C = 72.79925857486303, H = 225.31690626003223.

		59.6718 -51.1913 -51.7608

		59.6718 72.7993 225.3169

		****************/
		
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

}
