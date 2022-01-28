using MSS.Types;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	using RN = RNormalizer;
	public static class RMapHelper
	{
		#region Map Area Support

		public static RRectangle GetMapCoords(RectangleInt area, RPoint position, SizeInt mapBlockOffset, RSize samplePointDelta)
		{
			// adjust the selected area's origin to account for the portion of the start block that is off screen.
			var tPoint = area.Point.Translate(mapBlockOffset);

			// Multiply the position by samplePointDelta to convert to "map" coordinates.
			var offset = samplePointDelta.Scale(tPoint);
			var pos = position.Clone();
			RN.NormalizeInPlace(ref pos, ref offset);
			var newPos = pos.Translate(offset);

			// Multiply the selected area by samplePointDelta to convert to "map" coordinates.
			var newSize = samplePointDelta.Scale(area.Size);
			RN.NormalizeInPlace(ref newPos, ref newSize);

			// Create a new rectangle using the new position and size.
			var result = new RRectangle(newPos, newSize);

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
				var hRat = BigIntegerHelper.ConvertToDouble(hRatB);
				h = (int) Math.Round(canvasControlSize.Width * hRat);
			}
			else
			{
				// Width of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var wRatB = BigInteger.Divide(coords.WidthNumerator * 1000, coords.HeightNumerator * 1000);
				var wRat = BigIntegerHelper.ConvertToDouble(wRatB);
				w = (int)Math.Round(canvasControlSize.Height * wRat);

				// Height of image in pixels will take up the entire control.
				h = canvasControlSize.Width;
			}

			var result = new SizeInt(w, h);

			return result;
		}

		public static RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSize, out RRectangle newCoords)
		{
			//RSize spd;
			//SizeInt expandedCanvasSize;
			//RSize expandedMapSize;

			double expansionRatio;

			if (canvasSize.Width > canvasSize.Height)
			{
				var displayWidth = StepUpToNextPow(canvasSize.Width);
				 expansionRatio = (double) displayWidth / canvasSize.Width;
				//expandedCanvasSize = canvasSize.Scale(expansionRatio);
				//expandedMapSize = coords.Size.Scale(new SizeDbl(expansionRatio, expansionRatio));

				//var newNumerator = BigIntegerHelper.Divide(expandedMapSize.Width, coords.Exponent, displayWidth, out var newExponent);

				//spd = new RSize(newNumerator, newNumerator, newExponent);
			}
			else
			{
				var displayHeight = StepUpToNextPow(canvasSize.Height);
				expansionRatio = displayHeight / canvasSize.Height;
				//expandedCanvasSize = canvasSize.Scale(expansionRatio);
				//expandedMapSize = coords.Size.Scale(new SizeDbl(expansionRatio, expansionRatio));

				//var newNumerator = BigIntegerHelper.Divide(expandedMapSize.Height, coords.Exponent, displayHeight, out var newExponent);

				//spd = new RSize(newNumerator, newNumerator, newExponent);
			}

			var expandedCanvasSize = canvasSize.Scale(expansionRatio);
			var expandedMapSize = coords.Size.Scale(new SizeDbl(expansionRatio, expansionRatio));

			var nrmPos = RN.Normalize(coords.Position, expandedMapSize, out var nrmExpandedSize);
			var expCoords = new RRectangle(nrmPos, nrmExpandedSize);

			var result = GetAdjustedSamplePointDelta(expCoords, expandedCanvasSize);

			var adjMapSize = result.Scale(canvasSize);
			nrmPos = RN.Normalize(coords.Position, adjMapSize, out var nrmAdjMapSize);
			newCoords = new RRectangle(nrmPos, nrmAdjMapSize);

			return result;
		}

		private static RSize GetAdjustedSamplePointDelta(RRectangle coords, SizeInt canvasSize)
		{
			var newNumerator = canvasSize.Width > canvasSize.Height
				? BigIntegerHelper.Divide(coords.WidthNumerator, coords.Exponent, canvasSize.Width, out var newExponent)
				: BigIntegerHelper.Divide(coords.HeightNumerator, coords.Exponent, canvasSize.Height, out newExponent);

			var result = new RSize(newNumerator, newNumerator, newExponent);

			return result;
		}

		private static int StepUpToNextPow(int x)
		{
			var l = Math.Log2(x);
			var lc = Math.Ceiling(l);

			var result = (int) Math.Pow(2, lc);
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

			var result = new SizeInt(w, h);

			return result;
		}

		public static PointInt GetBlockPosition(PointInt posYInverted, SizeInt mapBlockOffset, SizeInt blockSize)
		{
			var pos = posYInverted.Diff(mapBlockOffset);

			var l = Math.DivRem(pos.X, blockSize.Width, out var remainder);
			if (remainder == 0 && l > 0)
			{
				l--;
			}

			var b = Math.DivRem(pos.Y, blockSize.Height, out remainder);
			if (remainder == 0 && b > 0)
			{
				b--;
			}

			var botRight = new PointInt(l, b).Scale(blockSize);
			var center = botRight.Translate(new SizeInt(blockSize.Width / 2, blockSize.Height / 2));
			return center;
		}

		// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
		// The screen origin in the left, bottom corner and the left, bottom corner of the map is displayed here.
		public static SizeInt GetMapBlockOffset(RRectangle mapCoords, RPoint subdivisionOrigin, RSize samplePointDelta, SizeInt blockSize, out SizeDbl samplesRemaining)
		{
			Debug.WriteLine($"Our origin is {BigIntegerHelper.GetDisplay(mapCoords.LeftBot)}");
			Debug.WriteLine($"Destination origin is {BigIntegerHelper.GetDisplay(subdivisionOrigin)}");

			//var ce = Math.Max(Math.Abs(mapCoords.Exponent), Math.Abs(subdivisionOrigin.Exponent));
			//if (mapCoords.Exponent < 0)
			//{
			//	ce *= -1;
			//}

			//var n = new BigInteger(Math.Pow(2, ce));
			//var t = new RSize(n, n, ce);

			//var tmCoords = RN.Normalize(mapCoords, t, out var _);
			//var tsCoords = RN.Normalize(subdivisionOrigin, t, out var _);

			//if (tmCoords.Exponent != tsCoords.Exponent)
			//{
			//	throw new ArgumentException($"GetMapBlockOffset found that the map coordinates and the subdivision are on different scales.");
			//}

			// Using normalize here to minimize the exponent value needed to express these values.
			var coords = RN.Normalize(mapCoords, subdivisionOrigin, out var destinationOrigin);

			var mDistance = coords.LeftBot.Diff(destinationOrigin);

			if (mDistance.Width == 0 && mDistance.Height == 0)
			{
				Debug.WriteLine($"The offset from the subOrigin is Zero.");
				samplesRemaining = new SizeDbl();
				return new SizeInt();
			}

			Debug.WriteLine($"The offset from the subOrigin is {BigIntegerHelper.GetDisplay(mDistance)}.");

			// Determine # of sample points are in the mDistance extents.
			var offsetInSamplePointsDC = GetNumberOfSamplePointsDiag(mapCoords.LeftBot, subdivisionOrigin, samplePointDelta, out var mDistanceDC);
			Debug.WriteLine($"The raw offset from the subOrigin is {mDistanceDC}.");

			var offset = RN.Normalize(mDistance, samplePointDelta, out var spd);
			var offSetInSamplePoints = GetNumberOfSamplePoints(offset, spd);
			
			Debug.WriteLine($"The offset in samplePoints is {offSetInSamplePoints}. Compare: {offsetInSamplePointsDC}.");

			// Calculate the new coords using the calculated offset and the subdivision's origin
			//var newCoords = RecalculateCoords(coords, destinationOrigin, offSetInSamplePoints, spd);
			//Debug.WriteLine($"The new coords are : {BigIntegerHelper.GetDisplay(newCoords)},\n old = {BigIntegerHelper.GetDisplay(mapCoords)}. (While calculating the MapBlockOffset.");

			// Adjust the coordinates to get a better samplePointDelta, etc.
			//mapCoords = JiggerCoords(coords, newCoords, spd, ref offSetInSamplePoints);
			//mapCoords = newCoords;

			// Get # of whole blocks and the # of pixels left over
			var offSetInBlocks = GetOffsetAndRemainder(offSetInSamplePoints, blockSize, out var offSetRemainderInSamplePoints);
			Debug.WriteLine($"The offset in blocks is {offSetInBlocks}.");
			Debug.WriteLine($"The offset in sample points before including BS is {offSetRemainderInSamplePoints}.");

			samplesRemaining = GetSamplesRemaining(offSetRemainderInSamplePoints, blockSize);
			Debug.WriteLine($"The remainder offset in sample points is {samplesRemaining}.");

			return offSetInBlocks;
		}

		// Calculate the number of samplePoints in the given offset.
		// It is assumed that offset is < Integer.MAX * samplePointDelta
		private static SizeInt GetNumberOfSamplePoints(RSize offset, RSize samplePointDelta)
		{
			// # of whole sample points between the source and destination origins.
			var numSamplesH = offset.Width / samplePointDelta.Width;
			var numSamplesV = offset.Height / samplePointDelta.Height;
			var offSetInSamplePoints = new SizeInt((int)numSamplesH, (int)numSamplesV);

			return offSetInSamplePoints;
		}

		private static SizeDbl GetNumberOfSamplePointsDiag(RPoint mapOrigin, RPoint subdivisionOrigin, RSize samplePointDelta, out SizeDbl offset)
		{
			var sx = BigIntegerHelper.ConvertToDouble(mapOrigin.X, mapOrigin.Exponent);
			var sy = BigIntegerHelper.ConvertToDouble(mapOrigin.Y, mapOrigin.Exponent);
			var dx = BigIntegerHelper.ConvertToDouble(subdivisionOrigin.X, subdivisionOrigin.Exponent);
			var dy = BigIntegerHelper.ConvertToDouble(subdivisionOrigin.Y, subdivisionOrigin.Exponent);

			offset = new SizeDbl(sx - dx, sy - dy);

			var spdDC = GetSizeDbl(samplePointDelta);
			var numSamplesH = offset.Width / spdDC.Width;
			var numSamplesV = offset.Height / spdDC.Height;
			var offsetInSamplePoints = new SizeDbl(numSamplesH, numSamplesV);

			return offsetInSamplePoints;
		}

		private static RRectangle RecalculateCoords(RRectangle coords, RPoint destinationOrigin, SizeInt offsetInSamplePoints, RSize samplePointDelta)
		{
			var normalizedOffset = samplePointDelta.Scale(offsetInSamplePoints);
			RN.NormalizeInPlace(ref destinationOrigin, ref normalizedOffset);
			var newOrigin = destinationOrigin.Translate(normalizedOffset);

			var newSize = coords.Size.Clone(); // new RSize(coords.WidthNumerator, coords.HeightNumerator, coords.Exponent);
			RN.NormalizeInPlace(ref newOrigin, ref newSize);
			var result = new RRectangle(newOrigin, newSize);

			return result;
		}

		// The coordinates previously calculated using the exact distance from a particular origin
		// is used to calculate a new set of coordinates having an origin within 5 sample points of the original
		// where the new origin has the smallest absolute value for the exponent.
		private static RRectangle JiggerCoords(RRectangle targetCoords, RRectangle calcCoords, RSize samplePointDelta, ref SizeInt offsetInSamplePoints)
		{
			Debug.WriteLine($"JiggerTarget x:{targetCoords.LeftBot.Values[0]}, y:{targetCoords.LeftBot.Values[1]}, exp:{targetCoords.Exponent}; " +
				$"w:{targetCoords.Size.Values[0]}, h:{targetCoords.Size.Values[1]}.");

			//var cCoords = calcCoords.Clone();
			//var cSpd = samplePointDelta.Clone();
			//NormalizeInPlace(ref cCoords, ref cSpd);

			var cCoords = RN.Normalize(calcCoords, samplePointDelta, out var cSpd);

			for(var pCntr = -2; pCntr < 4; pCntr++)
			{
				var pv = new SizeInt(pCntr, pCntr);
				var p = cCoords.LeftBot.Translate(cSpd.Scale(pv));

				for(var sCntr = -2; sCntr < 4; sCntr++)
				{
					var sv = new SizeInt(sCntr, sCntr);
					var s = cCoords.Size.Translate(cSpd.Scale(sv));
					RN.NormalizeInPlace(ref p, ref s);
					Debug.WriteLine($"{pCntr:D3},{sCntr:D3} :: x:{p.Values[0]}, y:{p.Values[1]}, exp:{p.Exponent}; w:{s.Values[0]}, h:{s.Values[1]}.");
				}
			}

			return calcCoords;
		}

		private static SizeInt GetOffsetAndRemainder(SizeInt offSetInSamplePoints, SizeInt blockSize, out SizeDbl offSetRemainderInSamplePoints)
		{
			// Get # of whole blocks and the # of pixels left over
			var blocksH = RoundToInterval(offSetInSamplePoints.Width, blockSize.Width, out var remainderW);
			var blocksV = RoundToInterval(offSetInSamplePoints.Height, blockSize.Height, out var remainderH);

			var offSetInBlocks = new SizeInt(blocksH, blocksV);
			offSetRemainderInSamplePoints = new SizeDbl(remainderW, remainderH);

			return offSetInBlocks;
		}

		private static int RoundToInterval(int x, int interval, out int remainder)
		{
			if (x == 0)
			{
				remainder = 0;
				return 0;
			}

			var result = Math.DivRem(x, interval, out remainder);

			if (remainder != 0)
			{
				result++;
			}

			return  result;
		}

		private static SizeDbl GetSamplesRemaining(SizeDbl offsetRemainder, SizeInt blockSize)
		{
			var samplesRemaining = new SizeDbl(
				GetSampRem(offsetRemainder.Width, blockSize.Width),
				GetSampRem(offsetRemainder.Height, blockSize.Height)
				);

			return samplesRemaining;
		}

		private static double GetSampRem(double extent, int blockLen)
		{
			if (extent < 0)
			{
				return -1 * (blockLen + extent);
			}
			else
			{
				return extent > 0 ? blockLen - extent : 0;
			}
		}

		private static SizeDbl GetSizeDbl(RSize rSize)
		{
			return new SizeDbl(BigIntegerHelper.ConvertToDouble(rSize.Width, rSize.Exponent), BigIntegerHelper.ConvertToDouble(rSize.Height, rSize.Exponent));
		}

		private static PointDbl GetPointDbl(RPoint rPoint)
		{
			return new PointDbl(BigIntegerHelper.ConvertToDouble(rPoint.X, rPoint.Exponent), BigIntegerHelper.ConvertToDouble(rPoint.Y, rPoint.Exponent));
		}

		#endregion

		//#region Normalize

		//// Rectangle & Point
		//public static RRectangle Normalize(RRectangle r, RPoint p, out RPoint newP)
		//{
		//	var rTemp = r.Clone();
		//	var pTemp = p.Clone();

		//	var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
		//	var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
		//	newP = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);

		//	return result;
		//}

		//// Rectangle & Point
		//public static void NormalizeInPlace(ref RRectangle r, ref RPoint p)
		//{
		//	var rTemp = r.Clone();
		//	var pTemp = p.Clone();

		//	var newExp = Normalize(rTemp.Values, pTemp.Values, r.Exponent, p.Exponent);
		//	r = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
		//	p = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
		//}

		//// Rectangle & Size
		//public static RRectangle Normalize(RRectangle r, RSize s, out RSize newS)
		//{
		//	var rTemp = r.Clone();
		//	var sTemp = s.Clone();

		//	var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
		//	var result = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
		//	newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

		//	return result;
		//}

		//// Rectangle & Size
		//public static void NormalizeInPlace(ref RRectangle r, ref RSize s)
		//{
		//	var rTemp = r.Clone();
		//	var sTemp = s.Clone();

		//	var newExp = Normalize(rTemp.Values, sTemp.Values, r.Exponent, s.Exponent);
		//	r = r.Exponent == newExp ? r : new RRectangle(rTemp.Values, newExp);
		//	s = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);
		//}

		//// Point and Size
		//public static RPoint Normalize(RPoint p, RSize s, out RSize newS)
		//{
		//	var pTemp = p.Clone();
		//	var sTemp = s.Clone();

		//	var newExp = Normalize(pTemp.Values, sTemp.Values, p.Exponent, s.Exponent);
		//	var result = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
		//	newS = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);

		//	return result;
		//}

		//// Point and Size
		//public static void NormalizeInPlace(ref RPoint p, ref RSize s)
		//{
		//	var pTemp = p.Clone();
		//	var sTemp = s.Clone();

		//	var newExp = Normalize(pTemp.Values, sTemp.Values, p.Exponent, s.Exponent);
		//	p = p.Exponent == newExp ? p : new RPoint(pTemp.Values, newExp);
		//	s = s.Exponent == newExp ? s : new RSize(sTemp.Values, newExp);
		//}

		//// Two Points
		//public static RPoint Normalize(RPoint p1, RPoint p2, out RPoint newP2)
		//{
		//	var p1Temp = p1.Clone();
		//	var p2Temp = p2.Clone();

		//	var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
		//	var result = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
		//	newP2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);

		//	return result;
		//}

		//// Two Points
		//public static void NormalizeInPlace(ref RPoint p1, ref RPoint p2)
		//{
		//	var p1Temp = p1.Clone();
		//	var p2Temp = p2.Clone();

		//	var newExp = Normalize(p1Temp.Values, p2Temp.Values, p1.Exponent, p2.Exponent);
		//	p1 = p1.Exponent == newExp ? p1 : new RPoint(p1Temp.Values, newExp);
		//	p2 = p2.Exponent == newExp ? p2 : new RPoint(p2Temp.Values, newExp);
		//}

		//// Two Sizes
		//public static RSize Normalize(RSize s1, RSize s2, out RSize news2)
		//{
		//	var s1Temp = s1.Clone();
		//	var s2Temp = s2.Clone();

		//	var newExp = Normalize(s1Temp.Values, s2Temp.Values, s1.Exponent, s2.Exponent);
		//	var result = s1.Exponent == newExp ? s1 : new RSize(s1Temp.Values, newExp);
		//	news2 = s2.Exponent == newExp ? s2 : new RSize(s2Temp.Values, newExp);

		//	return result;
		//}

		//// Two Sizes
		//public static void NormalizeInPlace(ref RSize s1, ref RSize s2)
		//{
		//	var s1Temp = s1.Clone();
		//	var s2Temp = s2.Clone();

		//	var newExp = Normalize(s1Temp.Values, s2Temp.Values, s1.Exponent, s2.Exponent);
		//	s1 = s1.Exponent == newExp ? s1 : new RSize(s1Temp.Values, newExp);
		//	s2 = s2.Exponent == newExp ? s2 : new RSize(s2Temp.Values, newExp);
		//}

		//public static int Normalize(BigInteger[] a, BigInteger[] b, int exponentA, int exponentB)
		//{
		//	var reductionFactor = -1 * GetReductionFactor(a, b);

		//	int result;

		//	if (exponentA > exponentB)
		//	{
		//		result = exponentB - reductionFactor;
		//		ScaleBInPlace(a, reductionFactor + (exponentA - exponentB));
		//		ScaleBInPlace(b, reductionFactor);
		//	}
		//	else if (exponentB > exponentA)
		//	{
		//		result = exponentA - reductionFactor;
		//		ScaleBInPlace(a, reductionFactor);
		//		ScaleBInPlace(b, reductionFactor + (exponentB - exponentA));
		//	}
		//	else
		//	{
		//		ScaleBInPlace(a, reductionFactor);
		//		ScaleBInPlace(b, reductionFactor);

		//		result = exponentA - reductionFactor;
		//	}

		//	return result;
		//}

		//private static int GetReductionFactor(BigInteger[] a, BigInteger[] b)
		//{
		//	var result = 0;

		//	long divisor = 2;

		//	while (IsDivisibleBy(a, divisor) && IsDivisibleBy(b, divisor))
		//	{
		//		result++;
		//		divisor *= 2;
		//	}

		//	return result;
		//}

		//private static bool IsDivisibleBy(BigInteger[] dividends, long divisor)
		//{
		//	for (var i = 0; i < dividends.Length; i++)
		//	{
		//		_ = BigInteger.DivRem(dividends[i], divisor, out var remainder);
		//		if (remainder != 0)
		//		{
		//			return false;
		//		}
		//	}

		//	return true;
		//}

		//public static BigInteger[] ScaleB(BigInteger[] vals, int exponentDelta)
		//{
		//	if (exponentDelta < 0)
		//	{
		//		throw new InvalidOperationException($"Cannot ScaleBInPlace using an exponentDelta < 0. The exponentDelta is {exponentDelta}.");
		//	}

		//	if (exponentDelta == 0)
		//	{
		//		return vals;
		//	}

		//	var factor = (long)Math.Pow(2, exponentDelta);
		//	var result = vals.Select(v => v * factor).ToArray();

		//	return result;
		//}

		//private static void ScaleBInPlace(BigInteger[] values, int exponentDelta)
		//{
		//	if (exponentDelta < 0)
		//	{
		//		//throw new InvalidOperationException($"Cannot ScaleBInPlace using an exponentDelta < 0. The exponentDelta is {exponentDelta}.");

		//		var factor = (long)Math.Pow(2, -1 * exponentDelta);
		//		for (var i = 0; i < values.Length; i++)
		//		{
		//			values[i] /= factor;
		//		}
		//	}
		//	else if (exponentDelta > 0)
		//	{
		//		var factor = (long)Math.Pow(2, exponentDelta);
		//		for (var i = 0; i < values.Length; i++)
		//		{
		//			values[i] *= factor;
		//		}
		//	}
		//	else
		//	{
		//		// Nothing to do, the delta is zero
		//		return;
		//	}

		//}

		//#endregion
	}
}
