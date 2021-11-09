using MSS.Types;
using MSS.Types.Base;
using MSS.Types.MSetDatabase;
using System;
using System.Linq;
using System.Numerics;

namespace MSS.Common.MSetDatabase
{
	public static class BCoordsHelper
	{
		public static BCoords BuildCoords(BRectangle bRectangle)
		{
			var display = GetDisplay(bRectangle);
			var valueDepth = GetValueDepth(bRectangle);
			var result = new BCoords(display, new BCoordsPoints(bRectangle), valueDepth);

			return result;
		}

		public static BRectangle BuildBRectangle(BCoordsPoints bCoordsPoints)
		{
			return new BRectangle(bCoordsPoints.GetValues().Select(v => new BigInteger(v)).ToArray(), bCoordsPoints.Exponent);
		}


		private static string GetDisplay(BRectangle bRectangle)
		{
			double scaleFactor = Math.Pow(2, bRectangle.Exponent);
			double denominator = 1d / scaleFactor;
			string strDenominator = denominator.ToString();

			var dRectangle = new DRectangle(bRectangle.Values);
			dRectangle = dRectangle.Scale(scaleFactor);

			Rectangle<StringStruct> strVals = new Rectangle<StringStruct>(
				bRectangle.Values.Select((x,i) => 
				new StringStruct(x.ToString() + "/" + strDenominator + " (" + dRectangle.Values[i].ToString() + ")")).ToArray()
				);

			string display = strVals.ToString();
			return display;
		}

		private static int GetValueDepth(BRectangle _)
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
		public static BRectangle ConvertFrom(ApCoords _)
		{
			var result = new BRectangle();

			return result;
		}

	}
}
