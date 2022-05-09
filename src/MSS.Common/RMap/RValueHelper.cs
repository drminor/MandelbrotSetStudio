using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Numerics;

namespace MSS.Common
{
	public static class RValueHelper
	{
		#region To String

		public static string ConvertToString(RValue rValue, int useSciNotationForLengthsGe = 100)
		{
			var dVals = ConvertToDoubles(rValue);
			var numericStringInfos = dVals.Select(x => new NumericStringInfo(x)).ToArray();
			var total = NumericStringInfo.Sum(numericStringInfos);

			var result = total.GetString(rValue.Precision);

			if (result.Length >= useSciNotationForLengthsGe)
			{
				result = SignManExp.ConvertToScientificNotation(result);
			}

			Debug.WriteLine($"RValue: {rValue}, produced: {result}.");

			return result;
		}

		#endregion

		#region From String

		public static bool TryConvertToRValue(string s, out RValue value)
		{
			value = ConvertToRValue(s);
			return true;
		}

		public static RValue ConvertToRValue(string s)
		{
			var sme = new SignManExp(s);
			var result = ConvertToRValue(sme);

			Debug.WriteLine($"String: {s}, produced RValue: {result}.");

			return result;
		}

		public static RValue ConvertToRValue(SignManExp sme)
		{
			// Supports arbitray string lengths.

			var formatProvider = CultureInfo.InvariantCulture;

			var pow = (int)Math.Round(3.3333 * (1 + sme.NumberOfDigitsAfterDecimalPoint));
			var bigInt = BigInteger.Parse(sme.Mantissa, formatProvider);

			var factor = BigInteger.Pow(2, pow);
			bigInt *= factor;
			bigInt = BigInteger.DivRem(bigInt, BigInteger.Pow(10, sme.NumberOfDigitsAfterDecimalPoint), out var _);

			//bigInt = AdjustWithPrecision(bigInt, pow, formatProvider);

			if (sme.IsNegative)
			{
				bigInt *= -1;
			}

			var result = new RValue(bigInt, -1 * pow, sme.Precision);

			result = Reducer.Reduce(result);

			return result;
		}

		private static BigInteger AdjustWithPrecision(BigInteger b, int precision, IFormatProvider formatProvider)
		{
			var allDigits = b.ToString(formatProvider);
			var requiredDigits = allDigits.Length > precision ? allDigits[0..precision] : allDigits;

			var result = BigInteger.Parse(requiredDigits, formatProvider);

			if (allDigits.Length > precision + 1)
			{
				var followingDigit = allDigits[precision + 1].ToString(formatProvider);
				var followingDigitValue = int.Parse(followingDigit, formatProvider);

				if (followingDigitValue > 4)
				{
					result += 1;
				}
			}

			return result;
		}

		public static RRectangle BuildRRectangle(string[] vals)
		{
			var result = BuildRRectangle(vals.Select(x => new SignManExp(x)).ToArray());
			return result;
		}

		public static RRectangle BuildRRectangle(SignManExp[] vals)
		{
			var result = BuildRRectangle(vals.Select(x => ConvertToRValue(x)).ToArray());
			return result;
		}

		public static RRectangle BuildRRectangle(RValue[] vals)
		{
			var result = BuildRRectangle(vals[0], vals[1], vals[2], vals[3]);

			//var nx1 = RNormalizer.Normalize(vals[0], vals[2], out var ny1);
			//var p1 = new RPoint(nx1.Value, ny1.Value, nx1.Exponent);

			//var nx2 = RNormalizer.Normalize(vals[1], vals[3], out var ny2);
			//var p2 = new RPoint(nx2.Value, ny2.Value, nx2.Exponent);

			//var np1 = RNormalizer.Normalize(p1, p2, out var np2);
			//var result = new RRectangle(np1, np2);

			return result;
		}

		public static RRectangle BuildRRectangle(RValue x1, RValue x2, RValue y1, RValue y2)
		{
			var nx1 = RNormalizer.Normalize(x1, y1, out var ny1);
			var p1 = new RPoint(nx1.Value, ny1.Value, nx1.Exponent);

			var nx2 = RNormalizer.Normalize(x2, y2, out var ny2);
			var p2 = new RPoint(nx2.Value, ny2.Value, nx2.Exponent);

			var np1 = RNormalizer.Normalize(p1, p2, out var np2);
			var result = new RRectangle(np1, np2);

			return result;
		}

		#endregion

		#region Convert to IList<Double>

		public static IList<double> ConvertToDoubles(RValue rValue, int chunkSize = 53)
		{
			return ConvertToDoubles(rValue.Value, rValue.Exponent, chunkSize);
		}

		// TODO: Move this to the BigIntegerHelper class.
		public static IList<double> ConvertToDoubles(BigInteger n, int exponent, int chunkSize)
		{
			var DIVISOR_LOG = chunkSize;
			var DIVISOR = new BigInteger(Math.Pow(2, DIVISOR_LOG));

			var result = new List<double>();

			if (n == 0)
			{
				result.Add(0);
			}
			//else if (BigIntegerHelper.SafeCastToDouble(n))
			//{
			//	result.Add((double)n);
			//	checked
			//	{
			//		result[0] *= Math.Pow(2, exponent);
			//	}
			//}
			else
			{
				var hi = BigInteger.DivRem(n, DIVISOR, out var lo);

				while (hi != 0)
				{
					result.Add((double)lo);
					hi = BigInteger.DivRem(hi, DIVISOR, out lo);
				}

				result.Add((double)lo);

				for (var i = 0; i < result.Count; i++)
				{
					checked
					{
						result[i] *= Math.Pow(2, exponent + i * DIVISOR_LOG);
					}
				}

				result.Reverse();
			}

			return result;
		}

		#endregion

		#region Precision

		public static int GetPrecision(RValue rValue1, RValue rValue2, out RValue diff)
		{
			var nr1 = RNormalizer.Normalize(rValue1, rValue2, out var nr2);
			var diffNoPrecision = nr1.Sub(nr2).Abs();

			var doubles = ConvertToDoubles(diffNoPrecision);
			var msd = doubles[0];
			var l10 = Math.Log10(msd);

			var result = (int)Math.Ceiling(Math.Abs(l10));

			diff = new RValue(diffNoPrecision.Value, diffNoPrecision.Exponent, result);

			return result;
		}

		public static long GetResolution(RValue rValue)
		{
			var reducedR = Reducer.Reduce(rValue);

			var reciprocalOfTheDenominator = BigInteger.Pow(2, -1 * reducedR.Exponent);

			if (reducedR.Value == 0)
			{
				return 0;
			}
			else
			{
				var result = BigInteger.Divide(reciprocalOfTheDenominator, reducedR.Value);
				return (long)result;
			}
		}

		#endregion
	}
}
