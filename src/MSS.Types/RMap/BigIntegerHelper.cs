using System;
using System.Numerics;

namespace MSS.Types
{
	public static class BigIntegerHelper
	{
		private readonly static BigInteger factor = new BigInteger(Math.Pow(2, 53));

		public static long[] ToLongs(BigInteger bi)
		{
			var hi = BigInteger.DivRem(bi, factor, out var lo);

			if (hi > factor)
			{
				throw new ArgumentOutOfRangeException(nameof(bi));
			}

			var result = new long[] { (long)hi, (long)lo };

			return result;
		}

		public static double GetValue(BigInteger n, int exponent)
		{
			if (!SafeCastToDouble(n))
			{
				throw new OverflowException($"It is not safe to cast BigInteger: {n} to a double.");
			}

			var hiAndLo = ToLongs(n);
			double result = hiAndLo[0] + hiAndLo[1];
			result = Math.ScaleB(result, exponent);

			if (!DoubleHelper.HasPrecision(result))
			{
				throw new OverflowException($"When converting BigInteger: {n} to a double, precision was lost.");
			}

			return result;
		}

		private static bool SafeCastToDouble(BigInteger n)
		{
			var s_bnDoubleMinValue = (BigInteger)double.MinValue;
			var s_bnDoubleMaxValue = (BigInteger)double.MaxValue;
			return s_bnDoubleMinValue <= n && n <= s_bnDoubleMaxValue;
		}


	}


}
