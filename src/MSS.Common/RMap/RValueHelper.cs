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

		//public static RValue ConvertToRValue(string s)
		//{
		//	if (double.TryParse(s, out var dValue))
		//	{
		//		return ConvertToRValue(dValue, exponent);
		//	}
		//	else
		//	{
		//		return new RValue();
		//	}
		//}

		public static bool TryConvertToRValue(string s, out RValue value)
		{
			//if (double.TryParse(s, out var dValue))
			//{
			//	value = ConvertToRValue(dValue, exponent);
			//	return true;
			//}
			//else
			//{
			//	value = new RValue();
			//	return false;
			//}

			value = ConvertToRValue(s);
			return true;
		}

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

		public static RValue ConvertToRValue(string s)
		{
			// Supports arbitray string lengths.

			var formatProvider = CultureInfo.InvariantCulture;

			var sme = new SignManExp(s);
			var pow = (int)Math.Round(3.3 * sme.NumberOfDigitsAfterDecimalPoint);
			var bigInt = BigInteger.Parse(sme.Mantissa, formatProvider);

			var factor = BigInteger.Pow(2, pow);

			var gcDivisor = BigInteger.GreatestCommonDivisor(bigInt, factor);

			bigInt *= factor;

			bigInt = AdjustWithPrecision(bigInt, sme.Precision, formatProvider);

			if (sme.IsNegative)
			{
				bigInt *= -1;
			}

			var result = new RValue(bigInt, -1 * pow, sme.Precision);

			result = Reducer.Reduce(result);

			Debug.WriteLine($"s: {s}, produced: {result}.");

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

		//public static RValue ConvertToRValueOLD(string s)
		//{
		//	// Supports arbitray string lengths.

		//	var dValComps = GetDValComps(s);

		//	//var rVals = dValComps.Select(x => ConvertToRValue(x, 0)).ToArray();
		//	var tt = new List<RValue>();
		//	for (var i = 0; i < dValComps.Count; i++)
		//	{
		//		var rValComp = ConvertToRValue(dValComps[i], 0);
		//		tt.Add(rValComp);
		//	}

		//	var rVals = tt.ToArray();

		//	var result = Sum(rVals);

		//	return result;
		//}

		//private static RValue ConvertToRValue(double d, int exponent)
		//{
		//	//Debug.WriteLine($"Beginning to Convert {d:G12} to an RValue.");
		//	var origD = d;

		//	var n = d.ToString("G17");
		//	var p = n.IndexOf('.');

		//	var nl = n.Length - p;
		//	d = d * Math.Pow(2, nl);
		//	exponent -= nl;

		//	var err = Math.Abs(d - Math.Truncate(d));

		//	var cnt = 0;
		//	while (err > Math.Abs(0.000001))
		//	{
		//		//Debug.WriteLine($"Still multiplying. NewD: {d:G12},  Err: {err:G12}.");

		//		d *= 2;
		//		exponent--;
		//		cnt++;

		//		err = Math.Abs(d - Math.Truncate(d));
		//	}

		//	d = Math.Round(d);

		//	var result = new RValue((BigInteger)d, exponent);
		//	Debug.WriteLine($"\nThe final RValue computed from: {origD:G12} is {result} Took {cnt} ops, Err: {err:G12}.");

		//	return result;
		//}

		//public static RValue Sum(params RValue[] rValues)
		//{
		//	var stage = rValues[0];

		//	for (var i = 1; i < rValues.Length; i++)
		//	{
		//		var rVal = rValues[i];
		//		var nrmStage = RNormalizer.Normalize(stage, rVal, out var nrmRVal);
		//		stage = nrmStage.Add(nrmRVal);

		//		//var st = BigIntegerHelper.GetDisplay(stage);
		//		//Debug.WriteLine($"Still summing, st is {st}.");
		//	}

		//	return stage;
		//}

		public static void Test(RValue rValue)
		{

			// "0.535575821681765930306959274776606";
			// "0.535575821681765
			//	0.000000000000000930306959274776606

			// "0.000000000000000000930306959274776606
			// "0000000000000000306959274776606";
			// "00000000000000000306959274776606
			// "0.0000000000000000306959274776606"

			//0.00000000000000930306959274776606

			// "5355758216817659000000000000000";

			//var dVals = ConvertToDoubles(rValue.Value, rValue.Exponent).ToArray();

			//var rVals = dVals.Select(x => ConvertToRValue(x, 0)).ToArray();

			//var c = Sum(rVals);

			//Debug.WriteLine($"C = {c}.");
		}

		public static RValue Test2(string s)
		{
			var result = ConvertToRValue(s);
			Debug.WriteLine($"The final result from Test2 is {result}.");

			return result;
		}

		//public static IList<double> GetDValComps(string s)
		//{
		//	s = SignManExp.ConvertToScientificNotation(s);
		//	var smeValue = new SignManExp(s);

		//	var result = new List<double>();
		//	var diag = new List<string>();

		//	while (TryGetNumericChunk(ref smeValue, out var chunk, out var strChunk))
		//	{
		//		result.Add(chunk);

		//		var ff = NumericStringInfo.ConvertToFixedPoint(strChunk);
		//		diag.Add(ff);
		//	}

		//	Debug.WriteLine("The DComps are:\n");
		//	for(var i = 0; i < result.Count; i++)
		//	{
		//		Debug.WriteLine($"{result[i]}\t\t{diag[i]}\n");
		//	}

		//	return result;
		//}

		//private static bool TryGetNumericChunk(ref SignManExp? smeValue, out double chunk, out string strChunk)
		//{
		//	var CHUNK_LENGTH = 13;

		//	if (smeValue == null)
		//	{
		//		chunk = double.NaN;
		//		strChunk = string.Empty;
		//		return false;
		//	}
		//	else
		//	{
		//		if (smeValue.Mantissa.Length > CHUNK_LENGTH)
		//		{
		//			var t = new SignManExp(smeValue.IsNegative, smeValue.Mantissa[0..CHUNK_LENGTH], smeValue.Exponent);
		//			strChunk = t.GetValueAsString();

		//			chunk = t.GetValueAsDouble();

		//			var newMantissa = smeValue.Mantissa[CHUNK_LENGTH..^0];

		//			if (smeValue.Exponent < 0)
		//			{
		//				newMantissa = "0." + newMantissa;
		//			}
		//			else
		//			{
		//				newMantissa = newMantissa[0..1] + '.' + newMantissa[1..];
		//			}

		//			smeValue = new SignManExp(smeValue.IsNegative, newMantissa, smeValue.Exponent - (CHUNK_LENGTH - 2));
		//		}
		//		else
		//		{
		//			chunk = smeValue.GetValueAsDouble();
		//			strChunk = smeValue.GetValueAsString();
		//			smeValue = null;
		//		}

		//		return true;
		//	}
		//}

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

		public static int GetResolution(RValue rValue)
		{
			var reducedR = Reducer.Reduce(rValue);

			var reciprocalOfTheDenominator = BigInteger.Pow(2, -1 * reducedR.Exponent);
			var result = BigInteger.Divide(reciprocalOfTheDenominator, reducedR.Value);

			//var gCD = BigInteger.GreatestCommonDivisor(rValue.Value, BigInteger.Pow(2, -1 * rValue.Exponent));
			//var log2 = BigIntegerHelper.LogBase2(gCD, 0);

			//4 * 2^-3 -> 1/4
			//4 * 2^3 -> 32

			//return log2;

			return (int) result;
		}

		#endregion
	}
}
