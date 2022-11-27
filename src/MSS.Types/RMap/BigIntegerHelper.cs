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
		// Same as RMapConstants.DEFAULT_PRECISION
		public const int DEFAULT_PRECISION = 55; // Number of significant binary digits 

		private static readonly double NAT_LOG_OF_2 = Math.Log(2);

		// Largest integer that can be represented by a double for which it and all smaller integers can be reduced by 1 without loosing precision.
		private static readonly BigInteger MAX_DP_INTEGER = BigInteger.Pow(2, 53);

		// Integer used to convert BigIntegers to/from array of longs.
		private static readonly BigInteger LONG_FACTOR = BigInteger.Pow(2, 53); //new BigInteger(long.MaxValue); // BigInteger.Add(BigInteger.One, BigInteger.Pow(2, 63));

		#region Division

		//var tResult = BigInteger.DivRem(106, 10, out var tRemainder); tResult = 10, tRemainder = 6

		public static RValue Divide(RValue dividend, int divisor, double toleranceFactor)
		{
			var exponentDelta = 0;
			var tolerance = toleranceFactor / divisor;

			var result = BigInteger.DivRem(dividend.Value, divisor, out var remainder);
			var adjRem = ((double)remainder) / Math.Pow(2, dividend.Exponent - exponentDelta);

			while(result == 0)
			{
				exponentDelta++;
				var adjDividend = dividend.Value * new BigInteger(Math.Pow(2, exponentDelta));

				result = BigInteger.DivRem(adjDividend, divisor, out remainder);
				adjRem = ((double)remainder) * Math.Pow(2, dividend.Exponent - exponentDelta);
			}

			while (adjRem > tolerance)
			{
				exponentDelta++;
				var adjDividend = dividend.Value * new BigInteger(Math.Pow(2, exponentDelta));

				result = BigInteger.DivRem(adjDividend, divisor, out remainder);
				adjRem = ((double)remainder) * Math.Pow(2, dividend.Exponent - exponentDelta);

				//ReportDivideValues(dividend, divisor, adjDividend, result, remainder, exponentDelta);
			}

			var newExponent = dividend.Exponent - exponentDelta;
			var rResult = new RValue(result, newExponent, dividend.Precision);

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

		public static string GetDisplay(RValue rValue, bool includeDecimalOutput = false)
		{
			var formatProvider = CultureInfo.InvariantCulture;

			var result = $"{rValue.Value}/2^{(-1 * rValue.Exponent).ToString(formatProvider)}";

			// TODO: Use Convert RValue To String insted of ConvertToDouble.
			// TODO: Use the RValue's precision to inform the ConvertToDouble method
			if (includeDecimalOutput)
			{
				result += $"({ConvertToDouble(rValue)})";
			}

			//result += $" exp: {rValue.Exponent.ToString(formatProvider)}";

			return result;
		}

		public static string GetDisplay(IBigRatShape bigRatShape, bool includeDecimalOutput = false)
		{
			return GetDisplay(bigRatShape.Values, bigRatShape.Exponent, includeDecimalOutput);
		}

		public static string GetDisplay(BigInteger[] values, int exponent, bool includeDecimalOutput = false)
		{
			var formatProvider = CultureInfo.InvariantCulture;

			var strDenominator = $"2^{(-1 * exponent).ToString(formatProvider)}"; 	//"; Math.Pow(2, -1 * exponent).ToString(formatProvider);

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

			var display = string.Join("; ", strVals); // + " exp:" + exponent.ToString(formatProvider);

			return display;
		}

		#endregion

		#region Convert to Array of Longs

		// TODO: Use Little-Endian format, instead of Big-Endian format when creating an Array of Longs from a BigInteger

		public static long[][] ToLongs(BigInteger[] values)
		{
			var result = values.Select(v => ToLongs(v)).ToArray();
			return result;
		}

		public static long[] ToLongs(BigInteger bi)
		{
			var hi = BigInteger.DivRem(bi, LONG_FACTOR, out var lo);

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

		//public static BigInteger FromLongs(long[] values)
		//{
		//	Debug.Assert(values.Length == 2, "FromLongs received array of values whose length is not 2.");

		//	var result = LONG_FACTOR * values[0];
		//	result += values[1];

		//	return result;
		//}

		public static BigInteger FromLongs(long[] values)
		{
			//DtoLongs are in Big - Endian order
			var result = BigInteger.Zero;

			for (var i = 0; i < values.Length; i++)
			{
				result *= LONG_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion

		#region Convert to Integer

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
			var result = ConvertToDouble(r.Value) * Math.Pow(2, r.Exponent);
			return result;
		}

		public static double ConvertToDouble(BigInteger n, int exponent)
		{
			var result = ConvertToDouble(n) * Math.Pow(2, exponent);
			return result;
		}

		public static bool TryConvertToDouble(BigInteger n, out double d)
		{
			if (SafeCastToDouble(n))
			{
				d = (double)n;
				return true;
			}
			else
			{
				d = double.NaN;
				return false;
			}
		}

		public static double ConvertToDouble(BigInteger n)
		{
			var result = SafeCastToDouble(n) ? (double)n : double.NaN;
			return result;
		}

		public static bool SafeCastToDouble(BigInteger n)
		{
			//bool result = DOUBLE_MIN_VALUE <= n && n <= DOUBLE_MAX_VALUE;
			var result = BigInteger.Abs(n) <= MAX_DP_INTEGER;

			return result;
		}

		#endregion

		#region Precision Support




		public static RValue TrimToMatchPrecision(RValue rValue)
		{
			var result = TrimToMatchPrecision(rValue, rValue.Precision);
			return result;
		}

		public static RValue TrimToNumBaseUIntDigits(RValue rValue, int numberOfBaseUIntDigits)
		{
			var result = TrimToMatchPrecision(rValue, numberOfBaseUIntDigits * 32);
			return result;
		}

		public static RValue TrimToMatchPrecision(RValue rValue, int precision)
		{
			var numberOfDecimalDigits = (int)Math.Round(Math.Log10(2) * precision, MidpointRounding.AwayFromZero);
			numberOfDecimalDigits++; // Leave an extra decimal digit for padding or rounding

			var bStr = rValue.Value.ToString(CultureInfo.InvariantCulture);
			var indexOfFSD = bStr.IndexOfAny(new[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' });

			var bStrLength = bStr.Length - indexOfFSD;

			if (bStrLength > numberOfDecimalDigits)
			{
				var dDiff = bStrLength - numberOfDecimalDigits;

				var diffInBinaryDigits = (int)Math.Round(dDiff / Math.Log10(2), MidpointRounding.ToZero);

				//var divisorPow =  (int)Math.Round(Math.Log2(bDiff), MidpointRounding.ToZero);
				var divisor = BigInteger.Pow(2, diffInBinaryDigits);

				var result = new RValue(rValue.Value / divisor, rValue.Exponent + diffInBinaryDigits, rValue.Precision);
				return result;
			}
			else
			{
				return rValue;
			}
		}

		public static int GetPrecision(IBigRatShape ratShape)
		{
			var result = GetPrecision(ratShape.Values, ratShape.Exponent);
			return result;
		}

		public static int GetPrecision(BigInteger[] values, int exponent)
		{
			var precision = values.Min(x => BigInteger.Abs(x).ToString(CultureInfo.InvariantCulture).Length);

			var be = BigInteger.Pow(2, Math.Abs(exponent));
			var ePrecision = be.ToString(CultureInfo.InvariantCulture).Length;

			precision = Math.Max(precision, ePrecision);
			precision += 1;

			return precision;
		}

		public static int GetPrecision(BigInteger value, int exponent)
		{
			var precision = BigInteger.Abs(value).ToString(CultureInfo.InvariantCulture).Length;

			var be = BigInteger.Pow(2, Math.Abs(exponent));
			var ePrecision = be.ToString(CultureInfo.InvariantCulture).Length;

			precision = Math.Max(precision, ePrecision);
			precision += 1;

			return precision;
		}

		#endregion

	}
}
