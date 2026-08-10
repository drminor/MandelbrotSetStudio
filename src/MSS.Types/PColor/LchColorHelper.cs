using System;

namespace MSS.Types.PColor
{
	public class LchColorHelper
	{
		public static XyzMatrix RGB_TO_XYZ = new XyzMatrix(new float[] { 0.412453f, 0.357580f, 0.180423f }, new float[] { 0.212671f, 0.715160f, 0.072169f }, new float[] { 0.019334f, 0.119193f, 0.950227f });

		public static XyzMatrix XYZ_TO_RGB = new XyzMatrix(new float[] { 3.2404542f, -1.5371385f, -0.4985314f }, new float[] { -0.9692660f, 1.8760108f, 0.0415560f }, new float[] { 0.0556434f, -0.2040259f, 1.0572252f });

		public static ColorVal D65_WHITE_POINT = new ColorVal(0.9504f, 1.0000f, 1.0888f);

		public static ColorVal Multiply(ColorVal c, XyzMatrix t)
		{
			float x = c.red * t.Value[0, 0] + c.green * t.Value[0, 1] + c.blue * t.Value[0, 2];
			float y = c.red * t.Value[1, 0] + c.green * t.Value[1, 1] + c.blue * t.Value[1, 2];
			float z = c.red * t.Value[2, 0] + c.green * t.Value[2, 1] + c.blue * t.Value[2, 2];

			var result = new ColorVal(x, y, z);

			return result;
		}


		public static float CIE_E = 0.008856f;
		public static float CIE_K = 903.3f;

		public static ColorVal ConvertXyzToLuv(ColorVal xyz, ColorVal whitePoint)
		{

			var yR = xyz.green / whitePoint.green;
			float l;

			if (yR > CIE_E)
			{
				var oneThird = 1d / 3d;
				l = (float)Math.Pow(yR, oneThird);
				l *= 116;
				l -= 16;
			}
			else
			{
				l = yR * CIE_K;
			}

			var uP = 4 * xyz.red / (xyz.red + 15 * xyz.green + 3 * xyz.blue);
			var uPR = 4 * whitePoint.red / (whitePoint.red + 15 * whitePoint.green + 3 * whitePoint.blue);

			var vP = 9 * xyz.green / (xyz.red + 15 * xyz.green + 3 * xyz.blue);
			var vPR = 9 * whitePoint.green / (whitePoint.red + 15 * whitePoint.green + 3 * whitePoint.blue);

			var u = 13 * l * (uP - uPR);
			var v = 13 * l * (vP - vPR);


			ColorVal result = new ColorVal(l, u, v);


			return result;
		}


	}
}
