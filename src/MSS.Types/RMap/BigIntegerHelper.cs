using System;
using System.Diagnostics;
using System.Numerics;

namespace MSS.Types
{
	public static class BigIntegerHelper
	{
		private static readonly BigInteger DOUBLE_MIN_VALUE = (BigInteger)double.MinValue;
		private static readonly BigInteger DOUBLE_MAX_VALUE = (BigInteger)double.MaxValue;

		private static readonly double NAT_LOG_OF_2 = Math.Log(2);

		// Largest integer that can be represented by a double for which it and all smaller integers can be reduced by 1 without loosing precision.
		private static readonly BigInteger FACTOR = new BigInteger(Math.Pow(2, 53));

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

		#region Division

		public static BigInteger Divide(BigInteger dividend, int dividendExponent, int divisor, out int newExponent)
		{
			var exponentDelta = 0;
			var tolerance = 1d / divisor;

			var bDivisor = new BigInteger(divisor);
			var result = BigInteger.DivRem(dividend, bDivisor, out var remainder);

			ReportDivideValues(dividend, dividendExponent, divisor, dividend, result, remainder, exponentDelta);

			var adjRem = GetValue(remainder, -1 * exponentDelta);
			//var adjRemA1 = BigIntegerHelper.GetValue(remainder, dividendExponent - exponentDelta);
			//var adjRemA2 = BigIntegerHelper.GetValue(remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);

			while (adjRem > tolerance)
			{
				exponentDelta++;
				var adjDividend = ScaleB(dividend, exponentDelta);
				result = BigInteger.DivRem(adjDividend, bDivisor, out remainder);

				adjRem = GetValue(remainder, -1 * exponentDelta);
				//adjRemA1 = BigIntegerHelper.GetValue(remainder, dividendExponent - exponentDelta);
				//adjRemA2 = BigIntegerHelper.GetValue(remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);

				//ReportDivyValues(dividend, dividendExponent, divisor, adjDividend, result, remainder, exponentDelta);
			}

			newExponent = dividendExponent - exponentDelta;
			return result;
		}


		private static void ReportDivideValues(BigInteger dividend, int dividendExponent, int divisor, BigInteger adjDividend, BigInteger result, BigInteger remainder, int exponentDelta)
		{
			var dividendD = GetValue(dividend, dividendExponent);
			var trueResult = dividendD / divisor;

			var adjDividendD = GetValue(adjDividend);
			var remainderD = GetValue(remainder);

			//var leftover = adjDividendD / remainderD;
			//var leftOver2 = BigInteger.DivRem(adjDividend, remainder, out var loReminder);
			//var diff = (1d / divisor) - leftover;

			var res = GetValue(result, dividendExponent - exponentDelta);
			var denominator = Math.Pow(2, -1 * (dividendExponent - exponentDelta));

			var adjRemainder = remainderD / denominator;

			var extent = res * divisor;
			var overallDif = dividendD - extent;

			Debug.WriteLine($"Dividend: {dividendD}, Divisor: {divisor}, trueResult: {trueResult}, currentDividend: {adjDividend}, remainder: {remainder}");
			Debug.WriteLine($"Result = {res} {result}/{denominator}, extent={extent}, overallDif={overallDif}, adjRem: {adjRemainder} ");
		}

		#endregion

		#region LogBase2 support

		public static int LogBase2(BigInteger n, int exponent)
		{
			var result = LogBase2(n) + exponent;
			CheckLogBase2Result(n, exponent, result);

			return result;
		}

		private static int LogBase2(BigInteger n)
		{
			//logb(x) = logc(x) / logc(b)
			double natLog = BigInteger.Log(n);
			double base2Log = natLog / NAT_LOG_OF_2;

			var result = (int)Math.Round(base2Log);

			return result;
		}

		[Conditional("Debug")]
		private static void CheckLogBase2Result(BigInteger n, int exponent, int ourResult)
		{
			if (TryGetValue(n, exponent, out double val))
			{
				var correctResult = (int) Math.Round(Math.Log2(val));

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

		#endregion

		public static BigInteger ScaleB(BigInteger n, int exponent)
		{
			if (exponent < 0)
			{
				throw new InvalidOperationException("Cannot use ScaleB on a BigInteger using a negative exponent.");
			}

			var result = n * new BigInteger(Math.Pow(2, exponent));
			return result;
		}

		public static double GetRatio(BigInteger dividend, int divisor)
		{
			if(divisor > 100000)
			{
				throw new NotSupportedException($"GetRatio expects the divisor to be fairly small. A divisor with value: {divisor} is not supported.");
			}

			double result;

			if (BigInteger.Abs(dividend) > FACTOR)
			{
				var temp = dividend / divisor;
				result = GetValue(temp);
			}
			else
			{
				var workingDividend = GetValue(dividend);
				result = workingDividend / divisor;
			}

			return result;
		}

		public static double GetValue(BigInteger n, int exponent)
		{
			if(n == 0)
			{
				return 0;
			}

			if (!SafeCastToDouble(n))
			{
				throw new OverflowException($"It is not safe to cast BigInteger: {n} to a double.");
			}

			var hiAndLo = ToLongs(n);
			double result = hiAndLo[0] + hiAndLo[1];
			result = Math.ScaleB(result, exponent);

			return !DoubleHelper.HasPrecision(result)
                ?               throw new OverflowException($"When converting BigInteger: {n} to a double, precision was lost.")
				: result;
		}

		public static bool TryGetValue(BigInteger n, int exponent, out double value)
		{
			if (!SafeCastToDouble(n))
			{
				value = double.NaN;
				return false;
			}

			var hiAndLo = ToLongs(n);
			var temp = hiAndLo[0] + hiAndLo[1];
			value = Math.ScaleB(temp, exponent);

			return DoubleHelper.HasPrecision(value);
		}

		public static double GetValue(BigInteger n)
		{
			if (!SafeCastToDouble(n))
			{
				throw new OverflowException($"It is not safe to cast BigInteger: {n} to a double.");
			}

			double result = (double)n;
			return result;
		}

		private static bool SafeCastToDouble(BigInteger n)
		{
			return DOUBLE_MIN_VALUE <= n && n <= DOUBLE_MAX_VALUE;
		}


	}


}
