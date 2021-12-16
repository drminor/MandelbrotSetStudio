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

		public static RRectangle Normalize(RRectangle r, RSize s, out RSize newS)
		{
			var rTemp = r.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

			return result;
		}

		public static void NormalizeInPlace(ref RRectangle r, ref RSize s)
		{
			var rTemp = r.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
			r = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			s = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);
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

		public static RPoint Normalize(RPoint p1, RPoint p2, out RPoint newP2)
		{
			var p1Temp = p1.Clone();
			var p2Temp = p2.Clone();

			var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
			var result = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
			newP2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);

			return result;
		}

		public static void NormalizeInPlace(ref RPoint p1, ref RPoint p2)
		{
			var p1Temp = p1.Clone();
			var p2Temp = p2.Clone();

			var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
			p1 = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
			p2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);
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
			if (exponentDelta < 0)
			{
				throw new InvalidOperationException($"Cannot ScaleBInPlace using an exponentDelta < 0. The exponentDelta is {exponentDelta}.");
			}

			if (exponentDelta == 0)
			{
				return;
			}

			var factor = (long)Math.Pow(2, exponentDelta);

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

		public static SizeInt GetCanvasSize(RRectangle coords, SizeInt canvasControlSize)
		{
			var wRatio = BigIntegerHelper.GetRatio(coords.WidthNumerator, canvasControlSize.Width);
			var hRatio = BigIntegerHelper.GetRatio(coords.HeightNumerator, canvasControlSize.Height);

			int w;
			int h;

			if (wRatio > hRatio)
			{
				// Width of image in pixels will take up the entire control.
				w = canvasControlSize.Width;

				// Height of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var hRatB = BigInteger.Divide(coords.HeightNumerator * 1000, coords.WidthNumerator * 1000);
				var hRat = BigIntegerHelper.GetValue(hRatB);
				h = (int) Math.Round(canvasControlSize.Width * hRat);
			}
			else
			{
				// Width of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var wRatB = BigInteger.Divide(coords.WidthNumerator * 1000, coords.HeightNumerator * 1000);
				var wRat = BigIntegerHelper.GetValue(wRatB);
				w = (int)Math.Round(canvasControlSize.Height * wRat);

				// Height of image in pixels will take up the entire control.
				h = canvasControlSize.Width;
			}

			var result = new SizeInt(w, h);

			return result;
		}

		public static SizeInt GetCanvasSizeInBlocks(SizeInt canvasSize, SizeInt blockSize)
		{
			var w = Math.DivRem(canvasSize.Width, blockSize.Width, out var remainder);

			if (remainder > 0)
			{
				w++;
			}

			var h = Math.DivRem(canvasSize.Height, blockSize.Height, out remainder);

			if (remainder > 0)
			{
				h++;
			}

			return new SizeInt(w, h);
		}

		public static PointInt GetCanvasBlockOffset(RPoint mapOrigin, RPoint subdivisionOrigin, SizeInt blockSize)
		{
			// The left-most, bottom-most block is 0, 0 in our cordinates
			// The canvasBlockOffset is the amount added to our block position to address the block in subdivison coordinates.

			var mapO = mapOrigin;
			var subdivisionO = subdivisionOrigin;
			NormalizeInPlace(ref mapO, ref subdivisionO);

			var distance = subdivisionO.Translate(mapO);

			var x = BigInteger.DivRem(BigInteger.Abs(distance.X), new BigInteger(blockSize.Width), out var remainder);

			if (remainder > 0)
			{
				x++;
			}

			var y = BigInteger.DivRem(BigInteger.Abs(distance.Y), new BigInteger(blockSize.Height), out remainder);

			if (remainder > 0)
			{
				y++;
			}

			if (distance.X < 0)
			{
				x = -1 * x;
			}

			if (distance.Y < 0)
			{
				y = -1 * y;
			}

			var result = new PointInt((int)x, (int)y);

			//var result = new PointInt(-4, -3);

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
