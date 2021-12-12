using MSS.Types;
using System;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		#region Normalize

		public static RRectangle Normalize(RRectangle r, RPoint p, out RPoint newP)
		{
			var rTemp = r.Clone();
			var pTemp = p.Clone();

			var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newP = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);

			return result;
		}

		public static void NormalizeInPlace(ref RRectangle r, ref RPoint p)
		{
			var rTemp = r.Clone();
			var pTemp = p.Clone();

			var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
			r = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			p = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
		}

		public static RPoint Normalize(RPoint p, RSize s, out RSize newS)
		{
			var pTemp = p.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(pTemp.Values, sTemp.Values, p.Exponent, s.Exponent);
			var result = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			return result;
		}

		public static void NormalizeInPlace(ref RPoint p, ref RSize s)
		{
			var pTemp = p.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(pTemp.Values, sTemp.Values, p.Exponent, s.Exponent);
			p = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			s = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);
		}

		public static int Normalize(BigInteger[] a, BigInteger[] b, int exponentA, int exponentB)
		{
			var reductionFactor = GetReductionFactor(a, b);

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

			while(IsDivisibleBy(a, divisor) && IsDivisibleBy(b, divisor))
			{
				result++;
				divisor += 2;
			}

			return result;
		}

		private static bool IsDivisibleBy(BigInteger[] dividends, long divisor)
		{
			for(var i = 0; i < dividends.Length; i++)
			{
				_ = BigInteger.DivRem(dividends[i], divisor, out var remainder);
				if (remainder > 0)
				{
					return false;
				}
			}

			return true;
		}

		public static BigInteger[] ScaleB(BigInteger[] vals, int exponentDelta)
		{
			if (exponentDelta == 0)
			{
				return vals;
			}

			var factor = (long)Math.Pow(2, -1 * exponentDelta);
			var result = vals.Select(v => v * factor).ToArray();

			return result;
		}

		private static void ScaleBInPlace(BigInteger[] values, int exponentDelta)
		{
			if (exponentDelta == 0)
			{
				return;
			}

			var factor = (long)Math.Pow(2, -1 * exponentDelta);

			for (var i = 0; i < values.Length; i++)
			{
				values[i] *= factor;
			}
		}

		#endregion

		#region Map Area Support

		public static RRectangle GetMapCoords(RectangleInt area, RPoint position, RSize samplePointDelta)
		{
			var result = ScaleAreaInt(area, samplePointDelta);
			NormalizeInPlace(ref result, ref position);
			result = result.Translate(position);

			return result;
		}

		private static RRectangle ScaleAreaInt(RectangleInt area, RSize factor)
		{
			var result = new RRectangle(area.X1 * factor.Width, area.X2 * factor.Width,
				area.Y1 * factor.Height, area.Y2 * factor.Height, factor.Exponent);

			return result;
		}

		public static SizeInt GetCanvasSizeInBlocks(RRectangle coords, RSize samplePointDelta, SizeInt blockSize)
		{
			//var id = ObjectId.GenerateNewId();
			//var origin = new RPoint();

			//// TODO: Calculate the number of blocks to cover the map area.
			////		then figure the difference in map coordinates from the beginning and end of a single block
			//var samplePointDelta = new RSize(BigInteger.One, BigInteger.One, -8);

			//var result = new Subdivision(id, origin, blockSize, samplePointDelta);

			//return result;

			var result = new SizeInt();

			return result;
		}

		///// <summary>
		///// 
		///// </summary>
		///// <param name="canvasExtent"></param>
		///// <param name="coordExtent"></param>
		///// <param name="coordExp"></param>
		///// <returns>Extent in X and SampleWidth in Y</returns>
		//private static RPoint GetExtentAndSampleWidth(int canvasExtent, BigInteger coordExtent, int coordExp)
		//{
		//	RPoint result = new RPoint();

		//	return result;
		//}

		//private static BigInteger GetExtent(int canvasExtent, BigInteger coordExtent, int coordExp)
		//{
		//	BigInteger result = new BigInteger();

		//	return result;
		//}

		#endregion

		#region NOT USED

		public static long Divide4(long n, int exp, out int newExp)
		{
			long result;

			var half = Math.DivRem(n, 2, out var remainder);

			if (remainder == 0)
			{
				var quarter = Math.DivRem(n, 4, out remainder);

				if (remainder == 0)
				{
					result = quarter;
					newExp = exp;
				}
				else
				{
					result = half;
					newExp = exp - 1;
				}
			}
			else
			{
				result = n;
				newExp = exp - 2;
			}

			return result;
		}

		public static int GetValueDepth(RRectangle _)
		{
			// TODO: Calculate the # of maximum binary bits of precision from sx, ex, sy and ey.
			var binaryBitsOfPrecision = 10;
			var valueDepth = CalculateValueDepth(binaryBitsOfPrecision);

			return valueDepth;
		}

		private static int CalculateValueDepth(int binaryBitsOfPrecision)
		{
			var result = Math.DivRem(binaryBitsOfPrecision, 53, out var remainder);

			if (remainder > 0) result++;

			return result;
		}

		#endregion
	}
}
