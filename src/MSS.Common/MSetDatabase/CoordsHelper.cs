using MSS.Types;
using MSS.Types.MSetDatabase;
using System;

namespace MSS.Common.MSetDatabase
{
	public static class CoordsHelper
	{
		public static Coords BuildCoords(long sx, long ex, long sy, long ey, int exp)
		{
			Coords result = BuildCoords(new RRectangle(sx, ex, sy, ey, exp));
			return result;
		}

		public static Coords BuildCoords(RRectangle rRectangle)
		{
			string[] stringVals = GetStringVals(rRectangle, out int valueDepth);
			Coords result = new Coords(stringVals, rRectangle.SxN, rRectangle.ExN, rRectangle.SyN, rRectangle.EyN, rRectangle.Exp, valueDepth);

			return result;
		}

		private static string[] GetStringVals(RRectangle rRectangle, out int valueDepth)
		{
			string[] result = new string[4];

			double scaleFactor = Math.Pow(2, rRectangle.Exp);
			double denominator = 1d / scaleFactor;
			string strDenominator = denominator.ToString();

			var dRectangle = CreateFrom(rRectangle);
			dRectangle = dRectangle.Scale(scaleFactor);

			result[0] = $"{rRectangle.SxN}/{strDenominator} ({dRectangle.Sx})";
			result[1] = $"{rRectangle.ExN}/{strDenominator} ({dRectangle.Ex})";
			result[2] = $"{rRectangle.SyN}/{strDenominator} ({dRectangle.Sy})";
			result[3] = $"{rRectangle.EyN}/{strDenominator} ({dRectangle.Ey})";

			// TODO: Calculate the # of maximum binary bits of precision from sx, ex, sy and ey.
			int binaryBitsOfPrecision = 10;
			valueDepth = CalculateValueDepth(binaryBitsOfPrecision);

			return result;
		}

		private static DRectangle CreateFrom(RRectangle rRectangle)
		{
			return new DRectangle(rRectangle.SxN, rRectangle.ExN, rRectangle.SyN, rRectangle.EyN);
		}

		private static int CalculateValueDepth(int binaryBitsOfPrecision)
		{
			int result = Math.DivRem(binaryBitsOfPrecision, 53, out int remainder);

			if (remainder > 0) result++;

			return result;
		}

		// TODO: Fix the ConvertFrom method that takes an ApCoords object and produces a Coords object.
		public static Coords CovertFrom(ApCoords apCoords)
		{
			var result = new Coords();

			return result;
		}

	}
}
