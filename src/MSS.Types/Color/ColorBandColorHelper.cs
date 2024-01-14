using System;

namespace MSS.Types.Color
{
	public class ColorBandColorHelper
	{
		public static ColorBandColor GetContrast(ColorBandColor Source)
		{
			// Following code is taken from GitHub
			// https://github.com/sanjayatpilcrow/SharpSnippets
			// Code by Sanjay(http://sharpsnippets.wordpress.com/)
			// By: <a href="https://sharpsnippets.wordpress.com/2014/03/11/c-extension-complementary-color/" title="C# Extension: Complementary Color" target="_blank">C# Extension: Complementary Color</a>
			
			var inputColor = Source;

			// if RGB values are close to each other by a diff less than 10%,
			// then if
			// RGB values are lighter side, decrease the blue by 50% (eventually it will increase in conversion below),
			// if RBB values are on darker side, decrease yellow by about 50% (it will increase in conversion)

			var avgColorValue = (byte)((Source.ColorComps[0] + Source.ColorComps[1] + Source.ColorComps[2]) / 3);

			int diff_r = Math.Abs(Source.ColorComps[0] - avgColorValue);
			int diff_g = Math.Abs(Source.ColorComps[1] - avgColorValue);
			int diff_b = Math.Abs(Source.ColorComps[2] - avgColorValue);

			if (diff_r < 20 && diff_g < 20 && diff_b < 20) //The color is a shade of gray
			{
				if (avgColorValue < 123) //color is dark
				{
					inputColor = new ColorBandColor(new byte[] { 220, 230, 50 });
				}
				else
				{
					inputColor = new ColorBandColor(new byte[] { 255, 255, 50 });
				}
			}

			var rgb = new RGB { R = inputColor.ColorComps[0], G = inputColor.ColorComps[1], B = inputColor.ColorComps[2] };
			var hsb = ConvertToHSB(rgb);
			
			hsb.H = hsb.H < 180
				? hsb.H + 180
				: hsb.H - 180;

			//_hsb.B = _isColorDark ? 240 : 50; //Added to create dark on light, and light on dark

			rgb = ConvertToRGB(hsb);

			var result = new ColorBandColor(new byte[] { (byte)rgb.R, (byte)rgb.G, (byte)rgb.B});

			return result;
		}

		#region Code from MSDN

		public static RGB ConvertToRGB(HSB hsb)
		{
			// Following code is taken as it is from MSDN.
			// By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
			
			var chroma = hsb.S * hsb.B;
			var hue2 = hsb.H / 60;
			var x = chroma * (1 - Math.Abs(hue2 % 2 - 1));
			
			var r1 = 0d;
			var g1 = 0d;
			var b1 = 0d;

			if (hue2 >= 0 && hue2 < 1)
			{
				r1 = chroma;
				g1 = x;
			}
			else if (hue2 >= 1 && hue2 < 2)
			{
				r1 = x;
				g1 = chroma;
			}
			else if (hue2 >= 2 && hue2 < 3)
			{
				g1 = chroma;
				b1 = x;
			}
			else if (hue2 >= 3 && hue2 < 4)
			{
				g1 = x;
				b1 = chroma;
			}
			else if (hue2 >= 4 && hue2 < 5)
			{
				r1 = x;
				b1 = chroma;
			}
			else if (hue2 >= 5 && hue2 <= 6)
			{
				r1 = chroma;
				b1 = x;
			}

			var m = hsb.B - chroma;

			return new RGB()
			{
				R = r1 + m,
				G = g1 + m,
				B = b1 + m
			};
		}

		public static HSB ConvertToHSB(RGB rgb)
		{
			// Following code is taken as it is from MSDN.
			// By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
			
			var r = rgb.R;
			var g = rgb.G;
			var b = rgb.B;

			var max = Max(r, g, b);
			var min = Min(r, g, b);
			var chroma = max - min;
			var hue2 = 0d;
			
			if (chroma != 0)
			{
				if (max == r)
				{
					hue2 = (g - b) / chroma;
				}
				else if (max == g)
				{
					hue2 = (b - r) / chroma + 2;
				}
				else
				{
					hue2 = (r - g) / chroma + 4;
				}
			}

			var hue = hue2 * 60;

			if (hue < 0)
			{
				hue += 360;
			}
			
			var brightness = max;
			double saturation = 0;
			
			if (chroma != 0)
			{
				saturation = chroma / brightness;
			}
			
			return new HSB()
			{
				H = hue,
				S = saturation,
				B = brightness
			};
		}

		private static double Max(double d1, double d2, double d3)
		{
			if (d1 > d2)
			{
				return Math.Max(d1, d3);
			}
			return Math.Max(d2, d3);
		}
		
		private static double Min(double d1, double d2, double d3)
		{
			if (d1 < d2)
			{
				return Math.Min(d1, d3);
			}
			return Math.Min(d2, d3);
		}
		
		public struct RGB
		{
			internal double R;
			internal double G;
			internal double B;
		}
		
		public struct HSB
		{
			internal double H;
			internal double S;
			internal double B;
		}

		#endregion

		#region HSP
		/*
		 * 
		#define Pr  .299
		#define Pg  .587
		#define Pb  .114

		//  public domain function by Darel Rex Finley, 2006
		//
		//  This function expects the passed-in values to be on a scale
		//  of 0 to 1, and uses that same scale for the return values.
		//
		//  See description/examples at alienryderflex.com/hsp.html

		void RGBtoHSP(
		double R, double G, double B,
		double* H, double* S, double* P)
		{

			//  Calculate the Perceived brightness.
			*P = sqrt(R * R * Pr + G * G * Pg + B * B * Pb);

			//  Calculate the Hue and Saturation.  (This part works
			//  the same way as in the HSV/B and HSL systems???.)
			if (R == G && R == B)
			{
				*H = 0.; *S = 0.; return;
			}
			if (R >= G && R >= B)
			{   //  R is largest
				if (B >= G)
				{
					*H = 6./ 6.- 1./ 6.* (B - G) / (R - G); *S = 1.- G / R;
				}
				else
				{
					*H = 0./ 6.+ 1./ 6.* (G - B) / (R - B); *S = 1.- B / R;
				}
			}
			else if (G >= R && G >= B)
			{   //  G is largest
				if (R >= B)
				{
					*H = 2./ 6.- 1./ 6.* (R - B) / (G - B); *S = 1.- B / G;
				}
				else
				{
					*H = 2./ 6.+ 1./ 6.* (B - R) / (G - R); *S = 1.- R / G;
				}
			}
			else
			{   //  B is largest
				if (G >= R)
				{
					*H = 4./ 6.- 1./ 6.* (G - R) / (B - R); *S = 1.- R / B;
				}
				else
				{
					*H = 4./ 6.+ 1./ 6.* (R - G) / (B - G); *S = 1.- G / B;
				}
			}
		}



		//  public domain function by Darel Rex Finley, 2006
		//
		//  This function expects the passed-in values to be on a scale
		//  of 0 to 1, and uses that same scale for the return values.
		//
		//  Note that some combinations of HSP, even if in the scale
		//  0-1, may return RGB values that exceed a value of 1.  For
		//  example, if you pass in the HSP color 0,1,1, the result
		//  will be the RGB color 2.037,0,0.
		//
		//  See description/examples at alienryderflex.com/hsp.html

		void HSPtoRGB(
		double H, double S, double P,
		double* R, double* G, double* B)
		{

			double part, minOverMax = 1.- S;

			if (minOverMax > 0.)
			{
				if (H < 1./ 6.)
				{   //  R>G>B
					H = 6.* (H - 0./ 6.); part = 1.+ H * (1./ minOverMax - 1.);
					*B = P / sqrt(Pr / minOverMax / minOverMax + Pg * part * part + Pb);
					*R = (*B) / minOverMax; *G = (*B) + H * ((*R) - (*B));
				}
				else if (H < 2./ 6.)
				{   //  G>R>B
					H = 6.* (-H + 2./ 6.); part = 1.+ H * (1./ minOverMax - 1.);
					*B = P / sqrt(Pg / minOverMax / minOverMax + Pr * part * part + Pb);
					*G = (*B) / minOverMax; *R = (*B) + H * ((*G) - (*B));
				}
				else if (H < 3./ 6.)
				{   //  G>B>R
					H = 6.* (H - 2./ 6.); part = 1.+ H * (1./ minOverMax - 1.);
					*R = P / sqrt(Pg / minOverMax / minOverMax + Pb * part * part + Pr);
					*G = (*R) / minOverMax; *B = (*R) + H * ((*G) - (*R));
				}
				else if (H < 4./ 6.)
				{   //  B>G>R
					H = 6.* (-H + 4./ 6.); part = 1.+ H * (1./ minOverMax - 1.);
					*R = P / sqrt(Pb / minOverMax / minOverMax + Pg * part * part + Pr);
					*B = (*R) / minOverMax; *G = (*R) + H * ((*B) - (*R));
				}
				else if (H < 5./ 6.)
				{   //  B>R>G
					H = 6.* (H - 4./ 6.); part = 1.+ H * (1./ minOverMax - 1.);
					*G = P / sqrt(Pb / minOverMax / minOverMax + Pr * part * part + Pg);
					*B = (*G) / minOverMax; *R = (*G) + H * ((*B) - (*G));
				}
				else
				{   //  R>B>G
					H = 6.* (-H + 6./ 6.); part = 1.+ H * (1./ minOverMax - 1.);
					*G = P / sqrt(Pr / minOverMax / minOverMax + Pb * part * part + Pg);
					*R = (*G) / minOverMax; *B = (*G) + H * ((*R) - (*G));
				}
			}
			else
			{
				if (H < 1./ 6.)
				{   //  R>G>B
					H = 6.* (H - 0./ 6.); *R = sqrt(P * P / (Pr + Pg * H * H)); *G = (*R) * H; *B = 0.;
				}
				else if (H < 2./ 6.)
				{   //  G>R>B
					H = 6.* (-H + 2./ 6.); *G = sqrt(P * P / (Pg + Pr * H * H)); *R = (*G) * H; *B = 0.;
				}
				else if (H < 3./ 6.)
				{   //  G>B>R
					H = 6.* (H - 2./ 6.); *G = sqrt(P * P / (Pg + Pb * H * H)); *B = (*G) * H; *R = 0.;
				}
				else if (H < 4./ 6.)
				{   //  B>G>R
					H = 6.* (-H + 4./ 6.); *B = sqrt(P * P / (Pb + Pg * H * H)); *G = (*B) * H; *R = 0.;
				}
				else if (H < 5./ 6.)
				{   //  B>R>G
					H = 6.* (H - 4./ 6.); *B = sqrt(P * P / (Pb + Pr * H * H)); *R = (*B) * H; *G = 0.;
				}
				else
				{   //  R>B>G
					H = 6.* (-H + 6./ 6.); *R = sqrt(P * P / (Pr + Pb * H * H)); *B = (*R) * H; *G = 0.;
				}
			}
		}

		*/

		#endregion
	}
}
