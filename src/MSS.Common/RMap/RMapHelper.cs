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
		#region MapAreaInfo Support

		public static RPointAndDelta GetNewCenterPoint(RPointAndDelta rPointAndDelta, VectorInt panAmount)
		{
			var rPanAmount = panAmount.Scale(rPointAndDelta.SamplePointDelta);
			var newMapCenter = rPointAndDelta.Position.Translate(rPanAmount);

			var transPd = new RPointAndDelta(newMapCenter, rPointAndDelta.SamplePointDelta);

			return transPd;
		}

		public static RPointAndDelta GetNewSamplePointDelta(RPointAndDelta pointAndDelta, double factor)
		{
			// Factor = number of new pixels each existing pixel will be replaced with.

			// Divide the SamplePointDelta by the specified factor.
			// Instead of dividing, multiply by the reciprocal.

			var reciprocal = 1 / factor;

			// Create an RValue that has the same value of the reciprocal.

			// Numerator: reciprocal * 1024
			// Denominator: 1024

			var rK = (int)Math.Round(reciprocal * 1024);
			var rReciprocal = new RValue(rK, -10);

			// Multiply the SamplePointDelta by 1/factor, adjusting the exponent as necessary.
			// as the exponent is futher decreased, the numerators of the X and Y values are increased to compensate.
			var rawResult = pointAndDelta.ScaleDelta(rReciprocal);

			// Divide all numerators by the greatest power 2 that all three numerators (X, Y and scale) have in common,
			// and reduce the denominator to compensate.
			var result = Reducer.Reduce(rawResult);

			return result;
		}

		public static int GetBinaryPrecision(RRectangle coords, RSize samplePointDelta, out int decimalPrecision)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(coords.Right, coords.Left, out decimalPrecision);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			return binaryPrecision;
		}

		public static int GetBinaryPrecision(MapAreaInfo mapAreaInfo)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(mapAreaInfo.Coords.Right, mapAreaInfo.Coords.Left, out _);

			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(mapAreaInfo.Subdivision.SamplePointDelta.Exponent));

			return binaryPrecision;
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

		#region Get MapBlockOffset Methods

		public static BigVector GetMapBlockOffset(RPointAndDelta rPointAndDelta, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
			// The screen origin = left, bottom. Map origin = left, bottom.

			if (rPointAndDelta.Position.IsZero())
			{
				canvasControlOffset = new VectorInt();
				return new BigVector();
			}

			var offsetInSamplePoints = rPointAndDelta.Position.Divide(rPointAndDelta.SamplePointDelta);

			var result = GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);

			return result;
		}

		public static BigVector GetOffsetAndRemainder(BigVector offsetInSamplePoints, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			var blocksX = BigInteger.DivRem(offsetInSamplePoints.X, blockSize.Width, out var remainderX);
			var blocksY = BigInteger.DivRem(offsetInSamplePoints.Y, blockSize.Height, out var remainderY);

			//var wholeBlocks = offsetInSamplePoints.DivRem(blockSize, out var remainder);
			//Debug.WriteLine($"Whole blocks: {wholeBlocks}, Remaining Pixels: {remainder}.");

			if (remainderX < 0)
			{
				blocksX--;
				remainderX += blockSize.Width; // Want to display the last remainderX of the block, so we pull the display blkSize - remainderX to the left.
			}

			if (remainderY < 0)
			{
				blocksY--;
				remainderY += blockSize.Height; // Want to display the last remainderY of the block, so we pull the display blkSize - remainderY down.
			}

			var offsetInBlocks = new BigVector(blocksX, blocksY);
			canvasControlOffset = new VectorInt(remainderX, remainderY);

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


		//public static double CalculatePitch(SizeDbl displaySize, int pitchTarget)
		//{
		//	// The Pitch is the narrowest canvas dimension / the value having the closest power of 2 of the value given by the narrowest canvas dimension / 16.
		//	double result;

		//	var width = displaySize.Width;
		//	var height = displaySize.Height;

		//	if (double.IsNaN(width) || double.IsNaN(height) || width == 0 || height == 0)
		//	{
		//		return pitchTarget;
		//	}

		//	if (width >= height)
		//	{
		//		result = width / Math.Pow(2, Math.Log2(width / pitchTarget));
		//	}
		//	else
		//	{
		//		result = height / Math.Pow(2, Math.Log2(height / pitchTarget));
		//	}

		//	if (result < 0)
		//	{
		//		Debug.WriteLine($"WARNING: Calculating Pitch using Display Size: {displaySize} and Pitch Target: {pitchTarget}, produced {result}.");
		//		result = pitchTarget;
		//	}

		//	return result;
		//}


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

		public static double GetSmallestScaleFactor(RectangleDbl a, RectangleDbl b)
		{
			var diff = a.Position.Diff(b.Position);
			var distance = diff.Abs();
			var aSizePlusTranslated = a.Size.Inflate(distance);

			var result = GetSmallestScaleFactor(aSizePlusTranslated, b.Size);

			return result;
		}

		public static double GetSmallestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		{
			var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
			var hRat = containerSize.Height / sizeToFit.Height;

			var result = Math.Min(wRat, hRat);

			return result;
		}

		public static double GetLargestScaleFactor(SizeDbl sizeToFit, SizeDbl containerSize)
		{
			var wRat = containerSize.Width / sizeToFit.Width; // Scale Factor to multiply item being fitted to get container units.
			var hRat = containerSize.Height / sizeToFit.Height;

			var result = Math.Max(wRat, hRat);

			return result;
		}

		public static SizeInt GetCanvasSize(SizeDbl newArea, SizeInt displaySize)
		{
			if (newArea.Width == 0 || newArea.Height == 0)
			{
				throw new ArgumentException("New area cannot have zero width or height upon call to GetCanvasSize.");
			}

			var wRatio = newArea.Width / displaySize.Width;
			var hRatio = newArea.Height / displaySize.Height;

			int w;
			int h;

			if (wRatio >= hRatio)
			{
				// Width of image in pixels will take up the entire control.
				w = displaySize.Width;

				// Height of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var hRat = newArea.Height / newArea.Width;
				h = (int)Math.Round(displaySize.Width * hRat);
			}
			else
			{
				// Width of image in pixels will be somewhat less, in proportion to the ratio of the width and height of the coordinates.
				var wRat = newArea.Width / newArea.Height;
				w = (int)Math.Round(displaySize.Height * wRat);

				// Height of image in pixels will take up the entire control.
				h = displaySize.Height;
			}

			var result = new SizeInt(w, h);

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
