using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public static class BigIntegerHelper
	{
		public const int MAX_PRECISION = 33;

		private static readonly double NAT_LOG_OF_2 = Math.Log(2);

		// Largest integer that can be represented by a double for which it and all smaller integers can be reduced by 1 without loosing precision.
		private static readonly BigInteger FACTOR = new BigInteger(Math.Pow(2, 53));

		#region Division

		//public static RValue Divide(RValue dividend, int divisor)
		//{
		//	var newNumerator = Divide(dividend.Value, dividend.Exponent, divisor, out var newExponent);
		//	var result = new RValue(newNumerator, newExponent, dividend.Precision); // TODO:Px -- Check RValue Precision
		//	return result;
		//}

		public static RValue Divide(RValue dividend, int divisor)
		{
			var exponentDelta = 0;
			var tolerance = 20d / (double)divisor;

			var result = BigInteger.DivRem(dividend.Value, divisor, out var remainder);
			var adjRem = ((double)remainder) / Math.Pow(2, exponentDelta);

			//var adjRemA1 = ((double)remainder) * Math.Pow(2, dividendExponent - exponentDelta);
			//var adjRemA2 = ((double)remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);
			ReportDivideValues(dividend, divisor, dividend.Value, result, remainder, exponentDelta);

			while (adjRem > tolerance)
			{
				exponentDelta++;
				var adjDividend = dividend.Value * new BigInteger(Math.Pow(2, exponentDelta));

				result = BigInteger.DivRem(adjDividend, divisor, out remainder);
				adjRem = ((double)remainder) / Math.Pow(2, exponentDelta);


				//adjRemA1 = ((double)remainder) * Math.Pow(2, dividendExponent - exponentDelta);
				//adjRemA2 = ((double)remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);
				ReportDivideValues(dividend, divisor, adjDividend, result, remainder, exponentDelta);
			}

			var newExponent = dividend.Exponent - exponentDelta;
			var rResult = new RValue(result, newExponent, dividend.Precision); // TODO:Px -- Check RValue Precision

			return rResult;
		}

		private static void ReportDivideValues(RValue dividend, int divisor, BigInteger adjDividend, BigInteger result, BigInteger remainder, int exponentDelta)
		{
			if (TryConvertToDouble(dividend, out var dividendD))
			{
				var trueResult = dividendD / divisor;

				if (TryConvertToDouble(remainder, out var remainderD))
				{
					var res = ConvertToDouble(result, dividend.Exponent - exponentDelta);
					var denominator = Math.Pow(2, -1 * (dividend.Exponent - exponentDelta));

					var adjRemainder = remainderD / denominator;

					var extent = res * divisor;
					var overallDif = dividendD - extent;

					Debug.WriteLine($"Dividend: {dividendD}, Divisor: {divisor}, trueResult: {trueResult}, currentDividend: {adjDividend}, remainder: {remainder}");
					Debug.WriteLine($"Result = {res} ({result}/{denominator}), extent={extent}, overallDif={overallDif}, adjRem: {adjRemainder}, loopCntr: {exponentDelta} ");
				}
				else
				{
					Debug.WriteLine($"Could not convert the dividend to a double. Dividend: {dividend} Divisor: {divisor}, Result: {result}, remainder: {remainder}");
				}
			}
			else
			{
				Debug.WriteLine($"Could not convert the remainder to a double. Dividend: {dividend} Divisor: {divisor}, Result: {result}, remainder: {remainder}");
			}
		}

		#endregion

		#region New Divide Methods -- Not used

		public static RValue DivideNew(RValue dividend, int divisor)
		{
			var exp = dividend.Exponent;

			if (exp > 0)
			{
				throw new InvalidOperationException("When calling Divide, the dividend's exponent must be 0 or negative.");
			}

			var bDivisor = new BigInteger(divisor * Math.Pow(2, -1 * dividend.Exponent));
			var rDivisor = new RValue(bDivisor, dividend.Exponent, dividend.Precision); // TODO:Px -- Check RValue Precision

			var newNumerator = DivideNew(dividend.Value, rDivisor.Value, out var newExponent);

			var result = new RValue(newNumerator, -1 * newExponent, dividend.Precision); // TODO:Px -- Check RValue Precision
			return result;
		}

		public static BigInteger DivideNew(BigInteger dividend, BigInteger divisor, out int newExponent)
		{
			newExponent = 0;
			//var tolerance = divisor;

			var tolerance = divisor / 100;

			var result = BigInteger.DivRem(dividend, divisor, out var remainder);
			//var adjRem = ((double)remainder) / Math.Pow(2, exponentDelta);
			//var adjRem = remainder;


			//var adjRemA1 = ((double)remainder) * Math.Pow(2, dividendExponent - exponentDelta);
			//var adjRemA2 = ((double)remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);
			//ReportDivideValues(dividend, dividendExponent, divisor, dividend, result, remainder, exponentDelta);

			while (result == 0 || remainder > tolerance)
			{
				newExponent++;
				var adjDividend = dividend * new BigInteger(Math.Pow(2, newExponent));

				result = BigInteger.DivRem(adjDividend, divisor, out remainder);
				//adjRem = ((double)remainder) / Math.Pow(2, exponentDelta);
				//adjRem = ((double)remainder);


				//adjRemA1 = ((double)remainder) * Math.Pow(2, dividendExponent - exponentDelta);
				//adjRemA2 = ((double)remainder) / divisor * Math.Pow(2, dividendExponent - exponentDelta);
				//ReportDivideValues(dividend, dividendExponent, divisor, adjDividend, result, remainder, exponentDelta);
			}

			return result;
		}

		#endregion

		#region LogBase2 support

		public static int LogBase2(BigInteger n, int exponent)
		{
			var result = LogBase2(n) + exponent;
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

		#endregion

		#region ToString support

		public static string GetDisplay(RValue rValue, bool includeDecimalOutput = false, IFormatProvider? formatProvider = null)
		{
			if (formatProvider is null)
			{
				formatProvider = CultureInfo.InvariantCulture;
			}

			var result = $"{rValue.Value}/{ Math.Pow(2, -1 * rValue.Exponent).ToString(formatProvider)}";

			// TODO: Use Convert RValue To String insted of ConvertToDouble.
			// TODO: Use the RValue's precision to inform the ConvertToDouble method
			if (includeDecimalOutput)
			{
				result += $"({ConvertToDouble(rValue)})";
			}

			result += $" exp: {rValue.Exponent.ToString(formatProvider)}";

			return result;
		}

		public static string GetDisplay(IBigRatShape bigRatShape, bool includeDecimalOutput = false)
		{
			return GetDisplay(bigRatShape.Values, bigRatShape.Exponent, includeDecimalOutput);
		}

		public static string GetDisplay(BigInteger[] values, int exponent, bool includeDecimalOutput = false, IFormatProvider? formatProvider = null)
		{
			if (formatProvider is null)
			{
				formatProvider = CultureInfo.InvariantCulture;
			}

			var strDenominator = Math.Pow(2, -1 * exponent).ToString(formatProvider);

			string[] strVals;
			if (includeDecimalOutput)
			{
				var dVals = values.Select(v => ConvertToDouble(v, exponent)).ToArray();
				strVals = values.Select((x, i) => new string(x.ToString(formatProvider) + "/" + strDenominator + " (" + dVals[i].ToString(formatProvider) + ")")).ToArray();
			}
			else
			{
				strVals = values.Select((x, i) => new string(x.ToString(formatProvider) + "/" + strDenominator)).ToArray();
			}

			var display = string.Join("; ", strVals) + " exp:" + exponent.ToString(formatProvider);
			return display;
		}

		#endregion

		#region Convert to Integer Types

		public static long[][] ToLongs(BigInteger[] values)
		{
			var result = values.Select(v => ToLongs(v)).ToArray();
			return result;
		}

		public static long[] ToLongs(BigInteger bi)
		{
			var hi = BigInteger.DivRem(bi, FACTOR, out var lo);

			//if (hi > 0)
			//{
			//	Debug.WriteLine($"Got a Hi value when converting a IBigRatShape value. The value is {hi}");
			//}

			if (BigInteger.Abs(hi) > long.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(bi), "The hi value is larger than an Int64.");
			}

			if (BigInteger.Abs(lo) > long.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(bi), "The lo value is larger than an Int64.");
			}

			var result = new long[] { (long)hi, (long)lo };

			return result;
		}

		public static BigInteger[] FromLongs(long[][] values)
		{
			var result = values.Select(v => FromLongs(v)).ToArray();
			return result;
		}

		public static BigInteger FromLongs(long[] values)
		{
			Debug.Assert(values.Length == 2, "FromLongs received array of values whose length is not 2.");

			//var result = FACTOR;
			var result = FACTOR * values[0];
			result += values[1];

			return result;
		}

		public static bool TryConvertToInt(IBigRatShape bigRatShape, out int[] values)
		{
			if (bigRatShape.Exponent == 0)
			{
				return TryConvertToInt(bigRatShape.Values, out values);
			}
			else
			{
				values = new int[0];
				return false;
			}
		}

		public static bool TryConvertToInt(BigInteger[] bValues, out int[] values)
		{
			var tResult = new List<int>();
			foreach (var val in bValues)
			{
				if (TryConvertToInt(val, out int value))
				{
					tResult.Add(value);
				}
				else
				{
					values = new int[0];
					return false;
				}
			}

			values = tResult.ToArray();
			return true;
		}

		private static bool TryConvertToInt(BigInteger n, out int value)
		{
			if (n < int.MaxValue && n > int.MinValue)
			{
				value = (int)n;
				return true;
			}
			else
			{
				value = -1;
				return false;
			}
		}

		#endregion

		#region Convert to Double

		public static bool TryConvertToDouble(RValue r, out double dValue)
		{
			if (r.Value == 0)
			{
				dValue = 0;
				return true;
			}

			if (TryConvertToDouble(r.Value, out dValue))
			{
				try
				{
					checked
					{
						dValue *= Math.Pow(2, r.Exponent);
						return true;
					}
				}
				catch
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		public static double ConvertToDouble(RValue r)
		{
			return ConvertToDouble(r.Value, r.Exponent);
		}

		public static double ConvertToDouble(BigInteger n, int exponent)
		{
			if(n == 0)
			{
				return 0;
			}

			if (TryConvertToDouble(n, out var result))
			{
				try
				{
					checked
					{
						result *= Math.Pow(2, exponent);
					}
				}
				catch
				{
					result = double.NaN;
				}
			}
			else
			{
				result = double.NaN;
			}

			return result;
		}

		public static bool TryConvertToDouble(BigInteger n, out double d)
		{
			if (!SafeCastToDouble(n))
			{
				d = double.NaN;
				return false;
			}
			else
			{
				d = (double)n;
				return true;
			}
		}

		public static bool SafeCastToDouble(BigInteger n)
		{
			//bool result = DOUBLE_MIN_VALUE <= n && n <= DOUBLE_MAX_VALUE;
			var result = BigInteger.Abs(n) <= FACTOR;

			return result;
		}

		#endregion
	}
}
