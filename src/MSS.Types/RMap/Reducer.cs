using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public static class Reducer
	{
		public static RValue Reduce(RValue rValue)
		{
			var val = Reduce(rValue.Value, rValue.Exponent, out var exponent);
			return new RValue(val, exponent);
		}

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

		public static RRectangle Reduce(RRectangle rRectangle)
		{
			var vals = Reduce(rRectangle, out var exponent);
			return new RRectangle(vals, exponent);
		}

		public static BigInteger[] Reduce(IBigRatShape bigRatShape, out int newExponent)
		{
			var result = Reduce(bigRatShape.Values, bigRatShape.Exponent, out newExponent);
			return result;
		}

		public static BigInteger[] Reduce(BigInteger[] vals, int exponent, out int newExponent)
		{
			var reductionFactor = 0;
			long divisor = 1;

			while (exponent + reductionFactor < 0 && IsDivisibleBy(vals, divisor * 2))
			{
				reductionFactor++;
				divisor *= 2;
			}

			newExponent = exponent + reductionFactor;
			var result = reductionFactor == 0 ? vals : vals.Select(v => v / divisor).ToArray();
			return result;
		}

		public static BigInteger Reduce(BigInteger value, int exponent, out int newExponent)
		{
			var reductionFactor = 0;
			long divisor = 1;

			while (exponent + reductionFactor < 0 && BigInteger.Remainder(value, divisor) == 0)
			{
				reductionFactor++;
				divisor *= 2;
			}

			newExponent = exponent + reductionFactor;
			var result = value / divisor;
			return result;
		}

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

	}
}
