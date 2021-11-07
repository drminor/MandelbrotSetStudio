using MSS.Types;
using MSS.Types.Base;
using MSS.Types.MSetDatabase;
using System;
using System.Linq;

namespace MSS.Common.MSetDatabase
{
	public static class CoordsHelper
	{
		public static Coords BuildCoords(RRectangle rRectangle)
		{
			var display = GetDisplay(rRectangle);
			var valueDepth = GetValueDepth(rRectangle);
			var result = new Coords(display, rRectangle, valueDepth);

			return result;
		}

		private static string GetDisplay(RRectangle rRectangle)
		{
			double scaleFactor = Math.Pow(2, rRectangle.Exponent);
			double denominator = 1d / scaleFactor;
			string strDenominator = denominator.ToString();

			var dRectangle = CreateFrom(rRectangle);
			dRectangle = dRectangle.Scale(scaleFactor);

			Rectangle<StringStruct> strVals = new Rectangle<StringStruct>(
				rRectangle.Values.Select((x,i) => 
				new StringStruct(x.ToString() + "/" + strDenominator + " (" + dRectangle.Values[i].ToString() + ")")).ToArray()
				);

			string display = strVals.ToString();
			return display;
		}

		private static DRectangle CreateFrom(RRectangle rRectangle)
		{
			return new DRectangle(rRectangle.Values);
		}

		private static int GetValueDepth(RRectangle _)
		{
			// TODO: Calculate the # of maximum binary bits of precision from sx, ex, sy and ey.
			int binaryBitsOfPrecision = 10;
			int valueDepth = CalculateValueDepth(binaryBitsOfPrecision);

			return valueDepth;
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
