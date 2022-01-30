using MSS.Types;
using System;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	public static class RNormalizer
	{
		// Rectangle & Point
		public static RRectangle Normalize(RRectangle r, RPoint p, out RPoint newP)
		{
			var rTemp = r.Clone();
			var pTemp = p.Clone();

			var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newP = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);

			//Validate(result);
			//Validate(newP);
			return result;
		}

		// Rectangle & Point
		public static void NormalizeInPlace(ref RRectangle r, ref RPoint p)
		{
			var rTemp = r.Clone();
			var pTemp = p.Clone();

			var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
			r = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			p = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);

			//Validate(r);
			//Validate(p);
		}

		// Rectangle & Size
		public static RRectangle Normalize(RRectangle r, RSize s, out RSize newS)
		{
			var rTemp = r.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			//Validate(result);
			//Validate(newS);
			return result;
		}

		// Rectangle & Size
		public static void NormalizeInPlace(ref RRectangle r, ref RSize s)
		{
			var rTemp = r.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
			r = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			s = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			//Validate(r);
			//Validate(s);
		}

		// Point and Size
		public static RPoint Normalize(RPoint p, RSize s, out RSize newS)
		{
			var pTemp = p.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(pTemp.Values, sTemp.Values, pTemp.Exponent, sTemp.Exponent);
			var result = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			//Validate(result);
			//Validate(newS);
			return result;
		}

		// Point and Size
		public static void NormalizeInPlace(ref RPoint p, ref RSize s)
		{
			var pTemp = p.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(pTemp.Values, sTemp.Values, p.Exponent, s.Exponent);
			p = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			s = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			//Validate(p);
			//Validate(s);
		}

		// Two Points
		public static RPoint Normalize(RPoint p1, RPoint p2, out RPoint newP2)
		{
			var p1Temp = p1.Clone();
			var p2Temp = p2.Clone();

			var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
			var result = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
			newP2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);

			//Validate(result);
			//Validate(newP2);
			return result;
		}

		// Two Points
		public static void NormalizeInPlace(ref RPoint p1, ref RPoint p2)
		{
			var p1Temp = p1.Clone();
			var p2Temp = p2.Clone();

			var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
			p1 = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
			p2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);

			//Validate(p1);
			//Validate(p2);
		}

		// Two Sizes
		public static RSize Normalize(RSize s1, RSize s2, out RSize newS2)
		{
			var s1Temp = s1.Clone();
			var s2Temp = s2.Clone();

			var newExp = Normalize(s1Temp.Values, s2Temp.Values, s1.Exponent, s2.Exponent);
			var result = s1.Exponent == newExp ? s1 : new RSize(s1Temp.Values, newExp);
			newS2 = s2.Exponent == newExp ? s2 : new RSize(s2Temp.Values, newExp);

			//Validate(result);
			//Validate(newS2);
			return result;
		}

		// Two Sizes
		public static void NormalizeInPlace(ref RSize s1, ref RSize s2)
		{
			var s1Temp = s1.Clone();
			var s2Temp = s2.Clone();

			var newExp = Normalize(s1Temp.Values, s2Temp.Values, s1.Exponent, s2.Exponent);
			s1 = s1.Exponent == newExp ? s1 : new RSize(s1Temp.Values, newExp);
			s2 = s2.Exponent == newExp ? s2 : new RSize(s2Temp.Values, newExp);

			//Validate(s1);
			//Validate(s2);
		}

		public static int Normalize(BigInteger[] a, BigInteger[] b, int exponentA, int exponentB)
		{


			var reductionFactor = -1 * GetReductionFactor(a, b);

			int result;

			if (exponentA > exponentB)
			{
				result = exponentB - reductionFactor;
				ScaleBInPlace(a, reductionFactor + (exponentA - exponentB));
				ScaleBInPlace(b, reductionFactor);
			}
			else if (exponentB > exponentA)
			{
				result = exponentA - reductionFactor;
				ScaleBInPlace(a, reductionFactor);
				ScaleBInPlace(b, reductionFactor + (exponentB - exponentA));
			}
			else
			{
				ScaleBInPlace(a, reductionFactor);
				ScaleBInPlace(b, reductionFactor);

				result = exponentA - reductionFactor;
			}

			return result;
		}

		private static int GetReductionFactor(BigInteger[] a, BigInteger[] b)
		{
			var result = 0;

			long divisor = 2;

			while (IsDivisibleBy(a, divisor) && IsDivisibleBy(b, divisor))
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
				var factor = (long)Math.Pow(2, -1 * exponentDelta);
				for (var i = 0; i < values.Length; i++)
				{
					values[i] /= factor;
				}
			}
			else if (exponentDelta > 0)
			{
				var factor = (long)Math.Pow(2, exponentDelta);
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

		public static void Validate(IBigRatShape bigRatShape)
		{
			var vals = Reducer.Reduce(bigRatShape, out var exponent);

			if (bigRatShape.Exponent != exponent)
			{
				throw new InvalidOperationException("Normalize did not reduce.");
			}

			for(var i = 0; i < vals.Length; i++)
			{
				if (vals[i] != bigRatShape.Values[i])
				{
					throw new InvalidOperationException("Normalize did not reduce -- val check.");
				}
			}

		}
	}
}
