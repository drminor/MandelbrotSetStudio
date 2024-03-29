﻿using MSS.Types;
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

			//Debug.WriteLine($"RValue: {rValue}, produced: {result}.");

			return result;
		}

		public static string[] GetFormattedCoords(RRectangle coords)
		{
			//var result = new string[4];
			//var rValues = coords.GetRValues();

			//result[0] = ConvertToString(rValues[0]);
			//result[1] = ConvertToString(rValues[1]);
			//result[2] = ConvertToString(rValues[2]);
			//result[3] = ConvertToString(rValues[3]);

			var result = coords.GetRValues().Select(x => ConvertToString(x)).ToArray();

			return result;
		}

		public static string[] GetValuesAsStrings(IBigRatShape bigRatShape)
		{
			var result = bigRatShape.Values.Select(x => new RValue(x, bigRatShape.Exponent)).Select(x => ConvertToString(x)).ToArray();
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
			var digits = sme.Mantissa.Replace(",", "");
			var bigInt = BigInteger.Parse(digits, formatProvider);

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
			var divisorLog = chunkSize;
			var divisor = new BigInteger(Math.Pow(2, divisorLog));

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
				var hi = BigInteger.DivRem(n, divisor, out var lo);

				while (hi != 0)
				{
					result.Add((double)lo);
					hi = BigInteger.DivRem(hi, divisor, out lo);
				}

				result.Add((double)lo);

				for (var i = 0; i < result.Count; i++)
				{
					checked
					{
						result[i] *= Math.Pow(2, exponent + (i * divisorLog));
					}
				}

				result.Reverse();
			}

			return result;
		}

		#endregion

		#region Compare

		public static bool AreClose(RValue a, RValue b, bool failOnTooFewDigits = true)
		{
			if (GetStringsToCompare(a, b, failOnTooFewDigits, out var strA, out var strB))
			{
				Debug.WriteLine($"AreClose is comparing {strA} with {strB} and returning {strA == strB}.");
				return strA == strB;
			}
			else
			{
				return false;
			}
		}

		public static bool GetStringsToCompare(RValue a, RValue b, bool failOnTooFewDigits, out string strA, out string strB)
		{
			strA = string.Empty;
			strB = string.Empty;

			var strARaw = GetStringsToCompare(a, b, out var strBRaw);

			var precision = Math.Min(a.Precision, b.Precision);
			var numberOfDecimalDigits = DoubleHelper.RoundToZero(DoubleHelper.GetNumberOfDecimalDigits(precision)); // (int)Math.Round(precision * Math.Log10(2), MidpointRounding.ToZero);

			if (strARaw.Length < numberOfDecimalDigits && failOnTooFewDigits)
			{
				Debug.WriteLine($"Value A has {strARaw.Length} decimal digits which is less than {numberOfDecimalDigits}.");
				return false;
			}

			if (strBRaw.Length < numberOfDecimalDigits && failOnTooFewDigits)
			{
				Debug.WriteLine($"Value B has {strBRaw.Length} decimal digits which is less than {numberOfDecimalDigits}.");
				return false;
			}

			strA = strARaw.Substring(0, Math.Min(numberOfDecimalDigits, strARaw.Length));
			strB = strBRaw.Substring(0, Math.Min(numberOfDecimalDigits, strBRaw.Length));

			Debug.WriteLine($"GetString found a:{strARaw}, b:{strBRaw} and is returning a:{strA}, b:{strB}");

			return true;
		}

		public static int GetNumberOfMatchingDigits(RValue a, RValue b, out int expected)
		{
			var precision = Math.Min(a.Precision, b.Precision);

			//expected = (int)Math.Round(precision * Math.Log10(2), MidpointRounding.ToZero);
			expected = DoubleHelper.RoundToZero(DoubleHelper.GetNumberOfDecimalDigits(precision));

			var strA = GetStringsToCompare(a, b, out var strB);
			Debug.WriteLine($"GetNumberOfMatchingDigits found a:{strA}, b:{strB}");

			var result = GetNumberOfMatchingChars(strA, strB);

			return result;
		}

		private static int GetNumberOfMatchingChars(string a, string b)
		{
			var len = Math.Min(a.Length, b.Length);

			var i = 0;
			for(; i < len; i++)
			{
				if (a[i] != b[i])
				{
					break;
				}
			}

			return i;
		}

		public static string GetStringsToCompare(RValue a, RValue b, out string strB)
		{
			var aNrm = RNormalizer.Normalize(a, b, out var bNrm);

			var result = aNrm.Value.ToString();
			strB = bNrm.Value.ToString();

			return result;
		}

		#endregion

		#region Precision

		// Each decimal digits is equal to ≈ 3.3219 binary digits. (53 log10(2) ≈ 15.955).

		/// <summary>
		/// Calculates the number of decimal digits required to resolve rValue2 from rValue1.
		/// </summary>
		/// <param name="rValue1"></param>
		/// <param name="rValue2"></param>
		/// <param name="diff">The absolute difference between rValue1 and rValue2</param>
		/// <returns>Number of decimal digits</returns>
		public static int GetPrecision(RValue rValue1, RValue rValue2, out RValue diff)
		{
			var nrmRVal1 = RNormalizer.Normalize(rValue1, rValue2, out var nrmRVal2);
			var diffNoPrecision = nrmRVal1.Sub(nrmRVal2).Abs();

			var doubles = ConvertToDoubles(diffNoPrecision);
			//var msd = doubles[0];
			//var logB10 = Math.Log10(msd);

			var val = doubles.Sum();
			var logB10 = Math.Log10(val);

			var result = (int)Math.Ceiling(Math.Abs(logB10));

			diff = new RValue(diffNoPrecision.Value, diffNoPrecision.Exponent, result);

			return result;
		}

		/// <summary>
		/// Calculates the number of binary digits required to resolve rValue2 from rValue1.
		/// </summary>
		/// <param name="rValue1"></param>
		/// <param name="rValue2"></param>
		/// <param name="diff">The absolute difference between rValue1 and rValue2</param>
		/// <returns>Number of decimal digits</returns>
		public static int GetBinaryPrecision(RValue rValue1, RValue rValue2, out int decimalPrecision)
		{
			var nrmRVal1 = RNormalizer.Normalize(rValue1, rValue2, out var nrmRVal2);
			var diffNoPrecision = nrmRVal1.Sub(nrmRVal2).Abs();

			var doubles = ConvertToDoubles(diffNoPrecision);
			
			//var msd = doubles[0];
			//var logB10 = Math.Log10(msd);

			var val = doubles.Sum();
			var logB10 = Math.Log10(val);

			var result = (int)Math.Ceiling(Math.Abs(logB10 * 3.3219));

			decimalPrecision = (int)Math.Ceiling(Math.Abs(logB10));

			return result;
		}

		public static string GetFormattedResolution(RValue rValue)
		{
			var culture = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
			var numberFormatInfo = culture.NumberFormat;
			numberFormatInfo.NumberGroupSeparator = ",";
			numberFormatInfo.NumberDecimalDigits = 0;

			var resolution = GetResolution(rValue);

			var septillons = BigInteger.Divide(resolution, BigInteger.Pow(new BigInteger(1000), 8)); // 10 ^ 24 - Yotta
			if (septillons > 0)
			{
				var n = septillons * 1000;
				var np = (long)Math.Round((double)n / 1000);
				return np.ToString("N", numberFormatInfo) + " Y";
			}
			else
			{
				var quintillions = BigInteger.Divide(resolution, BigInteger.Pow(new BigInteger(1000), 6)); // 10 ^ 18 - Exa
				if (quintillions > 0)
				{
					var n = quintillions * 1000;
					var np = (long)Math.Round((double)n / 1000);
					return np.ToString("N", numberFormatInfo) + " E";
				}
				else
				{
					var trillons = BigInteger.Divide(resolution, BigInteger.Pow(new BigInteger(1000), 4)); // 10 ^ 12 - Tera
					if (trillons > 0)
					{
						var n = trillons * 1000;
						var nb = (long)Math.Round((double)n / 1000);
						return nb.ToString("N", numberFormatInfo) + " T";
					}
					else
					{
						var millions = BigInteger.Divide(resolution, BigInteger.Pow(new BigInteger(1000), 2)); // 10 ^ 6 - Mega
						if (millions > 0)
						{
							var n = millions * 1000;
							var nb = (long)Math.Round((double)n / 1000);
							return nb.ToString("N", numberFormatInfo) + " M";
						}
						else
						{
							return resolution.ToString("N", numberFormatInfo);
						}
					}
				}
			}
		}

		public static BigInteger GetResolution(RValue rValue)
		{
			var reducedR = Reducer.Reduce(rValue);

			if (reducedR.Value == 0)
			{
				return 0;
			}
			else
			{
				var reciprocalOfTheDenominator = BigInteger.Pow(2, -1 * reducedR.Exponent);
				var result = BigInteger.Divide(reciprocalOfTheDenominator, reducedR.Value);
				return result;
			}
		}

		#endregion
	}
}
