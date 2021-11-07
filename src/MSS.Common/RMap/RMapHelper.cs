using MSS.Types;
using System;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public static class RMapHelper
	{
		/// <summary>
		/// Divide the given rectangle into 16 squares and then return the coordinates of the "middle" 4 squares.
		/// </summary>
		/// <param name="rRectangle"></param>
		/// <returns>A new RRectangle</returns>
		public static RRectangle Zoom(RRectangle rRectangle)
		{
			//DIAG -- double x0n = GetVal(rRectangle.X1, rRectangle.Exponent);

			// Here are the current values for the rectangle's Width and Height numerators
			long curWidth = rRectangle.WidthNumerator;
			long curHeight = rRectangle.HeigthNumerator;

			// Create a new rectangle with its exponent adjusted to support the new precision required in the numerators.
			RRectangle rectangleWithNewExp;

			var quarterOfXLen = Math.DivRem(curWidth, 4, out long remainderX);
			var quarterOfYLen = Math.DivRem(curHeight, 4, out long remainderY);

			if (remainderX == 0 && remainderY == 0)
			{
				// The exponent does not need to be changed, the quarter values are naturaly whole numbers
				rectangleWithNewExp = rRectangle;
			}
			else
			{
				var halfOfXLen = Math.DivRem(curWidth, 2, out remainderX);
				var halfOfYLen = Math.DivRem(curHeight, 2, out remainderY);

				if (remainderX == 0 && remainderY == 0)
				{
					// The exponent needs to be reduced by 1 (all values are halved)
					rectangleWithNewExp = new RRectangle(Rebase(rRectangle.Values, -1), rRectangle.Exponent - 1);

					// The half values are now = 1/4 of the original width
					quarterOfXLen = halfOfXLen;
					quarterOfYLen = halfOfYLen;
				}
				else
				{
					// The exponent needs to be reduced by 2 (all values are quartered)
					rectangleWithNewExp = new RRectangle(Rebase(rRectangle.Values, -2), rRectangle.Exponent - 2);

					// The current values can be used
					quarterOfXLen = curWidth;
					quarterOfYLen = curHeight;
				}
			}

			//DIAG double x1n = GetVal(rebased.X1, rebased.Exponent);

			RRectangle result = new RRectangle(
				rectangleWithNewExp.X1 + quarterOfXLen,
				rectangleWithNewExp.X1 + 3 * quarterOfXLen,
				rectangleWithNewExp.Y1 + quarterOfYLen,
				rectangleWithNewExp.Y1 + 3 * quarterOfYLen,
				rectangleWithNewExp.Exponent
				);

			return result;
		}

		//public static long Divide4(long n, int exp, out int newExp)
		//{
		//	long result;

		//	long t = Math.DivRem(n, 4, out long remainder);

		//	if (remainder == 0)
		//	{
		//		result = t;
		//		newExp = exp;
		//	}
		//	else if (remainder == 1)
		//	{
		//		result = (t * 4) + 1;
		//		newExp = exp - 2;
		//	}
		//	else if (remainder == 2)
		//	{
		//		result = (t * 2) + 1;
		//		newExp = exp - 1;
		//	}
		//	else
		//	{
		//		result = (t * 4) + 3;
		//		newExp = exp - 2;
		//	}

		//	return result;
		//}

		private static long[] Rebase(long[] vals, int exponentDelta)
		{
			if (exponentDelta == 0)
			{
				return vals;
			}

			long multplier = (long)Math.Pow(2, -1 * exponentDelta);
			long[] result = vals.Select(v => v * multplier).ToArray();

			return result;
		}

		//[Conditional("DEBUG")]
		//private static void CheckRemainder(long dividend, long divisor)
		//{
		//	Math.DivRem(dividend, divisor, out long remainder);
		//	Debug.Assert(remainder == 0);
		//}

		private static double GetVal(long n, int e)
		{
			var result = Math.ScaleB(n, e);
			return result;
		}

		/*
		 * (5/4) / 4 =
		 * = (1 + 1/4) / 4
		 * = 4 * (1 + 1/4) / 16
		 * = (4 + 1) / 16					
		 * = 5 / 16
		 *
		 */
	}
}
