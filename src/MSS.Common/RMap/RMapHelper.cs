using MSS.Types;
using System;
using System.Diagnostics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		public static RRectangle Zoom(RRectangle rRectangle)
		{
			long xLen = rRectangle.ExN - rRectangle.SxN;
			long quarterOfxLen = Divide4(xLen, rRectangle.Exp, out int newXExp);
			long newSxN = rRectangle.SxN + quarterOfxLen;
			long newExN = rRectangle.ExN + 3 * quarterOfxLen;

			long yLen = rRectangle.ExN - rRectangle.SxN;
			long quarterOfyLen = Divide4(yLen, rRectangle.Exp, out int newYExp);
			long newSyN = rRectangle.SyN + quarterOfyLen;
			long newEyN = rRectangle.EyN + 3 * quarterOfyLen;

			int commonExp = GetCommonExp(newXExp, newYExp);

			newSxN = Rebase(newSxN, newExN, newXExp, commonExp, out newExN);
			newSyN = Rebase(newSyN, newEyN, newYExp, commonExp, out newEyN);

			RRectangle result = new RRectangle(newSxN, newExN, newSyN, newEyN, commonExp);

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

		public static int GetCommonExp(int expX, int expY)
		{
			return Math.Min(expX, expY);
		}

		public static long Rebase(long x, long y, int currentExp, int newExp, out long newY)
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
				long divisor = (long)Math.Pow(currentExp - newExp, 2);
				CheckRemainder(x, divisor);
				CheckRemainder(y, divisor);
				result = x / divisor;
				newY = y / divisor;
			}
			else
			{
				result = x;
				newY = y;
			}

			return result;
		}

		[Conditional("DEBUG")]
		public static void CheckRemainder(long dividend, long divisor)
		{
			Math.DivRem(dividend, divisor, out long remainder);
			Debug.Assert(remainder == 0);
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
