using MSetExplorer.ScreenHelpers;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using MSS.Types.PColor;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorSpaces.xaml
	/// </summary>
	public partial class ColorSpaces : Window, IHaveAppNavRequestResponse
	{
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
				//_vm = (JobDetailsViewModel)DataContext;
				//_vm.PropertyChanged += _vm_PropertyChanged;

				Debug.WriteLine("The ColorSpaces Window is now loaded");
			}
			Loaded -= ColorSpacesWindow_Loaded;

			//var bitmap = CreateBWGradientImage();
			//Image1.Source = bitmap;

			CreateBWGradientImage32();
			CreateBWGradientImage64();


			Rgb redRgb = new Rgb("#3050A0");	// new Rgb("#ff0000");

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

		private BitmapSource CreateBWGradientImage32()
		{
			// Define parameters used to create the BitmapSource.
			PixelFormat pf = PixelFormats.Bgr32;

			int width = 256;
			int height = 100;
			int rawStride = (width * pf.BitsPerPixel + 7) / 8;
			byte[] rawImage = new byte[rawStride * height];

			for(int y = 0; y < height; y++)
			{
				var rowOffset = y * width * 4;
				for(int x = 0; x < width; x++)
				{
					var colOffset = rowOffset + x * 4;

					rawImage[colOffset] = (byte)(255 - x);
					rawImage[colOffset + 1] = (byte)(255 - x);
					rawImage[colOffset + 2] = (byte)(255 - x);
					rawImage[colOffset + 3] = 255;
				}
			}

			// Create a BitmapSource.
			BitmapSource bitmap = BitmapSource.Create(width, height,
				96, 96, pf, null,
				rawImage, rawStride);


			Image1.Source = bitmap;

			return bitmap;
		}

		private BitmapSource CreateBWGradientImage64()
		{
			// Define parameters used to create the BitmapSource.
			PixelFormat pf = PixelFormats.Rgba64;

			int width = 256;
			int height = 100;
			int rawStride = (width * pf.BitsPerPixel + 7) / 8;
			byte[] rawImage = new byte[rawStride * height];

			for (int y = 0; y < height; y++)
			{
				var rowOffset = y * width * 8;
				for (int x = 0; x < width; x++)
				{
					var colOffset = rowOffset + x * 8;

					rawImage[colOffset] = 0;
					rawImage[colOffset + 1] = (byte)(255 - x);
					rawImage[colOffset + 2] = 0;
					rawImage[colOffset + 3] = (byte)(255 - x);
					rawImage[colOffset + 4] = 0;
					rawImage[colOffset + 5] = (byte)(255 - x);
					rawImage[colOffset + 6] = 255;
					rawImage[colOffset + 7] = 255;
				}
			}

			// Create a BitmapSource.
			BitmapSource bitmap = BitmapSource.Create(width, height,
				96, 96, pf, null,
				rawImage, rawStride);


			Image2.Source = bitmap;

			return bitmap;
		}


		private void _vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(PerformanceHarnessMainWinViewModel.MathOpCounts))
			//{

			//}
		}

		#endregion

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

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
