using System;
using System.Numerics;

namespace MSS.Types
{
	public static class BigIntegerHelperOldMethods
	{
		private static readonly BigInteger DOUBLE_MIN_VALUE = (BigInteger)double.MinValue;
		private static readonly BigInteger DOUBLE_MAX_VALUE = (BigInteger)double.MaxValue;

		// Largest integer that can be represented by a double for which it and all smaller integers can be reduced by 1 without loosing precision.
		private static readonly BigInteger FACTOR = new BigInteger(Math.Pow(2, 53));

		public static double GetRatio(BigInteger dividend, int divisor)
		{
			if (divisor == 0)
			{
				throw new DivideByZeroException();
			}

			if (dividend == 0)
			{
				return 0;
			}

			double result;

			if (SafeCastToDouble(dividend))
			{
				var workingDividend = ConvertToDouble(dividend);
				result = workingDividend / divisor;
			}
			else
			{
				var hiAndLo = ToLongs(dividend);

				checked
				{
					result = hiAndLo[0] / divisor * Math.Pow(2, 53);
					result += hiAndLo[1] / divisor;
				}
			}

			return result;
		}

		public static long[] ToLongs(BigInteger bi)
		{
			var hi = BigInteger.DivRem(bi, FACTOR, out var lo);

			if (hi > FACTOR)
			{
				throw new ArgumentOutOfRangeException(nameof(bi));
			}

			var result = new long[] { (long)hi, (long)lo };

			return result;
		}

		private static void CheckLogBase2Result(BigInteger n, int exponent, int ourResult)
		{
			if (TryConvertToDouble(n, exponent, out var val))
			{
				var correctResult = (int)Math.Round(Math.Log2(val));

				if (ourResult != correctResult)
				{
					throw new InvalidOperationException($"Our calculation of LogB2 of the BigInteger: {n} * 2 to the {exponent} power is incorrect. OurResult = {ourResult}, correct result = {correctResult}");
				}
			}
			else
			{
				throw new NotImplementedException("We cannot get LogBase2 of BigIntegers larger than Double.Max.");
			}
		}

		public static bool TryConvertToDouble(RValue n, out double value)
		{
			return TryConvertToDouble(n.Value, n.Exponent, out value);
		}

		public static bool TryConvertToDouble(BigInteger n, int exponent, out double value)
		{
			if (n == 0)
			{
				value = 0;
				return true;
			}

			if (SafeCastToDouble(n))
			{
				checked
				{
					value = (double)n;
					value *= Math.Pow(2, exponent);
				}
			}
			else
			{
				var hiAndLo = ToLongs(n);

				checked
				{
					value = hiAndLo[0] * Math.Pow(2, exponent + 53);
					value += hiAndLo[1] * Math.Pow(2, exponent);
				}
			}

			return DoubleHelper.HasPrecision(value);
		}


		public static double ConvertToDouble(BigInteger n)
		{
			if (!SafeCastToDouble(n))
			{
				throw new OverflowException($"It is not safe to cast BigInteger: {n} to a double.");
			}

			var result = (double)n;
			return result;
		}

		public static bool SafeCastToDouble(BigInteger n)
		{
			//bool result = DOUBLE_MIN_VALUE <= n && n <= DOUBLE_MAX_VALUE;
			bool result = BigInteger.Abs(n) <= FACTOR;

			return result;
		}

	}
}
