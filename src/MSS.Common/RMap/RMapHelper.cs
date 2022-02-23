﻿using MSS.Types;
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
	}
}
