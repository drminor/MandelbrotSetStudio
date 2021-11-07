using MSS.Types;
using MSS.Types.MSetDatabase;
using System;
using System.Diagnostics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		public static RRectangle Zoom(RRectangle rRectangle)
		{
			long quarterOfxLen = Divide4(rRectangle.WidthNumerator, rRectangle.Exponent, out int newXExp);

			//long newX1 = rRectangle.X1 + quarterOfxLen;
			//long newX2 = rRectangle.X2 + 3 * quarterOfxLen;

			long newX1 = Rebase(rRectangle.X1, rRectangle.X2, rRectangle.Exponent, newXExp, out long newX2);

			double q = GetVal(quarterOfxLen, newXExp);
			double stX = GetVal(newX1, newXExp);
			double enX = GetVal(newX2, newXExp);

			newX1 += quarterOfxLen;
			newX1 += 3 * quarterOfxLen;

			stX = GetVal(newX1, newXExp);
			enX = GetVal(newX2, newXExp);

			long quarterOfyLen = Divide4(rRectangle.HeigthNumerator, rRectangle.Exponent, out int newYExp);

			//long newY1 = rRectangle.Y1 + quarterOfyLen;
			//long newY2 = rRectangle.Y2 + 3 * quarterOfyLen;

			long newY1 = Rebase(rRectangle.X1, rRectangle.Y2, rRectangle.Exponent, newYExp, out long newY2);
			newY1 += quarterOfyLen;
			newY2 += 3 * quarterOfyLen;

			int commonExp = GetCommonExp(newXExp, newYExp);

			newX1 = Rebase(newX1, newX2, newXExp, commonExp, out newX2);
			newY1 = Rebase(newY1, newY2, newYExp, commonExp, out newY2);

			RRectangle result = new RRectangle(newX1, newX2, newY1, newY2, commonExp);

			return result;
		}

		public static long Divide4(long n, int exp, out int newExp)
		{
			long result;

			long t = Math.DivRem(n, 4, out long remainder);

			if (remainder == 0)
			{
				result = t;
				newExp = exp;
			}
			else if (remainder == 1)
			{
				result = (t * 4) + 1;
				newExp = exp - 2;
			}
			else if (remainder == 2)
			{
				result = (t * 2) + 1;
				newExp = exp - 1;
			}
			else
			{
				result = (t * 4) + 3;
				newExp = exp - 2;
			}

			return result;
		}

		private static int GetCommonExp(int expX, int expY)
		{
			return Math.Min(expX, expY);
		}

		private static long Rebase(long x, long y, int currentExp, int newExp, out long newY)
		{
			long result;

			if (newExp > currentExp)
			{
				long multplier = (long) Math.Pow(newExp - currentExp, 2);
				result = x * multplier;
				newY = y * multplier;
			}
			else if(newExp < currentExp)
			{
				int diff = newExp - currentExp;
				double divisor = Math.Pow(2, diff);
				//CheckRemainder(x, divisor);
				//CheckRemainder(y, divisor);
				result = (long) (x / divisor);
				newY = (long) (y / divisor);
			}
			else
			{
				result = x;
				newY = y;
			}

			return result;
		}

		[Conditional("DEBUG")]
		private static void CheckRemainder(long dividend, long divisor)
		{
			Math.DivRem(dividend, divisor, out long remainder);
			Debug.Assert(remainder == 0);
		}

		private static double GetVal(long n, int e)
		{
			double scaleFactor = Math.Pow(2, e);
			double result = n * scaleFactor;
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
