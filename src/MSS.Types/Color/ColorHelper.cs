using MSS.Types.PColor;
using System;
using System.Drawing;

namespace MSS.Types
{
	public static class ColorHelper
	{
		#region HSB Support

		public static double[] GetHSB(byte[] rgbColorComps)
		{
			var color = Color.FromArgb(rgbColorComps[0], rgbColorComps[1], rgbColorComps[2]);

			var result = new double[]
			{
				color.GetHue(),
				color.GetSaturation(),
				color.GetBrightness()
			};

			return result;
		}

		//public static void PlaceRgb(double[] hsl, Span<byte> destination)
		//{
		//	var color = FromHsb(hsl[0], hsl[1], hsl[2]);

		//	destination[0] = color.B;
		//	destination[1] = color.G;
		//	destination[2] = color.R;
		//	destination[3] = color.A;
		//}

		public static int PlaceHsb(double[] hsb, Span<byte> destination)
		{
			var numberOfErrors = 0;
			var colorComps = GetColorComps(hsb);

			byte r;
			if (colorComps[0] > 255)
			{
				numberOfErrors++;
				r = 50;
			}
			else
			{
				r = Convert.ToByte(colorComps[0]);
			}

			byte g;
			if (colorComps[1] > 255)
			{
				numberOfErrors++;
				g = 50;
			}
			else
			{
				g = Convert.ToByte(colorComps[1]);
			}

			byte b;
			if (colorComps[2] > 255)
			{
				numberOfErrors++;
				b = 50;
			}
			else
			{
				b = Convert.ToByte(colorComps[2]);
			}

			destination[0] = b;
			destination[1] = g;
			destination[2] = r;
			destination[3] = 255;

			return numberOfErrors;
		}


		private static int[] GetColorComps(double[] hsb)
		{
			var hue = hsb[0];
			var saturation = hsb[1];
			var brightness = hsb[2];

			if (0f > hue
				|| 360f < hue)
			{
				throw new ArgumentOutOfRangeException(
					"hue",
					hue,
					"Value must be within a range of 0 - 360.");
			}

			if (0f > saturation
				|| 1f < saturation)
			{
				throw new ArgumentOutOfRangeException(
					"saturation",
					saturation,
					"Value must be within a range of 0 - 1.");
			}

			if (0f > brightness
				|| 1f < brightness)
			{
				throw new ArgumentOutOfRangeException(
					"brightness",
					brightness,
					"Value must be within a range of 0 - 1.");
			}

			if (0 == saturation)
			{
				return new int[] {
					Convert.ToInt32(brightness * 255),
					Convert.ToInt32(brightness * 255),
					Convert.ToInt32(brightness * 255)
				};
			}

			double fMax;
			double fMin;

			if (0.5 < brightness)
			{
				fMax = brightness - (brightness * saturation) + saturation;
				fMin = brightness + (brightness * saturation) - saturation;
			}
			else
			{
				fMax = brightness + (brightness * saturation);
				fMin = brightness - (brightness * saturation);
			}

			var iSextant = (int)Math.Floor(hue / 60d);
			if (300 <= hue)
			{
				hue -= 360;
			}

			hue /= 60;
			hue -= 2 * Math.Floor((iSextant + 1) % 6d / 2d);

			double fMid;
			if (0 == iSextant % 2)
			{
				fMid = (hue * (fMax - fMin)) + fMin;
			}
			else
			{
				fMid = fMin - (hue * (fMax - fMin));
			}

			switch (iSextant)
			{
				case 1:
					return new int[] { Convert.ToInt32(fMid * 255), Convert.ToInt32(fMax * 255), Convert.ToInt32(fMin * 255) };
				case 2:
					return new int[] { Convert.ToInt32(fMin * 255), Convert.ToInt32(fMax * 255), Convert.ToInt32(fMid * 255) };
				case 3:
					return new int[] { Convert.ToInt32(fMin * 255), Convert.ToInt32(fMid * 255), Convert.ToInt32(fMax * 255) };
				case 4:
					return new int[] { Convert.ToInt32(fMid * 255), Convert.ToInt32(fMin * 255), Convert.ToInt32(fMax * 255) };
				case 5:
					return new int[] { Convert.ToInt32(fMax * 255), Convert.ToInt32(fMin * 255), Convert.ToInt32(fMid * 255) };
				default:
					return new int[] { Convert.ToInt32(fMax * 255), Convert.ToInt32(fMid * 255), Convert.ToInt32(fMin * 255) };
			}
		}

		private static Color FromHsb(double hue, double saturation, double brightness)
		{
			if (0f > hue
				|| 360f < hue)
			{
				throw new ArgumentOutOfRangeException(
					"hue",
					hue,
					"Value must be within a range of 0 - 360.");
			}

			if (0f > saturation
				|| 1f < saturation)
			{
				throw new ArgumentOutOfRangeException(
					"saturation",
					saturation,
					"Value must be within a range of 0 - 1.");
			}

			if (0f > brightness
				|| 1f < brightness)
			{
				throw new ArgumentOutOfRangeException(
					"brightness",
					brightness,
					"Value must be within a range of 0 - 1.");
			}

			if (0 == saturation)
			{
				return Color.FromArgb(
									255,
									Convert.ToInt32(brightness * 255),
									Convert.ToInt32(brightness * 255),
									Convert.ToInt32(brightness * 255));
			}

			double fMax, fMid, fMin;
			int iSextant, iMax, iMid, iMin;

			if (0.5 < brightness)
			{
				fMax = brightness - (brightness * saturation) + saturation;
				fMin = brightness + (brightness * saturation) - saturation;
			}
			else
			{
				fMax = brightness + (brightness * saturation);
				fMin = brightness - (brightness * saturation);
			}

			iSextant = (int)Math.Floor(hue / 60d);
			if (300f <= hue)
			{
				hue -= 360f;
			}

			hue /= 60d;
			hue -= 2d * (float)Math.Floor((iSextant + 1d) % 6d / 2d);
			if (0 == iSextant % 2)
			{
				fMid = (hue * (fMax - fMin)) + fMin;
			}
			else
			{
				fMid = fMin - (hue * (fMax - fMin));
			}

			iMax = Convert.ToInt32(fMax * 255);
			iMid = Convert.ToInt32(fMid * 255);
			iMin = Convert.ToInt32(fMin * 255);

			switch (iSextant)
			{
				case 1:
					return Color.FromArgb(255, iMid, iMax, iMin);
				case 2:
					return Color.FromArgb(255, iMin, iMax, iMid);
				case 3:
					return Color.FromArgb(255, iMin, iMid, iMax);
				case 4:
					return Color.FromArgb(255, iMid, iMin, iMax);
				case 5:
					return Color.FromArgb(255, iMax, iMin, iMid);
				default:
					return Color.FromArgb(255, iMax, iMid, iMin);
			}
		}

		private static Color FromAhsb(int alpha, double hue, double saturation, double brightness)
		{
			if (0 > alpha
				|| 255 < alpha)
			{
				throw new ArgumentOutOfRangeException(
					"alpha",
					alpha,
					"Value must be within a range of 0 - 255.");
			}

			if (0f > hue
				|| 360f < hue)
			{
				throw new ArgumentOutOfRangeException(
					"hue",
					hue,
					"Value must be within a range of 0 - 360.");
			}

			if (0f > saturation
				|| 1f < saturation)
			{
				throw new ArgumentOutOfRangeException(
					"saturation",
					saturation,
					"Value must be within a range of 0 - 1.");
			}

			if (0f > brightness
				|| 1f < brightness)
			{
				throw new ArgumentOutOfRangeException(
					"brightness",
					brightness,
					"Value must be within a range of 0 - 1.");
			}

			if (0 == saturation)
			{
				return Color.FromArgb(
									alpha,
									Convert.ToInt32(brightness * 255),
									Convert.ToInt32(brightness * 255),
									Convert.ToInt32(brightness * 255));
			}

			double fMax, fMid, fMin;
			int iSextant, iMax, iMid, iMin;

			if (0.5 < brightness)
			{
				fMax = brightness - (brightness * saturation) + saturation;
				fMin = brightness + (brightness * saturation) - saturation;
			}
			else
			{
				fMax = brightness + (brightness * saturation);
				fMin = brightness - (brightness * saturation);
			}

			iSextant = (int)Math.Floor(hue / 60f);
			if (300f <= hue)
			{
				hue -= 360f;
			}

			hue /= 60f;
			hue -= 2f * (float)Math.Floor((iSextant + 1f) % 6f / 2f);
			if (0 == iSextant % 2)
			{
				fMid = (hue * (fMax - fMin)) + fMin;
			}
			else
			{
				fMid = fMin - (hue * (fMax - fMin));
			}

			iMax = Convert.ToInt32(fMax * 255);
			iMid = Convert.ToInt32(fMid * 255);
			iMin = Convert.ToInt32(fMin * 255);

			switch (iSextant)
			{
				case 1:
					return Color.FromArgb(alpha, iMid, iMax, iMin);
				case 2:
					return Color.FromArgb(alpha, iMin, iMax, iMid);
				case 3:
					return Color.FromArgb(alpha, iMin, iMid, iMax);
				case 4:
					return Color.FromArgb(alpha, iMid, iMin, iMax);
				case 5:
					return Color.FromArgb(alpha, iMax, iMin, iMid);
				default:
					return Color.FromArgb(alpha, iMax, iMid, iMin);
			}
		}

		#endregion

		#region LCH Support

		public static Lch GetLch(byte[] rgbColorComps)
		{
			var rgb = new Rgb(rgbColorComps[0], rgbColorComps[1], rgbColorComps[2]);
			var result = LchColorHelper.RgbToLch(rgb);

			return result;
		}

		public static Lch GetLch(byte r, byte g, byte b)
		{
			var rgb = new Rgb(r,g,b);
			var result = LchColorHelper.RgbToLch(rgb);

			return result;
		}

		public static Rgb GetRgb(double[] lchColorComps)
		{
			var lch = new Lch(lchColorComps[0], lchColorComps[1], lchColorComps[2]);
			var result = LchColorHelper.LchToRgb(lch);

			return result;
		}
		public static Rgb GetRgb(Lch lch)
		{
			var result = LchColorHelper.LchToRgb(lch);

			return result;
		}

		public static int PlaceLch(Lch lch, Span<byte> destination)
		{
			var numberOfErrors = 0;

			var rgb = LchColorHelper.LchToRgb(lch);

			var byteVals = rgb.Get_sRGB_Vals();

			byte r;
			if (rgb.red > 1)
			{
				numberOfErrors++;
				r = 255;
			}
			else if (rgb.red < 0)
			{
				numberOfErrors++;
				r = 0;
			}
			else
			{
				r = byteVals[0];
			}

			byte g;
			if (rgb.green > 255)
			{
				numberOfErrors++;
				g = 255;
			}
			else if (rgb.green < 0)
			{
				numberOfErrors++;
				g = 0;
			}
			else
			{
				g = byteVals[1];
			}

			byte b;
			if (rgb.blue > 1)
			{
				numberOfErrors++;
				b = 255;
			}
			else if (rgb.blue < 0)
			{
				numberOfErrors++;
				b = 0;
			}
			else
			{
				b = byteVals[2];
			}

			destination[0] = b;
			destination[1] = g;
			destination[2] = r;
			destination[3] = 255;

			return numberOfErrors;
		}

		#endregion
	}
}
