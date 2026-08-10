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


			ColorVal sRGB_Red = new ColorVal("#ff0000");

			var xYZ_Red = LchColorHelper.Multiply(sRGB_Red, LchColorHelper.RGB_TO_XYZ);

			var roundTrip = LchColorHelper.Multiply(xYZ_Red, LchColorHelper.XYZ_TO_RGB);

			var cssColor = roundTrip.GetCssColor();
			Debug.Print($"RoundTrip RGB_CssColor = {cssColor}");


			var luv = LchColorHelper.ConvertXyzToLuv(xYZ_Red, LchColorHelper.D65_WHITE_POINT);

			cssColor = luv.GetCssColor();
			Debug.Print($"LUV_CssColor = {cssColor}");
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
