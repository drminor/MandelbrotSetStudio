using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.Base;
using ProjectRepo.Entities;
using System;
using System.Linq;

namespace ProjectRepo
{
	public static class CoordsHelper
	{
		public static RRectangleRecord BuildCoords(RRectangle rRectangle)
		{
			var display = GetDisplay(rRectangle);
			//var valueDepth = GetValueDepth(rRectangle);
			var rRectangleDto = new DtoMapper().MapTo(rRectangle);
			var result = new RRectangleRecord(display, rRectangleDto);

			return result;
		}



		private static string GetDisplay(RRectangle rRectangle)
		{
			double scaleFactor = Math.Pow(2, rRectangle.Exponent);
			double denominator = 1d / scaleFactor;
			string strDenominator = denominator.ToString();

			var dRectangle = new DRectangle(rRectangle.Values);
			dRectangle = dRectangle.Scale(scaleFactor);

			Rectangle<StringStruct> strVals = new Rectangle<StringStruct>(
				rRectangle.Values.Select((x,i) => 
				new StringStruct(x.ToString() + "/" + strDenominator + " (" + dRectangle.Values[i].ToString() + ")")).ToArray()
				);

			string display = strVals.ToString();
			return display;
		}

		//private static int GetValueDepth(RRectangle _)
		//{
		//	// TODO: Calculate the # of maximum binary bits of precision from sx, ex, sy and ey.
		//	int binaryBitsOfPrecision = 10;
		//	int valueDepth = CalculateValueDepth(binaryBitsOfPrecision);

		//	return valueDepth;
		//}

		//private static int CalculateValueDepth(int binaryBitsOfPrecision)
		//{
		//	int result = Math.DivRem(binaryBitsOfPrecision, 53, out int remainder);

		//	if (remainder > 0) result++;

		//	return result;
		//}

		// TODO: Fix the ConvertFrom method that takes an ApCoords object and produces a Coords object.
		public static RRectangle ConvertFrom(ApCoords _)
		{
			var result = new RRectangle();

			return result;
		}

	}
}
