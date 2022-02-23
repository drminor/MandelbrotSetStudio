using MSS.Types;
using System;
using System.Diagnostics;
using System.Numerics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		#region Map Area Support

		public static RRectangle GetMapCoords(RectangleInt area, RPoint position, RSize samplePointDelta)
		{
			// Multiply the area by samplePointDelta to convert to map coordinates.
			var rArea = ScaleByRsize(area, samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RNormalizer.Normalize(rArea, position, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			return result;
		}

		private static RRectangle ScaleByRsize(RectangleInt area, RSize factor)
		{
			var rectangle = new RRectangle(area);
			var result = rectangle.Scale(factor);

			return result;
		}

		#endregion

		#region Job Creation

		public static SizeInt GetCanvasSize(SizeInt newArea, SizeInt canvasControlSize)
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

		public static RSize GetSamplePointDelta(ref RRectangle coords, SizeInt canvasSize)
		{
			var samplePointDelta = canvasSize.Width > canvasSize.Height
				? BigIntegerHelper.Divide(coords.Width, canvasSize.Width)
				: BigIntegerHelper.Divide(coords.Height, canvasSize.Height);

			var result = new RSize(samplePointDelta);

			// The size of the new map is equal to the product of the number of samples by the new samplePointDelta.
			var adjMapSize = result.Scale(canvasSize);

			// Calculate the new map coordinates using the existing position and the new size..
			var newCoords = CombinePosAndSize(coords.Position, adjMapSize);
			Debug.WriteLine($"\nThe new coords are : {newCoords},\n old = {coords}. (While calculating SamplePointDelta3.)\n");

			coords = newCoords;
			return result;
		}

		public static RRectangle CombinePosAndSize(RPoint pos, RSize size)
		{
			var nrmPos = RNormalizer.Normalize(pos, size, out var nrmSize);
			var result = new RRectangle(nrmPos, nrmSize);

			return result;
		}

		#endregion

		#region Job Block Support

		public static SizeInt GetCanvasSizeInBlocks(SizeInt canvasSize, SizeInt blockSize)
		{
			var w = Math.DivRem(canvasSize.Width, blockSize.Width, out var remainderW);
			var h = Math.DivRem(canvasSize.Height, blockSize.Height, out var remainderH);

			w = remainderW > 0 ? w++ : w;
			h = remainderH > 0 ? h++ : h;
			var result = new SizeInt(w, h);

			return result;
		}

		public static SizeInt GetCanvasSizeWholeBlocks(SizeDbl canvasSize, SizeInt blockSize, bool keepSquare)
		{
			var numBlocks = canvasSize.Divide(blockSize);
			var result = numBlocks.Truncate();

			if (keepSquare)
			{
				result = result.GetSquare();
			}

			return result;
		}

		public static SizeInt GetMapExtentInBlocks(SizeInt canvasSizeInBlocks, VectorInt canvasControlOffset)
		{
			var result = new SizeInt(
				canvasSizeInBlocks.Width + (Math.Abs(canvasControlOffset.X) > 0 ? 1 : 0),
				canvasSizeInBlocks.Height + (Math.Abs(canvasControlOffset.Y) > 0 ? 1 : 0)
				);

			return result;
		}

		// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
		// The screen origin in the left, bottom corner and the left, bottom corner of the map is displayed here.
		public static BigVector GetMapBlockOffset(RRectangle mapCoords, RPoint subdivisionOrigin, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			var coords = RNormalizer.Normalize(mapCoords, subdivisionOrigin, out var destinationOrigin);
			var mapOrigin = coords.Position;

			var distance = mapOrigin.Diff(destinationOrigin);
			Debug.WriteLine($"Our origin is {mapCoords.Position}, repo origin is {destinationOrigin}, for a distance of {distance}.");

			BigVector result;
			if (distance.X == 0 && distance.Y == 0)
			{
				canvasControlOffset = new VectorInt();
				result = new BigVector();
			}
			else
			{
				var offSetInSamplePoints = GetNumberOfSamplePoints(distance, samplePointDelta);
				//Debug.WriteLine($"The offset in samplePoints is {offSetInSamplePoints}.");

				result = GetOffsetAndRemainder(offSetInSamplePoints, blockSize, out canvasControlOffset);
				//Debug.WriteLine($"Starting Block Pos: {offSetInBlocks}, Pixel Pos: {canvasControlOffset}.");
			}

			return result;
		}

		// Calculate the number of samplePoints in the given offset.
		private static BigVector GetNumberOfSamplePoints(RVector distance, RSize samplePointDelta)
		{
			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			//var numSamplesH = nrmDistance.XNumerator / samplePointDelta.WidthNumerator;
			//var numSamplesV = nrmDistance.YNumerator / samplePointDelta.HeightNumerator;
			//var offSetInSamplePoints = new BigVector(numSamplesH, numSamplesV);

			var offSetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			return new BigVector(offSetInSamplePoints);
		}

		private static BigVector GetOffsetAndRemainder(BigVector offSetInSamplePoints, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			var blocksH = BigInteger.DivRem(offSetInSamplePoints.X, blockSize.Width, out var remainderH);
			var blocksV = BigInteger.DivRem(offSetInSamplePoints.Y, blockSize.Height, out var remainderV);

			var wholeBlocks = offSetInSamplePoints.DivRem(blockSize, out var remainder);
			Debug.WriteLine($"Whole blocks: {wholeBlocks}, Remaining Pixels: {remainder}.");

			if (remainderH < 0)
			{
				blocksH--;
				remainderH = blockSize.Width + remainderH; // Want to display the last remainderH of the block, so we pull the display blkSize - remainderH to the left.
			}

			if (remainderV < 0)
			{
				blocksV--;
				remainderV = blockSize.Height + remainderV; // Want to display the last remainderV of the block, so we pull the display blkSize - remainderH down.
			}

			var offSetInBlocks = new BigVector(blocksH, blocksV);
			canvasControlOffset = new VectorInt(remainderH, remainderV);

			return offSetInBlocks;
		}

		#endregion

		#region Old Get Offset

		// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
		// The screen origin in the left, bottom corner and the left, bottom corner of the map is displayed here.
		public static BigVector GetMapBlockOffsetV1(RRectangle mapCoords, RPoint subdivisionOrigin, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			BigVector result;

			// Using normalize here to minimize the exponent value needed to express these values.
			var coords = RNormalizer.Normalize(mapCoords, subdivisionOrigin, out var destinationOrigin);
			var mapOrigin = coords.Position;

			Debug.WriteLine($"Our origin is {mapCoords.Position}");
			Debug.WriteLine($"Destination origin is {subdivisionOrigin}");

			var mDistance = mapOrigin.Diff(destinationOrigin);

			if (mDistance.X == 0 && mDistance.Y == 0)
			{
				Debug.WriteLine($"The offset from the subOrigin is Zero.");

				canvasControlOffset = new VectorInt();
				result = new BigVector();
			}
			else
			{
				Debug.WriteLine($"The offset from the subOrigin is {mDistance}.");

				// Determine # of sample points are in the mDistance extents.
				//var offsetInSamplePointsDC = GetNumberOfSamplePointsDiag(mapCoords.LeftBot, subdivisionOrigin, samplePointDelta, out var mDistanceDC);
				//Debug.WriteLine($"The raw offset from the subOrigin is {mDistanceDC}.");

				var offset = RNormalizer.Normalize(mDistance, samplePointDelta, out var spd);
				var offSetInSamplePoints = GetNumberOfSamplePoints(offset, spd);

				//Debug.WriteLine($"The offset in samplePoints is {offSetInSamplePoints}. Compare: {offsetInSamplePointsDC}.");
				Debug.WriteLine($"The offset in samplePoints is {offSetInSamplePoints}.");

				//// Calculate the new coords using the calculated offset and the subdivision's origin
				//newCoords = RecalculateCoords(coords, destinationOrigin, offSetInSamplePoints, spd);

				//// Adjust the coordinates to get a better samplePointDelta, etc.
				//mapCoords = JiggerCoords(coords, newCoords, spd, ref offSetInSamplePoints);
				//mapCoords = newCoords;

				// Get # of whole blocks and the # of pixels left over
				var offSetInBlocks = GetOffsetAndRemainder(offSetInSamplePoints, blockSize, out canvasControlOffset);

				//var oibTest = GetOffsetAndRemainder(offSetInSamplePoints, blockSize, out var ccoTest);

				result = offSetInBlocks;
			}

			//Debug.WriteLine($"The new coords are : {newCoords},\n old = {mapCoords}. (While calculating the MapBlockOffset.)");
			//mapCoords = Reducer.Reduce(newCoords);

			return result;
		}

		//private static SizeInt GetOffsetAndRemainderV1(BigVector offSetInSamplePoints, SizeInt blockSize, out SizeDbl canvasControlOffset)
		//{
		//	var offset = offSetInSamplePoints.Divide(blockSize);
		//	var offsetInBlocks = GetBlocksToCover(offset);

		//	var rem = offset.Diff(offsetInBlocks);
		//	var remS = rem.Scale(blockSize);
		//	var remT = remS.Translate(blockSize);
		//	var remM = remT.Mod(blockSize);
		//	canvasControlOffset = remM.Scale(-1d);
		//	Debug.WriteLine($"Starting Block Pos: {offsetInBlocks}, Pixel Pos: {canvasControlOffset}.");

		//	return offsetInBlocks;
		//}

		//private static SizeInt GetBlocksToCover(SizeDbl offset)
		//{
		//	var s = offset.GetSign();
		//	var c = offset.Abs().Ceiling();
		//	var b2c = c.Scale(s);

		//	var result = offset.Abs().Ceiling().Scale(offset.GetSign());

		//	return result;
		//}

		private static SizeInt GetOffsetAndRemainderV2(SizeInt offSetInSamplePoints, SizeInt blockSize, out SizeDbl canvasControlOffset)
		{
			var offset = offSetInSamplePoints.Divide(blockSize);
			var offsetInBlocks = offset.Abs().Ceiling().Scale(offset.GetSign());

			var rem = offset.Diff(offsetInBlocks);
			var remS = rem.Scale(blockSize);
			var remT = remS.Translate(blockSize);

			//var remM = remT.Mod(blockSize);
			//canvasControlOffset = remM.Scale(-1d);

			canvasControlOffset = remT.Mod(blockSize);

			Debug.WriteLine($"Starting Block Pos: {offsetInBlocks}, Pixel Pos: {canvasControlOffset}.");

			return offsetInBlocks;
		}

		private static SizeInt GetOffsetAndRemainderOld(SizeInt offSetInSamplePoints, SizeInt blockSize, out SizeDbl canvasControlOffset)
		{
			var blocksH = Math.DivRem(offSetInSamplePoints.Width, blockSize.Width, out var remainderH);
			var blocksV = Math.DivRem(offSetInSamplePoints.Height, blockSize.Height, out var remainderV);

			var wholeBlocks = offSetInSamplePoints.DivRem(blockSize, out var remainder);

			//var offSetRemainderInSamplePoints = new SizeDbl(remainderW, remainderH);
			//canvasControlOffset = GetSamplesRemaining(offSetRemainderInSamplePoints, blockSize);
			//Debug.WriteLine($"The remainder offset in sample points is {canvasControlOffset}. RawOffset: {offSetRemainderInSamplePoints}.");

			Debug.WriteLine($"Whole blocks: {wholeBlocks}, Remaining Pixels: {remainder}.");

			if (remainderH < 0)
			{
				blocksH--;
				remainderH = blockSize.Width + remainderH; // Want to display the last remainderH of the block, so we pull the display blkSize - remainderH to the left.
			}
			else if (remainderH > 0)
			{
				//blocksH++;
				//remainderH = blockSize.Width - remainderH; // Want to skip over the remainderH of the block, so we pull the display remainderH to the left.
			}

			if (remainderV < 0)
			{
				blocksV--;
				remainderV = blockSize.Height + remainderV; // Want to display the last remainderV of the block, so we pull the display blkSize - remainderH down.
			}
			else if (remainderV > 0)
			{
				//blocksV++;
				//remainderV = blockSize.Height - remainderV;  // Want to skip over the remainderV of the block, so we pull the display remainderV down.
			}

			var offSetInBlocks = new SizeInt(blocksH, blocksV);
			canvasControlOffset = new SizeDbl(remainderH, remainderV); //.Scale(-1d);

			Debug.WriteLine($"Starting Block Pos: {offSetInBlocks}, Pixel Pos: {canvasControlOffset}.");

			return offSetInBlocks;
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
			double result;

			if (extent < 0)
			{
				//result = -1 * (blockLen + extent);
				result = blockLen + extent;
			}
			else if (extent > 0)
			{
				//result = blockLen - extent;
				result = extent;
			}
			else
			{
				result = 0;
			}

			return result;
		}


		#endregion

		#region Not Used

		public static SizeInt GetCanvasSize(RSize mapSize, SizeInt canvasControlSize)
		{
			var wRatio = BigIntegerHelper.GetRatio(mapSize.WidthNumerator, canvasControlSize.Width);
			var hRatio = BigIntegerHelper.GetRatio(mapSize.HeightNumerator, canvasControlSize.Height);

			int w;
			int h;

			if (wRatio > hRatio)
			{
				// Width of image in pixels will take up the entire control.
				w = canvasControlSize.Width;

				// Height of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var hRatB = BigInteger.Divide(mapSize.HeightNumerator * 1000, mapSize.WidthNumerator * 1000);
				var hRat = BigIntegerHelper.ConvertToDouble(hRatB);
				h = (int)Math.Round(canvasControlSize.Width * hRat);
			}
			else
			{
				// Width of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var wRatB = BigInteger.Divide(mapSize.WidthNumerator * 1000, mapSize.HeightNumerator * 1000);
				var wRat = BigIntegerHelper.ConvertToDouble(wRatB);
				w = (int)Math.Round(canvasControlSize.Height * wRat);

				// Height of image in pixels will take up the entire control.
				h = canvasControlSize.Width;
			}

			var result = new SizeInt(w, h);

			return result;
		}

		//public static RSize GetSamplePointDelta(ref RRectangle coords, SizeInt newArea, RSize screenSizeToMapRat, SizeInt canvasSize)
		//{
		//	double expansionRatio;

		//	if (canvasSize.Width > canvasSize.Height)
		//	{
		//		var displayWidth = StepUpToNextPow(canvasSize.Width);
		//		expansionRatio = (double)displayWidth / canvasSize.Width;
		//	}
		//	else
		//	{
		//		var displayHeight = StepUpToNextPow(canvasSize.Height);
		//		expansionRatio = (double)displayHeight / canvasSize.Height;
		//	}

		//	// Increase the canvas size to the next largest power of. 2 (for example: 768 => 1024)
		//	var expandedCanvasSize = canvasSize.Scale(expansionRatio);

		//	// Increase the width and height of the of map coordinates in the same proportion.
		//	var expandedMapSize = coords.Size.Scale(new SizeDbl(expansionRatio, expansionRatio));

		//	var expandedArea = new SizeDbl(newArea.Width * expansionRatio, newArea.Height * expansionRatio);

		//	// Calculate a "nice" sample point size with these diminesions.
		//	var result = GetAdjustedSamplePointDelta(expandedMapSize, expandedArea, screenSizeToMapRat, expandedCanvasSize);

		//	// Use the original # of sample points and multiply by the new sample point size
		//	// to get a new map size.
		//	var adjMapSize = result.Scale(canvasSize);

		//	// Create an updated coord value with the new size.
		//	var newCoords = CombinePosAndSize(coords.Position, adjMapSize);
		//	Debug.WriteLine($"The new coords are : {newCoords},\n old = {coords}. (While calculating SamplePointDelta.)");
		//	coords = newCoords;

		//	return result;
		//}

		private static int StepUpToNextPow(int x)
		{
			var l = Math.Log2(x);
			var lc = Math.Ceiling(l);

			var result = (int)Math.Pow(2, lc);
			return result;
		}

		private static RSize GetAdjustedSamplePointDelta(RSize mapSize, SizeDbl screenSize, RSize screenSizeToMapRat, SizeInt canvasSize)
		{
			RValue extent;

			if (canvasSize.Width > canvasSize.Height)
			{
				//var sW = GetClosestPow(screenSize.Width);
				//var mW = sW * screenSizeToMapRat.Width;
				//newNumerator = BigIntegerHelper.Divide(mW, mapSize.Exponent, canvasSize.Width, out newExponent);

				extent = BigIntegerHelper.Divide(mapSize.Width, canvasSize.Width);
				Debug.WriteLine($"Adjusting SamplePointDelta. MapsW:{mapSize.Width}, ScreenW:{screenSize.Width}, csw: {canvasSize.Width}, SPD: {extent}.");
			}
			else
			{
				//var sH = GetClosestPow(screenSize.Height);
				//var mH = sH * screenSizeToMapRat.Height;
				//newNumerator = BigIntegerHelper.Divide(mH, mapSize.Exponent, canvasSize.Height, out newExponent);

				extent = BigIntegerHelper.Divide(mapSize.Height, canvasSize.Height);
				Debug.WriteLine($"Adjusting SamplePointDelta. MapsH:{mapSize.Height}, ScreenH:{screenSize.Height}, csh: {canvasSize.Height}, SPD: {extent}.");
			}

			var result = new RSize(extent);

			return result;
		}

		private static int GetClosestPow(double x)
		{
			var l = Math.Log2(x);
			var lr = Math.Round(l);

			var result = (int)Math.Pow(2, lr);
			return result;
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
				RNormalizer.NormalizeInPlace(ref destinationOrigin, ref normalizedOffset);
				var newOrigin = destinationOrigin.Translate(normalizedOffset);

				var newSize = coords.Size.Clone();
				RNormalizer.NormalizeInPlace(ref newOrigin, ref newSize);
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
			Debug.WriteLine($"JiggerTarget x:{targetCoords.Position.Values[0]}, y:{targetCoords.Position.Values[1]}, exp:{targetCoords.Exponent}; " +
				$"w:{targetCoords.Size.Values[0]}, h:{targetCoords.Size.Values[1]}.");

			//var cCoords = calcCoords.Clone();
			//var cSpd = samplePointDelta.Clone();
			//NormalizeInPlace(ref cCoords, ref cSpd);

			var cCoords = RNormalizer.Normalize(calcCoords, samplePointDelta, out var cSpd);

			for (var pCntr = -2; pCntr < 4; pCntr++)
			{
				var pv = new SizeInt(pCntr, pCntr);
				var p = cCoords.Position.Translate(cSpd.Scale(pv));

				for (var sCntr = -2; sCntr < 4; sCntr++)
				{
					var sv = new SizeInt(sCntr, sCntr);
					var s = cCoords.Size.Translate(cSpd.Scale(sv));
					RNormalizer.NormalizeInPlace(ref p, ref s);
					Debug.WriteLine($"{pCntr:D3},{sCntr:D3} :: x:{p.Values[0]}, y:{p.Values[1]}, exp:{p.Exponent}; w:{s.Values[0]}, h:{s.Values[1]}.");
				}
			}

			return calcCoords;
		}

		private static SizeDbl GetNumberOfSamplePointsDiag(RPoint mapOrigin, RPoint subdivisionOrigin, RSize samplePointDelta, out SizeDbl offset)
		{
			var sx = BigIntegerHelper.ConvertToDouble(mapOrigin.XNumerator, mapOrigin.Exponent);
			var sy = BigIntegerHelper.ConvertToDouble(mapOrigin.YNumerator, mapOrigin.Exponent);
			var dx = BigIntegerHelper.ConvertToDouble(subdivisionOrigin.XNumerator, subdivisionOrigin.Exponent);
			var dy = BigIntegerHelper.ConvertToDouble(subdivisionOrigin.YNumerator, subdivisionOrigin.Exponent);

			offset = new SizeDbl(sx - dx, sy - dy);

			var spdDC = GetSizeDbl(samplePointDelta);
			var numSamplesH = offset.Width / spdDC.Width;
			var numSamplesV = offset.Height / spdDC.Height;
			var offsetInSamplePoints = new SizeDbl(numSamplesH, numSamplesV);

			return offsetInSamplePoints;
		}

		private static SizeDbl GetSizeDbl(RSize rSize)
		{
			return new SizeDbl(BigIntegerHelper.ConvertToDouble(rSize.Width), BigIntegerHelper.ConvertToDouble(rSize.Height));
		}

		//private static PointDbl GetPointDbl(RPoint rPoint)
		//{
		//	return new PointDbl(BigIntegerHelper.ConvertToDouble(rPoint.X, rPoint.Exponent), BigIntegerHelper.ConvertToDouble(rPoint.Y, rPoint.Exponent));
		//}

		//private static void CheckGetMapBlockOffsetParams(RRectangle mapCoords, RPoint subdivisionOrigin)
		//{
		//	var ce = Math.Max(Math.Abs(mapCoords.Exponent), Math.Abs(subdivisionOrigin.Exponent));
		//	if (mapCoords.Exponent < 0)
		//	{
		//		ce *= -1;
		//	}

		//	var n = new BigInteger(Math.Pow(2, ce));
		//	var t = new RSize(n, n, ce);

		//	var tmCoords = RN.Normalize(mapCoords, t, out var _);
		//	var tsCoords = RN.Normalize(subdivisionOrigin, t, out var _);

		//	if (tmCoords.Exponent != tsCoords.Exponent)
		//	{
		//		throw new ArgumentException($"GetMapBlockOffset found that the map coordinates and the subdivision are on different scales.");
		//	}
		//}

		#endregion
	}
}
