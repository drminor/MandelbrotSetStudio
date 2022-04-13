using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	public static class RValueHelper
	{
		#region To String

		public static string ConvertToString(RValue rValue)
		{
			var dVals =  ConvertToDoubles(rValue.Value, rValue.Exponent);
			var nsis = dVals.Select(x => new NumericStringInfo(x)).ToArray();

			var stage = nsis[0];

			for (var i = 1; i < nsis.Length; i++)
			{
				var t = stage.Add(nsis[i]);
				var st = t.GetString();
				stage = t;
			}

			var result = stage.GetString();

			return result;
		}

		#endregion

		#region From String

		public static RValue ConvertToRValue(string s, int exponent)
		{
			if (double.TryParse(s, out var dValue))
			{
				return ConvertToRValue(dValue, exponent);
			}
			else
			{
				return new RValue();
			}
		}

		public static bool TryConvertToRValue(string s, int exponent, out RValue value)
		{
			if (double.TryParse(s, out var dValue))
			{
				value = ConvertToRValue(dValue, exponent);
				return true;
			}
			else
			{
				value = new RValue();
				return false;
			}
		}

		public static RValue ConvertToRValue(double d, int exponent)
		{
			var origD = d;


			//var wp = Math.Truncate(d);
			//d = d - wp;
			//var l2 = Math.Log2(d);
			//var l2a = (int) Math.Round(l2, MidpointRounding.AwayFromZero);
			//var newD = d * Math.Pow(2, -1 * l2a);
			//newD += wp;


			var l2 = Math.Log2(d);
			var l2a = (int)Math.Round(l2, MidpointRounding.ToZero);

			if (l2 < 0)
			{
				d = d * Math.Pow(2, -1 * l2a);
				exponent -= l2a;
			}

			//var newD = d * Math.Pow(2, -1 * l2a);


			while (Math.Abs(d - Math.Truncate(d)) > 0.000001)
			{
				//var t = Math.Abs(d - Math.Truncate(d));
				Debug.WriteLine($"Still multiplying. NewD: {d:G12}, StartD: {origD:G12}.");

				d *= 2;
				exponent--;
			}

			var result = new RValue((BigInteger)d, exponent);
			return result;
		}

		public static RValue Sum(params RValue[] rValues)
		{
			var stage = rValues[0];

			for (var i = 1; i < rValues.Length; i++)
			{
				var rVal = rValues[i];
				var nrmStage = RNormalizer.Normalize(stage, rVal, out var nrmRVal);

				var t = nrmStage.Add(nrmRVal);
				
				var st = BigIntegerHelper.GetDisplay(t);
				Debug.WriteLine($"Still summing, st is {st}.");

				stage = t;
			}

			return stage;
		}


		public static void Test(RValue rValue)
		{
			var dVals = ConvertToDoubles(rValue.Value, rValue.Exponent).ToArray();

			var rVals = dVals.Select(x => ConvertToRValue(x, 0)).ToArray();

			var c = Sum(rVals);

			Debug.WriteLine($"C = {c}.");
		}

		#endregion

		#region Convert to IList<Double>

		public static IList<double> ConvertToDoubles(RValue rValue)
		{
			return ConvertToDoubles(rValue.Value, rValue.Exponent);
		}

		public static IList<double> ConvertToDoubles(BigInteger n, int exponent)
		{
			BigInteger DIVISOR = new BigInteger(Math.Pow(2, 3));

			var result = new List<double>();

			if (n == 0)
			{
				result.Add(0);
			}
			//else if (SafeCastToDouble(n))
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
						result[i] *= Math.Pow(2, exponent + i * 3);
					}
				}
				result.Reverse();
			}

			return result;
		}

		//865 
		///10 give 86, remainder 5

		//86
		///10 gives 8, remainder 6

		//8
		/// 10 gives 0, reminder 8


		#endregion
	}
}
