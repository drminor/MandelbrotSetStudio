﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public static class BigIntegerHelper
	{
		private static readonly double NAT_LOG_OF_2 = Math.Log(2);

		// Largest integer that can be represented by a double for which it and all smaller integers can be reduced by 1 without loosing precision.
		private static readonly BigInteger FACTOR = new BigInteger(Math.Pow(2, 53));

		#region Division

		public static RValue Divide(RValue dividend, int divisor)
		{
			var newNumerator = Divide(dividend.Value, dividend.Exponent, divisor, out var newExponent);
			var result = new RValue(newNumerator, newExponent);
			return result;
		}

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

		public static string GetDisplay(BigInteger v, int exponent)
		{
			return $"{v}/{ Math.Pow(2, -1 * exponent).ToString(CultureInfo.InvariantCulture)}";
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

			if (hi > FACTOR)
			{
				throw new ArgumentOutOfRangeException(nameof(bi));
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
			var result = new BigInteger(0);
			foreach (var v in values)
			{
				result += v;
			}

			return result;
		}

		public static bool TryConvertToInt(IBigRatShape bigRatShape, out int[] values)
		{
			if (bigRatShape.Exponent == 0)
			{
				var tResult = new List<int>();
				foreach(var val in bigRatShape.Values)
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
			else
			{
				values = new int[0];
				return false;
			}
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

		private static double ConvertToDouble(BigInteger n, int exponent)
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

		private static double ConvertToDouble(BigInteger n)
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
			//bool result = DOUBLE_MIN_VALUE <= n && n <= DOUBLE_MAX_VALUE;
			bool result = BigInteger.Abs(n) <= FACTOR;

			return result;
		}

		#endregion
	}
}
