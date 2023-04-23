using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public static class Reducer
	{
		#region Shapes

		public static RPoint Reduce(RPoint rPoint)
		{
			var vals = Reduce(rPoint, out var exponent);
			return new RPoint(vals, exponent);
		}

		public static RSize Reduce(RSize rPoint)
		{
			var vals = Reduce(rPoint, out var exponent);
			return new RSize(vals, exponent);
		}

		public static RVector Reduce(RVector rVector)
		{
			var vals = Reduce(rVector, out var exponent);
			return new RVector(vals, exponent);
		}

		public static RRectangle Reduce(RRectangle rRectangle)
		{
			var vals = Reduce(rRectangle, out var exponent);
			return new RRectangle(vals, exponent);
		}

		public static RPointAndDelta Reduce(RPointAndDelta rPointAndDelta)
		{
			var vals = Reduce(rPointAndDelta, out var exponent);
			return new RPointAndDelta(vals, exponent);
		}

		private static BigInteger[] Reduce(IBigRatShape bigRatShape, out int newExponent)
		{
			var result = Reduce(bigRatShape.Values, bigRatShape.Exponent, out newExponent);
			return result;
		}

		private static BigInteger[] Reduce(BigInteger[] vals, int exponent, out int newExponent)
		{
			var reductionFactor = 0;
			long divisor = 1;

			while (exponent + reductionFactor + 1 < 0 && IsDivisibleBy(vals, divisor * 2))
			{
				reductionFactor++;
				divisor *= 2;
			}

			newExponent = exponent + reductionFactor;
			var result = reductionFactor == 0 ? vals : vals.Select(v => v / divisor).ToArray();
			return result;
		}

		// TODO: Use BigInteger instead of long for the divisor
		private static bool IsDivisibleBy(BigInteger[] dividends, long divisor)
		{
			for (var i = 0; i < dividends.Length; i++)
			{
				if (BigInteger.Remainder(dividends[i], divisor) != 0)
				{
					return false;
				}
			}

			return true;
		}

		#endregion

		#region RValue

		public static RValue Reduce(RValue rValue)
		{
			var val = Reduce(rValue.Value, rValue.Exponent, out var exponent);
			return new RValue(val, exponent, rValue.Precision);
		}

		private static BigInteger Reduce(BigInteger value, int exponent, out int newExponent)
		{
			if (value == 0)
			{
				newExponent = exponent;
				return value;
			}

			if (Math.Abs(exponent) > 63)
			{
				value = ReduceByLongFactor(value, exponent, out exponent);
			}

			var reductionFactor = 0;
			long divisor = 1;

			while (exponent + reductionFactor + 1 < 0 && BigInteger.Remainder(value, divisor * 2) == 0)
			{
				reductionFactor++;
				divisor *= 2;
			}

			newExponent = exponent + reductionFactor;
			var result = value / divisor;
			return result;
		}

		private static BigInteger ReduceByLongFactor(BigInteger value, int exponent, out int newExponent)
		{
			var reductionFactor = 0;

			var remainder = BigInteger.Remainder(value, BigInteger.Pow(2, reductionFactor + 64));

			while (exponent + reductionFactor + 64 < 0 && remainder == 0)
			{
				reductionFactor += 64;
				remainder = BigInteger.Remainder(value, BigInteger.Pow(2, reductionFactor + 64));
			}

			newExponent = exponent + reductionFactor;
			var result = value / BigInteger.Pow(2, reductionFactor);
			return result;
		}

		#endregion
	}
}
