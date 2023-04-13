using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		#region Map Area Support

		public static RRectangle GetMapCoords(RectangleInt screenArea, RPoint mapPosition, RSize samplePointDelta)
		{
			//Debug.WriteLine($"GetMapCoords is receiving area: {screenArea}.");

			// Multiply the area by samplePointDelta to convert to map coordinates.
			var rArea = ScaleByRsize(screenArea, samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RNormalizer.Normalize(rArea, mapPosition, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			//Debug.WriteLine($"Calc Map Coords: Trans: {result}, Pos: {nrmPos}, Area: {nrmArea}, area rat: {GetAspectRatio(nrmArea)}, result rat: {GetAspectRatio(result)}");

			return result;
		}

		public static RRectangle GetMapCoords(SizeInt screenSize, RPoint mapPosition, RSize samplePointDelta)
		{
			//Debug.WriteLine($"GetMapCoords is receiving area: {screenArea}.");

			// Convert screen size to map size
			var mapSize = samplePointDelta.Scale(screenSize);

			// Translate the area by the current map position
			var nrmPos = RNormalizer.Normalize(mapPosition, mapSize, out var nrmSize);
			
			var result = new RRectangle(nrmPos, nrmSize);

			//Debug.WriteLine($"Calc Map Coords: Trans: {result}, Pos: {nrmPos}, Area: {nrmArea}, area rat: {GetAspectRatio(nrmArea)}, result rat: {GetAspectRatio(result)}");

			return result;
		}

		// The Pitch is the narrowest canvas dimension / the value having the closest power of 2 of the value given by the narrowest canvas dimension / 16.
		public static int CalculatePitch(SizeInt displaySize, int pitchTarget)
		{
			int result;

			var width = displaySize.Width;
			var height = displaySize.Height;

			if (double.IsNaN(width) || double.IsNaN(height) || width == 0 || height == 0)
			{
				return pitchTarget;
			}

			if (width >= height)
			{
				result = (int)Math.Round(width / Math.Pow(2, Math.Round(Math.Log2(width / pitchTarget))));
			}
			else
			{
				result = (int)Math.Round(height / Math.Pow(2, Math.Round(Math.Log2(height / pitchTarget))));
			}

			if (result < 0)
			{
				Debug.WriteLine($"WARNING: Calculating Pitch using Display Size: {displaySize} and Pitch Target: {pitchTarget}, produced {result}.");
				result = pitchTarget;
			}

			return result;
		}

		public static RRectangle GetNewCoordsForNewCanvasSize(RRectangle currentCoords, SizeInt currentSizeInBlocks, SizeInt newSizeInBlocks, Subdivision subdivision)
		{
			var diff = newSizeInBlocks.Sub(currentSizeInBlocks);

			if (diff == SizeInt.Zero)
			{
				return currentCoords;
			}

			diff = diff.Scale(subdivision.BlockSize);
			var rDiff = subdivision.SamplePointDelta.Scale(diff);
			rDiff = rDiff.DivideBy2();

			var result = AdjustCoords(currentCoords, rDiff);
			return result;
		}

		private static RRectangle AdjustCoords(RRectangle coords, RSize rDiff)
		{
			var nrmArea = RNormalizer.Normalize(coords, rDiff, out var nrmDiff);

			var x1 = nrmArea.X1 - nrmDiff.Width.Value;
			var x2 = nrmArea.X2 + nrmDiff.Width.Value;

			var y1 = nrmArea.Y1 - nrmDiff.Height.Value;
			var y2 = nrmArea.Y2 + nrmDiff.Height.Value;

			var result = new RRectangle(x1, x2, y1, y2, nrmArea.Exponent);

			return result;
		}

		#endregion

		#region Job Creation

		public static SizeInt GetCanvasSize(SizeInt newArea, SizeInt displaySize)
		{
			if (newArea.Width == 0 || newArea.Height == 0)
			{
				throw new ArgumentException("New area cannot have zero width or height upon call to GetCanvasSize.");
			}

			var wRatio = (double)newArea.Width / displaySize.Width;
			var hRatio = (double)newArea.Height / displaySize.Height;

			int w;
			int h;

			if (wRatio >= hRatio)
			{
				// Width of image in pixels will take up the entire control.
				w = displaySize.Width;

				// Height of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var hRat = (double)newArea.Height / newArea.Width;
				h = (int)Math.Round(displaySize.Width * hRat);
			}
			else
			{
				// Width of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var wRat = (double)newArea.Width / newArea.Height;
				w = (int)Math.Round(displaySize.Height * wRat);

				// Height of image in pixels will take up the entire control.
				h = displaySize.Height;
			}

			var result = new SizeInt(w, h);

			return result;
		}

		public static RSize GetSamplePointDelta(ref RRectangle coords, SizeInt canvasSize, double toleranceFactor)
		{
			var spdH = BigIntegerHelper.Divide(coords.Width, canvasSize.Width, toleranceFactor);
			var spdV = BigIntegerHelper.Divide(coords.Height, canvasSize.Height, toleranceFactor);

			var nH = RNormalizer.Normalize(spdH, spdV, out var nV);

			// Take the smallest value
			var result = new RSize(RValue.Min(nH, nV));

			// The size of the new map is equal to the product of the number of samples by the new samplePointDelta.
			var adjMapSize = result.Scale(canvasSize);

			// Calculate the new map coordinates using the existing position and the new size.
			var newCoords = CombinePosAndSize(coords.Position, adjMapSize);

			coords = newCoords;
			return result;
		}

		public static RSize GetSamplePointDelta2(ref RRectangle coords, SizeInt canvasSize, double toleranceFactor)
		{
			var spdH = BigIntegerHelper.Divide(coords.Width, canvasSize.Width, toleranceFactor);
			var spdV = BigIntegerHelper.Divide(coords.Height, canvasSize.Height, toleranceFactor);

			var nH = RNormalizer.Normalize(spdH, spdV, out var nV);

			// Take the smallest value
			var rawSamplePointDelta = new RSize(RValue.Min(nH, nV));

			// The size of the new map is equal to the product of the number of samples by the new samplePointDelta.
			//var adjMapSize = rawSamplePointDelta.Scale(canvasSize);

			// Calculate the new map coordinates using the existing position and the new size.
			//var newCoords = CombinePosAndSize(coords.Position, adjMapSize);

			//var nrmPos = RNormalizer.Normalize(coords.Position, adjMapSize, out var nrmSize);
			//var newCoords = new RRectangle(nrmPos, nrmSize);


			// Update the MapPosition and the calculated SamplePointDelta have the same exponent.
			var nrmPos = RNormalizer.Normalize(coords.Position, rawSamplePointDelta, out var nrmSamplePointDelta);

			var adjMapSize = nrmSamplePointDelta.Scale(canvasSize);

			var newCoords = new RRectangle(nrmPos, adjMapSize);

			coords = newCoords;
			return nrmSamplePointDelta;
		}

		#endregion

		#region SamplePointDelta Diagnostic Support

		public static SizeDbl GetSamplePointDiag(RRectangle coords, SizeInt canvasSize, out RectangleDbl newCoords)
		{
			var rectangleDbl = ConvertToRectangleDbl(coords);

			var spdH = rectangleDbl.Width / canvasSize.Width;
			var spdV = rectangleDbl.Height / canvasSize.Height;

			var result = new SizeDbl(Math.Min(spdH, spdV));
			var adjMapSize = result.Scale(canvasSize);

			newCoords = new RectangleDbl(rectangleDbl.Position, adjMapSize);

			return result;
		}

		public static void ReportSamplePointDiff(RSize spd, SizeDbl spdD, RRectangle origCoords, RRectangle coords, RectangleDbl coordsD)
		{
			var origCoordsD = ConvertToRectangleDbl(origCoords);

			var realCoordsD = ConvertToRectangleDbl(coords);
			var coordsDiff = realCoordsD.Diff(coordsD).Abs();

			var realSpdD = ConvertToSizeDbl(spd);
			var spdDiff = realSpdD.Diff(spdD).Abs();

			Debug.WriteLine($"\nThe new coords are : {coords}, old = {origCoords}. Using SamplePointDelta: {spd}\n");

			if (coordsDiff.Width > 0 || coordsDiff.Height > 0)
			{
				var perWDiff = 100 * coordsDiff.Width / realCoordsD.Width;
				var perHDiff = 100 * coordsDiff.Height / realCoordsD.Height;
				Debug.WriteLine($"Compare to double math: Coords Size differs by w:{perWDiff}, h:{perHDiff} percentage.");
			}

			Debug.WriteLine($"\nThe new coords are: {realCoordsD},\n old = {origCoordsD}, Compare: {coordsD}. Diff: {coordsDiff}, Exp: {coords.Exponent}");
			Debug.WriteLine($"\nThe new SamplePointDelta is: {realSpdD}, Compare: {spdD}. Diff: {spdDiff}, Exp: {spd.Exponent}.");

			Debug.WriteLine($"\nCoords Precision: {BigIntegerHelper.GetPrecision(origCoords)} Spd Precision: {BigIntegerHelper.GetPrecision(spd)}.");
		}

		#endregion

		#region Job Block Support

		public static SizeInt GetCanvasSizeInWholeBlocks(SizeDbl canvasSize, SizeInt blockSize, bool keepSquare)
		{
			var result = canvasSize.Divide(blockSize).Truncate();

			if (keepSquare)
			{
				result = result.GetSquare();
			}

			return result;
		}

		public static SizeInt GetMapExtentInBlocks(SizeInt canvasSize, SizeInt blockSize)
		{
			var rawResult = canvasSize.DivRem(blockSize, out var remainder);
			var extra = new VectorInt(remainder.Width > 0 ? 1 : 0, remainder.Height > 0 ? 1 : 0);
			var result = rawResult.Add(extra);

			return result;
		}

		public static SizeInt GetMapExtentInBlocks(SizeInt canvasSize, VectorInt canvasControlOffset, SizeInt blockSize)
		{
			Debug.Assert(canvasControlOffset.X >= 0 && canvasControlOffset.Y >= 0, "Using a canvasControlOffset with a negative w or h when getting the MapExtent in blocks.");


			var totalSize = canvasSize.Add(canvasControlOffset);

			var rawResult = totalSize.DivRem(blockSize, out var remainder);
			var extra = new VectorInt(remainder.Width > 0 ? 1 : 0, remainder.Height > 0 ? 1 : 0);
			var result = rawResult.Add(extra);

			return result;
		}

		//public static SizeInt GetMapExtentInBlocks(SizeDbl canvasSize, VectorInt canvasControlOffset, SizeInt blockSize)
		//{
		//	var sizeCorrection = blockSize.Sub(canvasControlOffset).Mod(blockSize);
		//	var totalSize = canvasSize.Inflate(sizeCorrection);
		//	var rawResult = totalSize.DivRem(blockSize, out var remainder);
		//	var extra = new SizeInt(remainder.Width > 0 ? 1 : 0, remainder.Height > 0 ? 1 : 0);
		//	var result = rawResult.Inflate(extra);

		//	return result;
		//}

		// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
		// The screen origin in the left, bottom corner and the left, bottom corner of the map is displayed here.
		public static BigVector GetMapBlockOffset(ref RRectangle mapCoords, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			var mapOrigin = mapCoords.Position;
			var distance = new RVector(mapOrigin);
			//Debug.WriteLine($"Our origin is {mapCoords.Position}, repo origin is {destinationOrigin}, for a distance of {distance}.");

			BigVector result;
			if (distance == RVector.Zero)
			{
				canvasControlOffset = new VectorInt();
				result = new BigVector();
			}
			else
			{
				var offsetInSamplePoints = GetNumberOfSamplePoints(distance, samplePointDelta, out var newDistance);
				//Debug.WriteLine($"The offset in samplePoints is {offsetInSamplePoints}.");

				var newMapOrigin = new RPoint(newDistance);
				mapCoords = CombinePosAndSize(newMapOrigin, mapCoords.Size);

				result = GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
				//Debug.WriteLine($"Starting Block Pos: {result}, Pixel Pos: {canvasControlOffset}.");
			}

			return result;
		}

		// Calculate the number of samplePoints in the given offset.
		private static BigVector GetNumberOfSamplePoints(RVector distance, RSize samplePointDelta, out RVector newDistance)
		{
			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			var offsetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			var rOffset = new RVector(offsetInSamplePoints);
			newDistance = rOffset.Scale(nrmSamplePointDelta);
			newDistance = Reducer.Reduce(newDistance);

			return offsetInSamplePoints;
		}

		private static BigVector GetOffsetAndRemainder(BigVector offsetInSamplePoints, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			var blocksH = BigInteger.DivRem(offsetInSamplePoints.X, blockSize.Width, out var remainderH);
			var blocksV = BigInteger.DivRem(offsetInSamplePoints.Y, blockSize.Height, out var remainderV);

			//var wholeBlocks = offsetInSamplePoints.DivRem(blockSize, out var remainder);
			//Debug.WriteLine($"Whole blocks: {wholeBlocks}, Remaining Pixels: {remainder}.");

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

			var offsetInBlocks = new BigVector(blocksH, blocksV);
			canvasControlOffset = new VectorInt(remainderH, remainderV);

			return offsetInBlocks;
		}

		#endregion

		#region Screen To Subdivision Translation

		public static BigVector ToSubdivisionCoords(PointInt screenPosition, BigVector mapBlockOffset, out bool isInverted)
		{
			var repoPos = mapBlockOffset.Tranlate(screenPosition);

			BigVector result;
			if (repoPos.Y < 0)
			{
				isInverted = true;
				result = new BigVector(repoPos.X, (repoPos.Y * -1) - 1);
			}
			else
			{
				isInverted = false;
				result = repoPos;
			}

			return result;
		}

		public static PointInt ToScreenCoords(BigVector repoPosition, bool inverted, BigVector mapBlockOffset)
		{
			BigVector posT;

			if (inverted)
			{
				posT = new BigVector(repoPosition.X, (repoPosition.Y + 1) * -1);
			}
			else
			{
				posT = repoPosition;
			}

			var screenOffsetRat = posT.Diff(mapBlockOffset);

			if (BigIntegerHelper.TryConvertToInt(screenOffsetRat.Values, out var values))
			{
				var result = new PointInt(values);
				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot convert the ScreenCoords to integers.");
			}
		}

		#endregion

		#region Type Helpers

		/// <summary>
		/// Creates a new RRectangle with the new coordinate value, all other values remain the same.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="index">0 = X1; 1 = X2; 2 = Y1; 3 = Y2</param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static RRectangle UpdatePointValue(RRectangle source, int index, RValue value)
		{
			var nrmRect = RNormalizer.Normalize(source.Clone(), value, out var nrmValue);
			nrmRect.Values[index] = nrmValue.Value;
			return nrmRect;
		}

		private static RRectangle CombinePosAndSize(RPoint pos, RSize size)
		{
			var nrmPos = RNormalizer.Normalize(pos, size, out var nrmSize);
			var result = new RRectangle(nrmPos, nrmSize);

			return result;
		}

		private static RRectangle ScaleByRsize(RectangleInt area, RSize factor)
		{
			var rectangle = new RRectangle(area);
			var result = rectangle.Scale(factor);

			return result;
		}

		private static RectangleDbl ConvertToRectangleDbl(RRectangle rRectangle)
		{
			try
			{
				return new RectangleDbl(
					BigIntegerHelper.ConvertToDouble(rRectangle.Left),
					BigIntegerHelper.ConvertToDouble(rRectangle.Right),
					BigIntegerHelper.ConvertToDouble(rRectangle.Bottom),
					BigIntegerHelper.ConvertToDouble(rRectangle.Top)
					);
			}
			catch
			{
				return new RectangleDbl(double.NaN, double.NaN, double.NaN, double.NaN);
			}
		}

		private static SizeDbl ConvertToSizeDbl(RSize rSize)
		{
			try
			{
				return new SizeDbl(
					BigIntegerHelper.ConvertToDouble(rSize.Width),
					BigIntegerHelper.ConvertToDouble(rSize.Height)
					);
			}
			catch
			{
				return new SizeDbl(double.NaN, double.NaN);
			}
		}

		public static double GetAspectRatio(RRectangle rRectangle)
		{
			if (BigIntegerHelper.TryConvertToDouble(rRectangle.WidthNumerator, out var w) && BigIntegerHelper.TryConvertToDouble(rRectangle.HeightNumerator, out var h))
			{
				var result = w / h;
				return result;
			}
			else
			{
				return double.NaN;
			}
		}

		public static SizeDbl GetBoundingSize(RectangleDbl a, RectangleDbl b)
		{
			var boundingRectangle = GetBoundingRectangle(a, b);
			var boundingSize = GetBoundingSize(boundingRectangle);

			return boundingSize;
		}

		public static RectangleDbl GetBoundingRectangle(RectangleDbl a, RectangleDbl b)
		{
			var p1 = a.Point1.Min(b.Point1);
			var p2 = a.Point2.Max(b.Point2);
			var result = new RectangleDbl(p1, p2);

			return result;
		}

		public static SizeDbl GetBoundingSize(RectangleDbl a)
		{
			var distance = a.Position.Abs();
			var result = a.Size.Inflate(new VectorDbl(distance));

			return result;
		}


		//public static double GetSmallestScaleFactor(RectangleDbl a, RectangleDbl b)
		//{
		//	var diff = a.Position.Diff(b.Position);
		//	var distance = diff.Abs();
		//	var aSizePlusTranslated = a.Size.Inflate(distance);

		//	var result = GetSmallestScaleFactor(aSizePlusTranslated, b.Size);

		//	return result;
		//}

		public static double GetSmallestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		{
			var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
			var hRat = containerSize.Height / sizeToFit.Height;

			var result = Math.Min(wRat, hRat);

			return result;
		}

		//public static double GetLargestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		//{
		//	var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
		//	var hRat = containerSize.Height / sizeToFit.Height;

		//	var result = Math.Max(wRat, hRat);

		//	return result;
		//}


		#endregion
	}
}
