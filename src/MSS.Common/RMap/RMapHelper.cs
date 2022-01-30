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

		public static RPoint GetNewMapPosition(PointInt screenPos, RPoint position, RSize samplePointDelta)
		{
			// Multiply the screen position to convert to map coordinates.
			//var rsPos = ScaleByRsize(screenPos, samplePointDelta);
			var rsPos = ScaleByRSize(screenPos, samplePointDelta);

			// Translate the map position by the screen position.
			var result = position.Translate(rsPos);

			result = Reducer.Reduce(result);

			return result;
		}

		private static RPoint ScaleByRSize(PointInt pos, RSize factor)
		{
			var result = new RPoint(pos.X * factor.Width, pos.Y * factor.Width, factor.Exponent);
			return result;
		}

		public static RRectangle GetMapCoords(RectangleInt area, RPoint position, RSize samplePointDelta)
		{
			// Multiply the area by samplePointDelta to convert to map coordinates.
			var rArea = ScaleByRsize(area, samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RN.Normalize(rArea, position, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			return result;
		}

		private static RRectangle ScaleByRsize(RectangleInt area, RSize factor)
		{
			var result = new RRectangle(area.X1 * factor.Width, area.X2 * factor.Width, area.Y1 * factor.Height, area.Y2 * factor.Height, factor.Exponent);
			return result;
		}

		#endregion

		#region Job Creation V1

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

		public static RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSize)
		{
			var newNumerator = canvasSize.Width > canvasSize.Height
				? BigIntegerHelper.Divide(coords.WidthNumerator, coords.Exponent, canvasSize.Width, out var newExponent)
				: BigIntegerHelper.Divide(coords.HeightNumerator, coords.Exponent, canvasSize.Height, out newExponent);

			var result = new RSize(newNumerator, newNumerator, newExponent);

			return result;
		}

		public static RSize GetSamplePointDeltaV1(ref RRectangle coords, SizeInt canvasSize)
		{
			double expansionRatio;

			if (canvasSize.Width > canvasSize.Height)
			{
				var displayWidth = StepUpToNextPow(canvasSize.Width);
				expansionRatio = (double)displayWidth / canvasSize.Width;
			}
			else
			{
				var displayHeight = StepUpToNextPow(canvasSize.Height);
				expansionRatio = displayHeight / canvasSize.Height;
			}

			// Increase the canvas size to the next largest power of. 2 (for example: 768 => 1024)
			var expandedCanvasSize = canvasSize.Scale(expansionRatio);

			// Increase the width and height of the of map coordinates in the same proportion.
			var expandedMapSize = coords.Size.Scale(new SizeDbl(expansionRatio, expansionRatio));

			// Calculate a "nice" sample point size with these diminesions.
			var result = GetAdjustedSamplePointDelta(expandedMapSize, expandedCanvasSize);

			var adjMapSize = result.Scale(canvasSize);
			var nrmPos = RN.Normalize(coords.Position, adjMapSize, out var nrmAdjMapSize);
			coords = new RRectangle(nrmPos, nrmAdjMapSize);

			return result;
		}

		public static RSize GetAdjustedSamplePointDelta(RSize mapSize, SizeInt canvasSize)
		{
			var newNumerator = canvasSize.Width > canvasSize.Height
				? BigIntegerHelper.Divide(mapSize.Width, mapSize.Exponent, canvasSize.Width, out var newExponent)
				: BigIntegerHelper.Divide(mapSize.Height, mapSize.Exponent, canvasSize.Height, out newExponent);

			var result = new RSize(newNumerator, newNumerator, newExponent);

			return result;
		}

		#endregion

		#region Job Creation V2

		public static SizeInt GetCanvasSize2(SizeInt newArea, SizeInt canvasControlSize)
		{
			var wRatio = (double)newArea.Width / canvasControlSize.Width;
			var hRatio = (double)newArea.Height / canvasControlSize.Height;

			int w;
			int h;

			if (wRatio > hRatio)
			{
				// Width of image in pixels will take up the entire control.
				w = canvasControlSize.Width;

				// Height of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var hRat = (double)newArea.Height / newArea.Width;
				h = (int)Math.Round(canvasControlSize.Width * hRat);
			}
			else
			{
				// Width of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var wRat = (double)newArea.Width / newArea.Height;
				w = (int)Math.Round(canvasControlSize.Height * wRat);

				// Height of image in pixels will take up the entire control.
				h = canvasControlSize.Width;
			}

			var result = new SizeInt(w, h);

			return result;
		}

		public static RSize GetSamplePointDelta2(ref RRectangle coords, SizeInt newArea, RSize screenSizeToMapRat, SizeInt canvasSize)
		{
			double expansionRatio;

			if (canvasSize.Width > canvasSize.Height)
			{
				var displayWidth = StepUpToNextPow(canvasSize.Width);
				expansionRatio = (double)displayWidth / canvasSize.Width;
			}
			else
			{
				var displayHeight = StepUpToNextPow(canvasSize.Height);
				expansionRatio = (double)displayHeight / canvasSize.Height;
			}

			// Increase the canvas size to the next largest power of. 2 (for example: 768 => 1024)
			var expandedCanvasSize = canvasSize.Scale(expansionRatio);

			// Increase the width and height of the of map coordinates in the same proportion.
			var expandedMapSize = coords.Size.Scale(new SizeDbl(expansionRatio, expansionRatio));

			var expandedArea = new SizeDbl(newArea.Width * expansionRatio, newArea.Height * expansionRatio);

			// Calculate a "nice" sample point size with these diminesions.
			var result = GetAdjustedSamplePointDelta2(expandedMapSize, expandedArea, screenSizeToMapRat, expandedCanvasSize);

			// Use the original # of sample points and multiply by the new sample point size
			// to get a new map size.
			var adjMapSize = result.Scale(canvasSize);

			// Create an updated coord value with the new size.
			var newCoords = CombinePosAndSize(coords.Position, adjMapSize);
			Debug.WriteLine($"The new coords are : {newCoords},\n old = {coords}. (While calculating SamplePointDelta.)");
			coords = newCoords;

			return result;
		}

		private static RSize GetAdjustedSamplePointDelta2(RSize mapSize, SizeDbl screenSize, RSize screenSizeToMapRat, SizeInt canvasSize)
		{
			int newExponent;
			BigInteger newNumerator;


			if (canvasSize.Width > canvasSize.Height)
			{
				//var sW = GetClosestPow(screenSize.Width);
				//var mW = sW * screenSizeToMapRat.Width;
				//newNumerator = BigIntegerHelper.Divide(mW, mapSize.Exponent, canvasSize.Width, out newExponent);
				newNumerator = BigIntegerHelper.Divide(mapSize.Width, mapSize.Exponent, canvasSize.Width, out newExponent);
			}
			else
			{
				//var sH = GetClosestPow(screenSize.Height);
				//var mH = sH * screenSizeToMapRat.Height;
				//newNumerator = BigIntegerHelper.Divide(mH, mapSize.Exponent, canvasSize.Height, out newExponent);
				newNumerator = BigIntegerHelper.Divide(mapSize.Height, mapSize.Exponent, canvasSize.Height, out newExponent);
			}

			var result = new RSize(newNumerator, newNumerator, newExponent);

			return result;
		}

		public static RRectangle CombinePosAndSize(RPoint pos, RSize size)
		{
			var nrmPos = RN.Normalize(pos, size, out var nrmSize);
			var result = new RRectangle(nrmPos, nrmSize);

			RN.Validate(result);

			return result;
		}

		private static int GetClosestPow(double x)
		{
			var l = Math.Log2(x);
			var lr = Math.Round(l);

			var result = (int)Math.Pow(2, lr);
			return result;
		}

		private static int StepUpToNextPow(int x)
		{
			var l = Math.Log2(x);
			var lc = Math.Ceiling(l);

			var result = (int) Math.Pow(2, lc);
			return result;
		}

		#endregion

		#region Job Block Support

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
		public static SizeInt GetMapBlockOffset(ref RRectangle mapCoords, RPoint subdivisionOrigin, RSize samplePointDelta, SizeInt blockSize, out SizeDbl samplesRemaining)
		{
			SizeInt result;
			RRectangle newCoords;

			Debug.WriteLine($"Our origin is {mapCoords.LeftBot}");
			Debug.WriteLine($"Destination origin is {subdivisionOrigin}");

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

				newCoords = mapCoords;
				samplesRemaining = new SizeDbl();
				result = new SizeInt();
			}
			else
			{
				Debug.WriteLine($"The offset from the subOrigin is {mDistance}.");

				// Determine # of sample points are in the mDistance extents.
				var offsetInSamplePointsDC = GetNumberOfSamplePointsDiag(mapCoords.LeftBot, subdivisionOrigin, samplePointDelta, out var mDistanceDC);
				Debug.WriteLine($"The raw offset from the subOrigin is {mDistanceDC}.");

				var offset = RN.Normalize(mDistance, samplePointDelta, out var spd);
				var offSetInSamplePoints = GetNumberOfSamplePoints(offset, spd);

				Debug.WriteLine($"The offset in samplePoints is {offSetInSamplePoints}. Compare: {offsetInSamplePointsDC}.");

				// Calculate the new coords using the calculated offset and the subdivision's origin
				newCoords = RecalculateCoords(coords, destinationOrigin, offSetInSamplePoints, spd);

				// Adjust the coordinates to get a better samplePointDelta, etc.
				//mapCoords = JiggerCoords(coords, newCoords, spd, ref offSetInSamplePoints);
				//mapCoords = newCoords;

				// Get # of whole blocks and the # of pixels left over
				var offSetInBlocks = GetOffsetAndRemainder(offSetInSamplePoints, blockSize, out var offSetRemainderInSamplePoints);
				Debug.WriteLine($"The offset in blocks is {offSetInBlocks}.");
				Debug.WriteLine($"The offset in sample points before including BS is {offSetRemainderInSamplePoints}.");

				samplesRemaining = GetSamplesRemaining(offSetRemainderInSamplePoints, blockSize);
				Debug.WriteLine($"The remainder offset in sample points is {samplesRemaining}.");

				result = offSetInBlocks;
			}

			Debug.WriteLine($"The new coords are : {newCoords},\n old = {mapCoords}. (While calculating the MapBlockOffset.)");
			mapCoords = Reducer.Reduce(newCoords);

			return result;
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
			RRectangle result;

			if (offsetInSamplePoints.Width == 0 && offsetInSamplePoints.Height == 0)
			{
				result = new RRectangle(coords.Position, coords.Size);
			}
			else
			{
				var normalizedOffset = samplePointDelta.Scale(offsetInSamplePoints);
				RN.NormalizeInPlace(ref destinationOrigin, ref normalizedOffset);
				var newOrigin = destinationOrigin.Translate(normalizedOffset);

				var newSize = coords.Size.Clone(); // new RSize(coords.WidthNumerator, coords.HeightNumerator, coords.Exponent);
				RN.NormalizeInPlace(ref newOrigin, ref newSize);
				result = new RRectangle(newOrigin, newSize);
			}

			result = Reducer.Reduce(result);
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
	}
}
