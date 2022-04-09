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
			//Debug.WriteLine($"GetMapCoords is receiving area: {area}.");

			// Multiply the area by samplePointDelta to convert to map coordinates.
			var rArea = ScaleByRsize(area, samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RNormalizer.Normalize(rArea, position, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			Debug.WriteLine($"Calc Map Coords: Trans: {result}, Pos: {nrmPos}, Area: {nrmArea}, area rat: {GetAspectRatio(nrmArea)}, result rat: {GetAspectRatio(result)}");

			return result;
		}

		private static double GetAspectRatio(RRectangle rRectangle)
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

		private static RRectangle ScaleByRsize(RectangleInt area, RSize factor)
		{
			var rectangle = new RRectangle(area);
			var result = rectangle.Scale(factor);

			return result;
		}

		// The Pitch is the narrowest canvas dimension / the value having the closest power of 2 of the value given by the narrowest canvas dimension / 16.
		public static int CalculatePitch(SizeDbl displaySize, int pitchTarget)
		{
			int result;

			var width = displaySize.Width;
			var height = displaySize.Height;

			if (width >= height)
			{
				result = (int)Math.Round(width / Math.Pow(2, Math.Round(Math.Log2(width / pitchTarget))));
			}
			else
			{
				result = (int)Math.Round(height / Math.Pow(2, Math.Round(Math.Log2(height / pitchTarget))));
			}

			Debug.WriteLine($"The new ScreenSelection Pitch is {result}.");
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
			//Debug.WriteLine($"\nThe new coords are : {newCoords},\n old = {coords}. (While calculating SamplePointDelta3.)\n");

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

		public static SizeInt GetCanvasSizeInWholeBlocks(SizeDbl canvasSize, SizeInt blockSize, bool keepSquare)
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
				canvasSizeInBlocks.Width + (Math.Abs(canvasControlOffset.X) > 0 ? 2 : 0),
				canvasSizeInBlocks.Height + (Math.Abs(canvasControlOffset.Y) > 0 ? 2 : 0)
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
			//Debug.WriteLine($"Our origin is {mapCoords.Position}, repo origin is {destinationOrigin}, for a distance of {distance}.");

			BigVector result;
			if (distance.X == 0 && distance.Y == 0)
			{
				canvasControlOffset = new VectorInt();
				result = new BigVector();
			}
			else
			{
				var offsetInSamplePoints = GetNumberOfSamplePoints(distance, samplePointDelta);
				//Debug.WriteLine($"The offset in samplePoints is {offsetInSamplePoints}.");

				result = GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
				//Debug.WriteLine($"Starting Block Pos: {result}, Pixel Pos: {canvasControlOffset}.");
			}

			return result;
		}

		// Calculate the number of samplePoints in the given offset.
		private static BigVector GetNumberOfSamplePoints(RVector distance, RSize samplePointDelta)
		{
			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			var offsetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			return new BigVector(offsetInSamplePoints);
		}

		private static BigVector GetOffsetAndRemainder(BigVector offsetInSamplePoints, SizeInt blockSize, out VectorInt canvasControlOffset)
		{
			var blocksH = BigInteger.DivRem(offsetInSamplePoints.X, blockSize.Width, out var remainderH);
			var blocksV = BigInteger.DivRem(offsetInSamplePoints.Y, blockSize.Height, out var remainderV);

			var wholeBlocks = offsetInSamplePoints.DivRem(blockSize, out var remainder);
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
				posT = new BigVector(repoPosition.XNumerator, (repoPosition.YNumerator + 1) * -1);
			}
			else
			{
				posT = repoPosition;
			}

			var screenOffsetRat = posT.Diff(mapBlockOffset);
			var reducedOffset = Reducer.Reduce(screenOffsetRat);

			if (BigIntegerHelper.TryConvertToInt(reducedOffset, out var values))
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
	}
}
