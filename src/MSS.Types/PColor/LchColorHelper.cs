using System;
using System.Diagnostics;

namespace MSS.Types.PColor
{
	public class LchColorHelper
	{
		// From https://codeberg.org/zeileis/colorspace/src/branch/main/src/colorspace.c
		//0.412453, 0.357580, 0.180423
		//0.212671, 0.715160, 0.072169
		//0.019334, 0.119193, 0.950227

		//3.240479, -1.537150, - 0.498535
		//-0.969256, 1.875992, 0.041556
		//0.055648, -0.204043, 1.057311

		//public static XyzMatrix RGB_TO_XYZ = new XyzMatrix(new double[] { 0.412453, 0.357580, 0.180423 }, new double[] { 0.212671, 0.715160, 0.072169 }, new double[] { 0.019334, 0.119193, 0.950227 });
		//public static XyzMatrix XYZ_TO_RGB = new XyzMatrix(new double[] { 3.240479, -1.537150, - 0.498535 }, new double[] { -0.969256, 1.875992, 0.041556 }, new double[] { 0.055648, -0.204043, 1.057311 });


		// RGB from http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html
		//0.4124564,  0.3575761,  0.1804375
		//0.2126729,  0.7151522,  0.0721750
		//0.0193339,  0.1191920,  0.9503041

		//3.2404542, -1.5371385, -0.4985314
		//-0.9692660,  1.8760108,  0.0415560
		//0.0556434, -0.2040259,  1.0572252
		public static XyzMatrix RGB_TO_XYZ = new XyzMatrix(new double[] { 0.4124564, 0.3575761, 0.1804375 }, new double[] { 0.2126729, 0.7151522, 0.0721750 }, new double[] { 0.0193339,  0.1191920,  0.9503041 });
		public static XyzMatrix XYZ_TO_RGB = new XyzMatrix(new double[] { 3.2404542, -1.5371385, -0.4985314 }, new double[] { -0.9692660, 1.8760108, 0.0415560 }, new double[] { 0.0556434, -0.2040259, 1.0572252 });

		//0.5767309,  0.1855540,  0.1881852
		//0.2973769,  0.6273491,  0.0752741
		//0.0270343,  0.0706872,  0.9911085

		//2.0413690, -0.5649464, -0.3446944
		//-0.9692660, 1.8760108,  0.0415560
		//0.0134474, -0.1183897,  1.0154096
		public static XyzMatrix ADOBE_RGB_TO_XYZ = new XyzMatrix(new double[] { 0.5767309, 0.1855540, 0.1881852 }, new double[] { 0.2973769, 0.6273491, 0.0752741 }, new double[] { 0.0270343, 0.0706872, 0.9911085 });
		public static XyzMatrix XYZ_TO_ADOBE_RGB = new XyzMatrix(new double[] { 2.0413690, -0.5649464, -0.3446944 }, new double[] { -0.9692660, 1.8760108, 0.0415560 }, new double[] { 0.0134474, -0.1183897, 1.0154096 });

		//0.7976749,  0.1351917,  0.0313534
		//0.2880402,  0.7118741,  0.0000857
		//0.0000000,  0.0000000,  0.8252100

		//1.3459433, -0.2556075, -0.0511118
		//-0.5445989,  1.5081673,  0.0205351
		//0.0000000,  0.0000000,  1.2118128

		public static XyzMatrix PRO_PHOTO_TO_XYZ = new XyzMatrix(new double[] { 0.7976749, 0.1351917, 0.0313534 }, new double[] { 0.2880402, 0.7118741, 0.0000857 }, new double[] { 0.0000000, 0.0000000, 0.8252100 });
		public static XyzMatrix XYZ_TO_PRO_PHOTO = new XyzMatrix(new double[] { 1.3459433, -0.2556075, -0.0511118 }, new double[] { -0.5445989, 1.5081673, 0.0205351 }, new double[] { 0.0000000, 0.0000000, 1.2118128 });


		public static Xyz D50_WHITE_POINT = new Xyz(0.964220, 1.0000, 0.825210);
		public static Xyz D55_WHITE_POINT = new Xyz(0.956820, 1.0000, 0.921490);
		public static Xyz D65_WHITE_POINT = new Xyz(0.950470, 1.0000, 1.088830);

		public static double CIE_EPSILON = 216d / 24389d; //0.008856;
		public static double CIE_KAPPA = 24389d / 27d; //903.3;

		public static Xyz RgbToXyz(Rgb c, XyzMatrix t)
		{
			double x = c.red * t.Value[0, 0] + c.green * t.Value[0, 1] + c.blue * t.Value[0, 2];
			double y = c.red * t.Value[1, 0] + c.green * t.Value[1, 1] + c.blue * t.Value[1, 2];
			double z = c.red * t.Value[2, 0] + c.green * t.Value[2, 1] + c.blue * t.Value[2, 2];

			var result = new Xyz(x, y, z);

			return result;
		}

		public static Rgb XyzToRgb(Xyz c, XyzMatrix t)
		{
			double r = c.X * t.Value[0, 0] + c.Y * t.Value[0, 1] + c.Z * t.Value[0, 2];
			double g = c.X * t.Value[1, 0] + c.Y * t.Value[1, 1] + c.Z * t.Value[1, 2];
			double b = c.X * t.Value[2, 0] + c.Y * t.Value[2, 1] + c.Z * t.Value[2, 2];

			var result = new Rgb(r, g, b);

			return result;
		}

		public static Luv XyzToLuv(Xyz xyz, Xyz whitePoint)
		{
			var yR = xyz.Y / whitePoint.Y;
			double l;

			if (yR > CIE_EPSILON)
			{
				l = Math.Pow(yR, 1d/3d);
				l *= 116;
				l -= 16;
			}
			else
			{
				l = yR * CIE_KAPPA;
			}

			XYZ_to_uv(xyz, out double uP, out double vP);
			XYZ_to_uv(whitePoint, out double uPr, out double vPr);

			var u = 13 * l * (uP - uPr);
			var v = 13 * l * (vP - vPr);

			Luv result = new Luv(l, u, v);

			return result;
		}

		public static Xyz LuvToXyz(Luv luv, Xyz whitePoint)
		{
			Xyz result;

			if (luv.L <= 0 && luv.U == 0 & luv.V == 0)
			{
				result = new Xyz(0, 0, 0);
			}
			else
			{
				// CIE_EPSILON (216 / 24389) divided by CIE_KAPPA (24389 / 27) = 216/27 = 8
				double y = luv.L > 8 ? Math.Pow((luv.L + 16) / 116, 3) : luv.L / CIE_KAPPA;

				XYZ_to_uv(whitePoint, out double uPr, out double vPr);

				double uP, vP;

				if (luv.L == 0)
				{
					uP = uPr;
					vP = vPr;
				}
				else
				{
					uP = luv.U / (13 * luv.L) + uPr;
					vP = luv.V / (13 * luv.L) + vPr;
				}

				var x = 9.0 * y * uP / (4 * vP);
				var z = -x / 3 - 5 * y + 3 * y / vP;

				result = new Xyz(x,y,z);
			}

			return result;
		}

		private static void XYZ_to_uv(Xyz xyz, out double u, out double v)
		{
			double t, x, y;

			t = xyz.X + xyz.Y + xyz.Z;

			if (t == 0)
			{
				x = 0;
				y = 0;
			}
			else
			{
				x = xyz.X / t;
				y = xyz.Y / t;
			}

			u = 2 * x / (6 * y - x + 1.5);
			v = 4.5 * y / (6 * y - x + 1.5);
		}

		public static Lch LuvToLch(Luv luv)
		{
			var c = Math.Sqrt(luv.U * luv.U + luv.V * luv.V);
			var hRad = Math.Atan2(luv.V, luv.U);
			var hDeg = hRad * (180 / Math.PI);

			while (hDeg > 360) hDeg -= 360;
			while (hDeg < 0) hDeg += 360;

			var result = new Lch(luv.L, c, hDeg);

			return result;
		}

		public static Luv LchToLuv(Lch lch)
		{
			var hRad = lch.H * (Math.PI / 180);

			var u = lch.C * Math.Cos(hRad);
			var v = lch.C * Math.Sin(hRad);

			var result = new Luv(lch.L, u, v);

			return result;
		}

		public static Lch RgbToLch(Rgb rgb)
		{
			var xyz = RgbToXyz(rgb, PRO_PHOTO_TO_XYZ);
			var luv = XyzToLuv(xyz, D50_WHITE_POINT);
			var lch = LuvToLch(luv);

			return lch;
		}

		public static Rgb LchToRgb(Lch lch)
		{
			var luv = LchToLuv(lch);
			var xyz = LuvToXyz(luv, D50_WHITE_POINT);
			var rgb = XyzToRgb(xyz, XYZ_TO_PRO_PHOTO);

			return rgb;
		}

		public static Lch RgbToLch(Rgb rgb, XyzMatrix t, Xyz whitePoint)
		{
			var xyz = RgbToXyz(rgb, t);
			var luv = XyzToLuv(xyz, whitePoint);
			var lch = LuvToLch(luv);

			return lch;
		}

		public static Rgb LchToRgb(Lch lch, XyzMatrix t, Xyz whitePoint)
		{
			var luv = LchToLuv(lch);
			var xyz = LuvToXyz(luv, whitePoint);
			var rgb = XyzToRgb(xyz, t);

			return rgb;
		}

	}
}
