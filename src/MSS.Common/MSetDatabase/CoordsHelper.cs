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

			double denominator = Math.Pow(2, rRectangle.Exp);
			string strDenominator = denominator.ToString();

			// TODO: Add method to check for underflow when converting a 'RPoint' component to a double.
			double sx = rRectangle.SxN / denominator;
			double ex = rRectangle.ExN / denominator;
			double sy = rRectangle.SyN / denominator;
			double ey = rRectangle.EyN / denominator;

			result[0] = $"{rRectangle.SxN}/{strDenominator} ({sx})";
			result[1] = $"{rRectangle.ExN}/{strDenominator} ({ex})";
			result[2] = $"{rRectangle.SyN}/{strDenominator} ({sy})";
			result[3] = $"{rRectangle.EyN}/{strDenominator} ({ey})";

			// TODO: Calculate the # of maximum binary bits of precision from sx, ex, sy and ey.
			int binaryBitsOfPrecision = 10;
			valueDepth = CalculateValueDepth(binaryBitsOfPrecision);

			return result;
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
