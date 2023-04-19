using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	public static class RMapHelper
	{
		#region Map Area Support

		// Convert the screen coordinates given by screenArea into map coordinates,
		// then move these map coordiates by the x and y distances specified in the current MapPosition.
		public static RRectangle GetMapCoords(RectangleInt screenArea, RPoint mapPosition, RSize samplePointDelta)
		{
			// Convert to map coordinates.
			var rArea = ScaleByRsize(screenArea, samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RNormalizer.Normalize(rArea, mapPosition, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			//Debug.WriteLine($"GetMapCoords is receiving area: {screenArea}.");
			//Debug.WriteLine($"Calc Map Coords: Trans: {result}, Pos: {nrmPos}, Area: {nrmArea}, area rat: {GetAspectRatio(nrmArea)}, result rat: {GetAspectRatio(result)}");

			return result;
		}

		public static RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSize, double toleranceFactor, out double wToHRatio)
		{
			var spdH = BigIntegerHelper.Divide(coords.Width, canvasSize.Width, toleranceFactor);
			var spdV = BigIntegerHelper.Divide(coords.Height, canvasSize.Height, toleranceFactor);

			var nH = RNormalizer.Normalize(spdH, spdV, out var nV);

			// Take the smallest value
			var result = new RSize(RValue.Min(nH, nV));

			wToHRatio = nH.DivideLimitedPrecision(nV);

			return result;
		}

		public static RRectangle AdjustCoordsWithNewSPD(RRectangle coords, RSize samplePointDelta, SizeInt canvasSize)
		{
			// The size of the new map is equal to the product of the number of samples by the new samplePointDelta.
			var adjMapSize = samplePointDelta.Scale(canvasSize);

			// Calculate the new map coordinates using the existing position and the new size.
			var newCoords = CombinePosAndSize(coords.Position, adjMapSize);

			return newCoords;
		}

		public static int CalculatePitch(SizeInt displaySize, int pitchTarget)
		{
			// The Pitch is the narrowest canvas dimension / the value having the closest power of 2 of the value given by the narrowest canvas dimension / 16.
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

		#endregion

		#region Get Extents in Blocks

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
			var result = GetMapExtentInBlocks(totalSize, blockSize);

			return result;
		}

		#endregion

		#region Get MapBlockOffset Methods - New

		public static BigVector GetMapBlockOffset(RPoint mapPosition, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset/*, out RPoint newPosition*/)
		{
			// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
			// The screen origin = left, bottom. Map origin = left, bottom.

			if (mapPosition.IsZero())
			{
				canvasControlOffset = new VectorInt();
				return new BigVector();
			}

			var distance = new RVector(mapPosition);
			var offsetInSamplePoints = GetNumberOfSamplePoints(distance, samplePointDelta/*, out newPosition*/);

			var result = GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);

			return result;
		}

		private static BigVector GetNumberOfSamplePoints(RVector distance, RSize samplePointDelta/*, out RPoint newPosition*/)
		{
			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			var offsetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			return offsetInSamplePoints;
		}

		#endregion

		#region Get MapBLockOffset Methods Previous / Ref

		public static BigVector GetMapBlockOffsetRef(RRectangle mapCoords, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset, out RPoint newPosition)
		{
			// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
			// The screen origin = left, bottom. Map origin = left, bottom.

			var mapOrigin = mapCoords.Position;
			var distance = new RVector(mapOrigin);
			//Debug.WriteLine($"Our origin is {mapCoords.Position}, repo origin is {destinationOrigin}, for a distance of {distance}.");

			BigVector result;
			if (distance == RVector.Zero)
			{
				newPosition = mapOrigin;
				canvasControlOffset = new VectorInt();
				result = new BigVector();
			}
			else
			{
				var offsetInSamplePoints = GetNumberOfSamplePointsRef(distance, samplePointDelta, out newPosition);
				//Debug.WriteLine($"The offset in samplePoints is {offsetInSamplePoints}.");

				//var newMapOrigin = new RPoint(newDistance);
				//mapCoords = CombinePosAndSize(newMapOrigin, mapCoords.Size);

				result = GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
				//Debug.WriteLine($"Starting Block Pos: {result}, Pixel Pos: {canvasControlOffset}.");
			}

			Debug.Assert(canvasControlOffset.X >= 0 && canvasControlOffset.Y >= 0, "GetMapBlockOffset is returning a canvasControlOffset with a negative w or h value.");

			return result;
		}

		private static BigVector GetNumberOfSamplePointsRef(RVector distance, RSize samplePointDelta, out RPoint newPosition)
		{
			// Calculate the number of samplePoints in the given offset.

			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			var offsetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			// Multiply the result by samplePointDelta to get the 'adjusted distance'
			var newDistance = ScaleByRsize(offsetInSamplePoints, nrmSamplePointDelta);
			newPosition = new RPoint(Reducer.Reduce(newDistance));

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

			//if (BigIntegerHelper.TryConvertToInt(screenOffsetRat.Values, out var values))
			//{
			//	var result = new PointInt(values);
			//	return result;
			//}
			//else
			//{
			//	throw new InvalidOperationException($"Cannot convert the ScreenCoords to integers.");
			//}

			if (screenOffsetRat.TryConvertToInt(out var result))
			{
				return new PointInt(result);
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

		public static RRectangle CombinePosAndSize(RPoint pos, RSize size)
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

		private static RVector ScaleByRsize(BigVector extent, RSize factor)
		{
			var rExtent = new RVector(extent);
			var result = rExtent.Scale(factor);

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

		public static double GetSmallestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		{
			var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
			var hRat = containerSize.Height / sizeToFit.Height;

			var result = Math.Min(wRat, hRat);

			return result;
		}


		public static MapAreaInfo Convert(MapAreaInfo2 mapAreaInfo2)
		{
			return MapAreaInfo.Empty;
		}

		public static MapAreaInfo2 Convert(MapAreaInfo mapAreaInfo)
		{
			var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
			var blockSize = mapAreaInfo.Subdivision.BlockSize;

			var nrmCoords = NormalizeCoordsWithSPD(mapAreaInfo.Coords, mapAreaInfo.Subdivision.SamplePointDelta);

			var offset = new RSize(nrmCoords.WidthNumerator / 2, nrmCoords.HeightNumerator / 2, nrmCoords.Exponent);

			var center = nrmCoords.Position.Translate(offset);

			var mapBlockOffset = GetMapBlockOffset(center, samplePointDelta, blockSize, out var canvasControlOffset);

			var result = new MapAreaInfo2(center, mapAreaInfo.Subdivision, mapBlockOffset, mapAreaInfo.Precision, canvasControlOffset);

			return result;
		}


		public static RRectangle NormalizeCoordsWithSPD(RRectangle coords, RSize samplePointDelta)
		{
			if (coords.Exponent == samplePointDelta.Exponent)
			{
				return coords;
			}

			var rCoords = coords;
			if (coords.Exponent > samplePointDelta.Exponent)
			{
				rCoords = Reducer.Reduce(coords);
				if (rCoords.Exponent > samplePointDelta.Exponent)
				{ 
					throw new InvalidOperationException("Cannot Normalize coords having an exponent > the exponent of the SamplePointDelta.");
				}
			}

			var factor = (long)Math.Pow(2, samplePointDelta.Exponent - rCoords.Exponent);

			var newVals = rCoords.Values.Select(v => v * factor).ToArray();

			var result = new RRectangle(newVals, samplePointDelta.Exponent);

			return result;
		}

		#endregion

		#region Old JobCreation and Map Area Support

		/// <summary>
		/// Same as new, except no translation, only resizing
		/// </summary>
		/// <param name="screenSize"></param>
		/// <param name="mapPosition"></param>
		/// <param name="samplePointDelta"></param>
		/// <returns></returns>
		public static RRectangle GetMapCoordsOld(SizeInt screenSize, RPoint mapPosition, RSize samplePointDelta)
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

		public static SizeInt GetMapExtentInBlocks(SizeDbl canvasSize, VectorInt canvasControlOffset, SizeInt blockSize)
		{
			var sizeCorrection = blockSize.Sub(canvasControlOffset).Mod(blockSize);
			var totalSize = canvasSize.Inflate(sizeCorrection);
			var rawResult = totalSize.DivRem(blockSize, out var remainder);
			var extra = new SizeInt(remainder.Width > 0 ? 1 : 0, remainder.Height > 0 ? 1 : 0);
			var result = rawResult.Inflate(extra);

			return result;
		}

		public static double GetSmallestScaleFactor(RectangleDbl a, RectangleDbl b)
		{
			var diff = a.Position.Diff(b.Position);
			var distance = diff.Abs();
			var aSizePlusTranslated = a.Size.Inflate(distance);

			var result = GetSmallestScaleFactor(aSizePlusTranslated, b.Size);

			return result;
		}

		public static double GetLargestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		{
			var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
			var hRat = containerSize.Height / sizeToFit.Height;

			var result = Math.Max(wRat, hRat);

			return result;
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
	}
}
