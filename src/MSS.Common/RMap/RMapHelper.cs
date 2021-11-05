using MSS.Types;
using System;

namespace MSS.Common
{
	public static class RMapHelper
	{
		public static RRectangle Zoom(RRectangle rRectangle)
		{
			long xLen = rRectangle.ExN - rRectangle.SxN;
			long quarterOfxLen = Divide4(xLen, rRectangle.Exp, out uint newXExp);
			long newSxN = rRectangle.SxN + quarterOfxLen;
			long newExN = rRectangle.ExN + 3 * quarterOfxLen;

			long yLen = rRectangle.ExN - rRectangle.SxN;
			long quarterOfyLen = Divide4(yLen, rRectangle.Exp, out uint newYExp);
			long newSyN = rRectangle.SyN + quarterOfyLen;
			long newEyN = rRectangle.EyN + 3 * quarterOfyLen;

			uint newExp = GetCommonExp(newXExp, newYExp);

			newSxN = Rebase(newSxN, newXExp, newExp);
			newExN = Rebase(newExN, newXExp, newExp);
			newSyN = Rebase(newSyN, newYExp, newExp);
			newEyN = Rebase(newEyN, newYExp, newExp);

			RRectangle result = new RRectangle(newSxN, newExN, newSyN, newEyN, newExp);

			return result;
		}

		public static long Divide4(long n, uint dp, out uint newDp)
		{
			long result;

			long t = Math.DivRem(n, 4, out long remainder);

			if (remainder == 0)
			{
				result = t;
				newDp = dp;
			}
			else if (remainder == 1)
			{
				result = (t * 4) + 1;
				newDp = dp * 4;
			}
			else if (remainder == 2)
			{
				result = (t * 2) + 1;
				newDp = dp * 2;
			}
			else
			{
				result = (t * 4) + 3;
				newDp = dp * 4;
			}

			return result;

			//double t = n / (double)divisor;

			//if (Mod)
		}

		public static uint GetCommonExp(uint expX, uint expY)
		{
			return Math.Max(expX, expY);
		}

		public static long Rebase(long n, uint currentExp, uint newExp)
		{
			long result;

			if (newExp > currentExp)
			{
				result = n * 2 * (newExp - currentExp);
			}
			else if(newExp < currentExp)
			{
				result = n / 2 * (currentExp - newExp);
			}
			else
			{
				result = n;
			}

			return result;
		}


		/*
		 * 5 / 4 = 1 + 1/4 or (4 + 1) / 16
		 * 
		 * 
		 * 
		 */ 
	}
}
