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

		public static string ConvertToString(RValue rValue)
		{
			var dVals = ConvertToDoubles(rValue);
			var numericStringInfos = dVals.Select(x => new NumericStringInfo(x)).ToArray();
			var total = NumericStringInfo.Sum(numericStringInfos);

			var result = total.GetString(rValue.Precision);

			//var t = BigInteger.Parse(result, CultureInfo.InvariantCulture);
			//t = AdjustWithPrecision(t, rValue.Precision, CultureInfo.InvariantCulture);
			//result = t.ToString(CultureInfo.InvariantCulture);

			if (result.Length > 8)
			{
				result = SignManExp.ConvertToScientificNotation(result);
			}

			Debug.WriteLine($"RValue: {rValue}, produced: {result}.");

			return result;
		}

		#endregion

		#region From String

		public static RRectangle BuildRRectangleFromStrings(string[] vals)
		{
			var x1 = ConvertToRValue(vals[0]);
			var x2 = ConvertToRValue(vals[1]);
			var y1 = ConvertToRValue(vals[2]);
			var y2= ConvertToRValue(vals[3]);

			var nX1 = RNormalizer.Normalize(x1, x2, out var nX2);
			var nY1 = RNormalizer.Normalize(y1, y2, out var nY2);

			var w = nX2.Sub(nX1);

			var h = nY2.Sub(nY1);

			return new RRectangle();
		}

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

		#endregion

		#region Convert to IList<Double>

		public static IList<double> ConvertToDoubles(RValue rValue)
		{
			return ConvertToDoubles(rValue.Value, rValue.Exponent);
		}

		// TODO: Move this to the BigIntegerHelper class and increase the "chunk" size.
		public static IList<double> ConvertToDoubles(BigInteger n, int exponent)
		{
			var DIVISOR_LOG = 3;
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

		public static long GetResolution(RValue rValue, out int precision)
		{
			var reducedR = Reducer.Reduce(rValue);
			precision = -1 * reducedR.Exponent;

			var reciprocalOfTheDenominator = BigInteger.Pow(2, precision);

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
