using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public static class RNormalizer
	{
		// Rectangle & Point
		public static RRectangle Normalize(RRectangle r, RPoint p, out RPoint newP)
		{
			var rTemp = r.Clone();
			var pTemp = p.Clone();

			var newExp = NormalizeInPlace(rTemp, pTemp);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newP = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);

			return result;
		}

		// Rectangle & Size
		public static RRectangle Normalize(RRectangle r, RSize s, out RSize newS)
		{
			var rTemp = r.Clone();
			var sTemp = s.Clone();

			var newExp = NormalizeInPlace(rTemp, sTemp);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			return result;
		}

		// Rectangle & RValue
		public static RRectangle Normalize(RRectangle r, RValue v, out RValue newV)
		{
			var rTemp = r.Clone();
			var vTemp = v.Clone();

			var newExp = NormalizeInPlace(rTemp, vTemp);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newV = v.Exponent == newExp ? v : new RValue(vTemp.Value, newExp, vTemp.Precision);

			return result;
		}

		// Point and Size
		public static RPoint Normalize(RPoint p, RSize s, out RSize newS)
		{
			var pTemp = p.Clone();
			var sTemp = s.Clone();

			var newExp = NormalizeInPlace(pTemp, sTemp);
			var result = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			return result;
		}

		// Two Points
		public static RPoint Normalize(RPoint p1, RPoint p2, out RPoint newP2)
		{
			var p1Temp = p1.Clone();
			var p2Temp = p2.Clone();

			var newExp = NormalizeInPlace(p1Temp, p2Temp);
			var result = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
			newP2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);

			return result;
		}

		// Two Sizes
		public static RSize Normalize(RSize s1, RSize s2, out RSize newS2)
		{
			var s1Temp = s1.Clone();
			var s2Temp = s2.Clone();

			var newExp = NormalizeInPlace(s1Temp, s2Temp);
			var result = s1.Exponent == newExp ? s1 : new RSize(s1Temp.Values, newExp);
			newS2 = s2.Exponent == newExp ? s2 : new RSize(s2Temp.Values, newExp);

			return result;
		}

		// RVector and Size
		public static RVector Normalize(RVector v, RSize s, out RSize newS)
		{
			var pTemp = v.Clone();
			var sTemp = s.Clone();

			var newExp = NormalizeInPlace(pTemp, sTemp);
			var result = v.Exponent == newExp ? v : new RVector(pTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			return result;
		}

		// Point and Vector
		public static RPoint Normalize(RPoint p, RVector v, out RVector newV)
		{
			var pTemp = p.Clone();
			var vTemp = v.Clone();

			var newExp = NormalizeInPlace(pTemp, vTemp);
			var result = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			newV = v.Exponent == newExp ? v : new RVector(vTemp.Values, newExp);

			return result;
		}

		// Two RValues
		public static RValue Normalize(RValue r, RValue v, out RValue newV)
		{
			var rTemp = r.Clone();
			var vTemp = v.Clone();

			var newExp = NormalizeInPlace(rTemp, vTemp);
			var result = r.Exponent == newExp ? r : new RValue(rTemp.Value, newExp, rTemp.Precision);
			newV = v.Exponent == newExp ? v : new RValue(vTemp.Value, newExp, vTemp.Precision);

			return result;
		}

		public static int NormalizeInPlace(IBigRatShape a, IBigRatShape b)
		{
			var reductionFactor = -1 * GetReductionFactor(a, b);

			int result;

			if (a.Exponent > b.Exponent)
			{
				result = b.Exponent - reductionFactor;
				ScaleBInPlace(a.Values, reductionFactor + (a.Exponent - b.Exponent));
				ScaleBInPlace(b.Values, reductionFactor);
			}
			else if (b.Exponent > a.Exponent)
			{
				result = a.Exponent - reductionFactor;
				ScaleBInPlace(a.Values, reductionFactor);
				ScaleBInPlace(b.Values, reductionFactor + (b.Exponent - a.Exponent));
			}
			else
			{
				ScaleBInPlace(a.Values, reductionFactor);
				ScaleBInPlace(b.Values, reductionFactor);

				result = a.Exponent - reductionFactor;
			}

			return result;
		}

		private static int GetReductionFactor(IBigRatShape a, IBigRatShape b)
		{
			var result = 0;

			long divisor = 2;

			while (a.Exponent + result < 0 && IsDivisibleBy(a.Values, divisor) 
				&& b.Exponent + result < 0 && IsDivisibleBy(b.Values, divisor))
			{
				result++;
				divisor *= 2;
			}

			return result;
		}

		private static bool IsDivisibleBy(BigInteger[] dividends, long divisor)
		{
			for (var i = 0; i < dividends.Length; i++)
			{
				if (BigInteger.Remainder(dividends[i], divisor) !=0) 
				{
					return false;
				}
			}

			return true;
		}

		public static BigInteger[] ScaleB(BigInteger[] vals, int exponentDelta)
		{
			BigInteger[] result;
			if (exponentDelta < 0)
			{
				var factor = (long)Math.Pow(2, -1 * exponentDelta);
				result = vals.Select(v => v / factor).ToArray();
			}
			else if (exponentDelta > 0)
			{
				var factor = (long)Math.Pow(2, exponentDelta);
				result = vals.Select(v => v * factor).ToArray();
			}
			else
			{
				result = vals;
			}

			return result;
		}

		private static void ScaleBInPlace(BigInteger[] values, int exponentDelta)
		{
			if (exponentDelta < 0)
			{
				var factor = BigInteger.Pow(2, -1 * exponentDelta); // (long)Math.Pow(2, -1 * exponentDelta);
				for (var i = 0; i < values.Length; i++)
				{
					values[i] /= factor;
				}
			}
			else if (exponentDelta > 0)
			{
				var factor = BigInteger.Pow(2, exponentDelta); // (long)Math.Pow(2, exponentDelta);
				for (var i = 0; i < values.Length; i++)
				{
					values[i] *= factor;
				}
			}
			else
			{
				// Nothing to do, the delta is zero
				return;
			}
		}

		//public static void Validate(IBigRatShape bigRatShape)
		//{
		//	var vals = Reducer.Reduce(bigRatShape, out var exponent);

		//	if (bigRatShape.Exponent != exponent)
		//	{
		//		throw new InvalidOperationException("Normalize did not reduce.");
		//	}

		//	for(var i = 0; i < vals.Length; i++)
		//	{
		//		if (vals[i] != bigRatShape.Values[i])
		//		{
		//			throw new InvalidOperationException("Normalize did not reduce -- val check.");
		//		}
		//	}
		//}

	}
}
