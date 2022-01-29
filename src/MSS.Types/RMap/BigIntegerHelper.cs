using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
			var tolerance = 20d / divisor;
			var bDivisor = new BigInteger(divisor);

			var result = BigInteger.DivRem(dividend, bDivisor, out var remainder);
			var adjRem = ((double)remainder) / Math.Pow(2, exponentDelta);
			//var adjRemA1 = ((double)remainder) * Math.Pow(2, dividendExponent - exponentDelta);
			//var adjRemA2 = ((double)remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);
			//ReportDivideValues(dividend, dividendExponent, divisor, dividend, result, remainder, exponentDelta);

			while (adjRem > tolerance)
			{
				exponentDelta++;
				var adjDividend = dividend * new BigInteger(Math.Pow(2, exponentDelta));

				result = BigInteger.DivRem(adjDividend, bDivisor, out remainder);
				adjRem = ((double)remainder) / Math.Pow(2, exponentDelta);
				//adjRemA1 = ((double)remainder) * Math.Pow(2, dividendExponent - exponentDelta);
				//adjRemA2 = ((double)remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);
				//ReportDivideValues(dividend, dividendExponent, divisor, adjDividend, result, remainder, exponentDelta);
			}

			newExponent = dividendExponent - exponentDelta;
			return result;
		}

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

		private static void ReportDivideValues(BigInteger dividend, int dividendExponent, int divisor, BigInteger adjDividend, BigInteger result, BigInteger remainder, int exponentDelta)
		{
			var dividendD = ConvertToDouble(dividend, dividendExponent);
			var trueResult = dividendD / divisor;

			var remainderD = ConvertToDouble(remainder);

			var res = ConvertToDouble(result, dividendExponent - exponentDelta);
			var denominator = Math.Pow(2, -1 * (dividendExponent - exponentDelta));

			var adjRemainder = remainderD / denominator;

			var extent = res * divisor;
			var overallDif = dividendD - extent;

			Debug.WriteLine($"Dividend: {dividendD}, Divisor: {divisor}, trueResult: {trueResult}, currentDividend: {adjDividend}, remainder: {remainder}");
			Debug.WriteLine($"Result = {res} ({result}/{denominator}), extent={extent}, overallDif={overallDif}, adjRem: {adjRemainder} ");
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
			// Using formula: logb(x) = logc(x) / logc(b) 
			// to change the base from e to 2.

			var natLog = BigInteger.Log(n);
			var base2Log = natLog / NAT_LOG_OF_2;

			var result = (int)Math.Round(base2Log);

			return result;
		}

		[Conditional("Debug")]
		private static void CheckLogBase2Result(BigInteger n, int exponent, int ourResult)
		{
			if (TryConvertToDouble(n, exponent, out var val))
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

		public static string GetDisplay(IBigRatShape bigRatShape)
		{
			return GetDisplay(bigRatShape.Values, bigRatShape.Exponent);
		}

		public static string GetDisplay(BigInteger[] values, int exponent, IFormatProvider? formatProvider = null)
		{
			if (formatProvider is null)
			{
				formatProvider = CultureInfo.InvariantCulture;
			}

			var strDenominator = Math.Pow(2, -1 * exponent).ToString(formatProvider);

			var dVals = values.Select(v => ConvertToDouble(v, exponent)).ToArray();

			var strVals = values.Select((x, i) => new string(x.ToString(formatProvider) + "/" + strDenominator + " (" + dVals[i].ToString(formatProvider) + ")")).ToArray();

			var display = string.Join("; ", strVals);
			return display;
		}

		#region Convert to Double

		public static double ConvertToDouble(BigInteger n, int exponent)
		{
			if(n == 0)
			{
				return 0;
			}

			double result;

			if (SafeCastToDouble(n))
			{
				checked
				{
					result = (double)n;
					result *= Math.Pow(2, exponent);
				}
			}
			else
			{
				var hiAndLo = ToLongs(n);

				checked
				{
					result = hiAndLo[0] * Math.Pow(2, exponent + 53);
					result += hiAndLo[1] * Math.Pow(2, exponent);
				}
			}

			return !DoubleHelper.HasPrecision(result)
                ?               throw new OverflowException($"When converting BigInteger: {n} to a double, precision was lost.")
				: result;
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

		private static bool SafeCastToDouble(BigInteger n)
		{
			return DOUBLE_MIN_VALUE <= n && n <= DOUBLE_MAX_VALUE;
		}

		#endregion
	}
}
