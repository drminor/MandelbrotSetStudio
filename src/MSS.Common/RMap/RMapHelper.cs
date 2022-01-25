using MSS.Types;
using System;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		#region Normalize

		// Rectangle & Point
		public static RRectangle Normalize(RRectangle r, RPoint p, out RPoint newP)
		{
			var rTemp = r.Clone();
			var pTemp = p.Clone();

			var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newP = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);

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
		}

		// Rectangle & Size
		public static RRectangle Normalize(RRectangle r, RSize s, out RSize newS)
		{
			var rTemp = r.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
			var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

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
		}

		// Point and Size
		public static RPoint Normalize(RPoint p, RSize s, out RSize newS)
		{
			var pTemp = p.Clone();
			var sTemp = s.Clone();

			var newExp = Normalize(pTemp.Values, sTemp.Values, p.Exponent, s.Exponent);
			var result = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
			newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

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
		}

		// Two Points
		public static RPoint Normalize(RPoint p1, RPoint p2, out RPoint newP2)
		{
			var p1Temp = p1.Clone();
			var p2Temp = p2.Clone();

			var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
			var result = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
			newP2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);

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
		}

		// Two Sizes
		public static RSize Normalize(RSize s1, RSize s2, out RSize news2)
		{
			var s1Temp = s1.Clone();
			var s2Temp = s2.Clone();

			var newExp = Normalize(s1Temp.Values, s2Temp.Values, s1.Exponent, s2.Exponent);
			var result = s1.Exponent == newExp ? s1 : new RSize(s1Temp.Values, newExp);
			news2 = s2.Exponent == newExp ? s2 : new RSize(s2Temp.Values, newExp);

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

			while(IsDivisibleBy(a, divisor) && IsDivisibleBy(b, divisor))
			{
				result++;
				divisor *= 2;
			}

			return result;
		}

		private static bool IsDivisibleBy(BigInteger[] dividends, long divisor)
		{
			for(var i = 0; i < dividends.Length; i++)
			{
				_ = BigInteger.DivRem(dividends[i], divisor, out var remainder);
				if (remainder != 0)
				{
					return false;
				}
			}

			return true;
		}

		public static BigInteger[] ScaleB(BigInteger[] vals, int exponentDelta)
		{
			if (exponentDelta < 0)
			{
				throw new InvalidOperationException($"Cannot ScaleBInPlace using an exponentDelta < 0. The exponentDelta is {exponentDelta}.");
			}

			if (exponentDelta == 0)
			{
				return vals;
			}

			var factor = (long)Math.Pow(2, exponentDelta);
			var result = vals.Select(v => v * factor).ToArray();

			return result;
		}

		private static void ScaleBInPlace(BigInteger[] values, int exponentDelta)
		{
			if (exponentDelta < 0)
			{
				//throw new InvalidOperationException($"Cannot ScaleBInPlace using an exponentDelta < 0. The exponentDelta is {exponentDelta}.");

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

		#endregion

		#region Map Area Support

		public static RRectangle GetMapCoords(RectangleInt area, RPoint position, RSize samplePointDelta)
		{
			// Create a rational rectangle on the complex plane
			// that corresponds to the position and size of the area in pixels
			// with origin = 0, 0.
			var result = ScaleAreaInt(area, samplePointDelta);

			// Prepare the position and scaled area for translation.
			NormalizeInPlace(ref result, ref position);

			// Use the position to move the scaled area.
			result = result.Translate(position);

			return result;
		}

		private static RRectangle ScaleAreaInt(RectangleInt area, RSize factor)
		{
			var result = new RRectangle(area.X1 * factor.Width, area.X2 * factor.Width,
				area.Y1 * factor.Height, area.Y2 * factor.Height, factor.Exponent);

			return result;
		}

		#endregion

		#region Job Creation

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

		public static RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSize)
		{
			var newNumerator = canvasSize.Width > canvasSize.Height
				? BigIntegerHelper.Divide(coords.WidthNumerator, coords.Exponent, canvasSize.Width, out var newExponent)
				: BigIntegerHelper.Divide(coords.HeightNumerator, coords.Exponent, canvasSize.Height, out newExponent);

			var result = new RSize(newNumerator, newNumerator, newExponent);

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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapOrigin">The coordinates on the complex plane for the block at screen position: x:0, y:0</param>
		/// <param name="subdivisionOrigin">The coordinates on the complex plane for the block at position: x:0, y:0</param>
		/// <param name="samplePointDelta">The width and height of the section of the complex plane, corresponding to 1 pixel</param>
		/// <param name="blockSize">The width and height of a block in pixels.</param>
		/// <returns></returns>
		public static SizeInt GetMapBlockOffset(RPoint mapOrigin, RPoint subdivisionOrigin, RSize samplePointDelta, SizeInt blockSize)
		{
			// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
			// The screen origin in the left, bottom corner and the left, bottom corner of the map is displayed here.

			var sourceOrigin = mapOrigin;
			var destinatinOrigin = subdivisionOrigin;
			NormalizeInPlace(ref sourceOrigin, ref destinatinOrigin);

			var mDistance = sourceOrigin.Diff(destinatinOrigin);

			// The width and height of the section of the complex plane, corresponding to 1 block.
			var coordBlockSize = samplePointDelta.Scale(blockSize);
			NormalizeInPlace(ref mDistance, ref coordBlockSize);

			var width = RoundToBlock(mDistance.Width, coordBlockSize.Width);
			var height = RoundToBlock(mDistance.Height, coordBlockSize.Height);
			var result = new SizeInt((int)width, (int)height);

			return result;
		}

		private static BigInteger RoundToBlock(BigInteger x, BigInteger blockLength)
		{
			if (x == 0)
			{
				return 0;
			}

			var result = BigInteger.DivRem(x, blockLength, out var remainder);

			if (remainder != 0)
			{
				result++;
			}

			return result;
		}

		#endregion
	}
}
