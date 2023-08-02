using MSS.Common;
using PngImageLib;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImageBuilder
{
	internal static class BitmapHelper
	{
		private const double VALUE_FACTOR = 10000;

		//public static int GetNumberOfLines(int blockPtrY, int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY, out int linesToSkip)
		//{
		//	int numberOfLines;

		//	if (blockPtrY == 0)
		//	{
		//		// This is the block with the largest y coordinate (aka the last block)
		//		linesToSkip = 0;
		//		var numberOfLinesForFirstBlock = GetNumberOfLinesForFirstBlock(imageHeight, numberOfWholeBlocksY, blockHeight, canvasControlOffsetY);
		//		var numberOfLinesSoFar = numberOfLinesForFirstBlock + (blockHeight * (numberOfWholeBlocksY - 2));
		//		numberOfLines = imageHeight - numberOfLinesSoFar;
		//	}
		//	else if (blockPtrY == numberOfWholeBlocksY - 1)
		//	{
		//		// This is the block with the smallest y coordinate (aka the first block)
		//		numberOfLines = GetNumberOfLinesForFirstBlock(imageHeight, numberOfWholeBlocksY, blockHeight, canvasControlOffsetY);
		//		linesToSkip = blockHeight - numberOfLines; // (Since the pixel lines are accessed from high to low index, this is measured from index = blockHeight - 1)
		//	}
		//	else
		//	{
		//		linesToSkip = 0;
		//		numberOfLines = blockHeight;
		//	}

		//	return numberOfLines;

		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static int GetNumberOfLinesForFirstBlock(int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY)
		//{
		//	return canvasControlOffsetY + imageHeight - (blockHeight * (numberOfWholeBlocksY - 1));
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static int GetSegmentLength(int blockPtrX, int imageWidth, int numberOfWholeBlocksX, int blockWidth, int canvasControlOffsetX, out int samplesToSkip)
		//{
		//	// TODO: Build an array of Segment lengths, once and then re-use for each row.
		//	int result;

		//	if (blockPtrX == 0)
		//	{
		//		// TODO: Why does this work for the x-axis, but not the y-axis.
		//		samplesToSkip = canvasControlOffsetX;
		//		result = blockWidth - canvasControlOffsetX;
		//	}
		//	else if (blockPtrX == numberOfWholeBlocksX - 1)
		//	{
		//		samplesToSkip = 0;
		//		result = canvasControlOffsetX + imageWidth - (blockWidth * (numberOfWholeBlocksX - 1));
		//	}
		//	else
		//	{
		//		samplesToSkip = 0;
		//		result = blockWidth;
		//	}

		//	return result;
		//}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort[]? GetOneLineFromCountsBlock(ushort[]? counts, int lPtr, int stride)
		{
			if (counts == null)
			{
				return null;
			}
			else
			{
				var result = new ushort[stride];

				Array.Copy(counts, lPtr * stride, result, 0, stride);
				return result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void FillImageLineSegment(byte[] imageData, int pixPtr, ushort[]? counts, ushort[]? escapeVelocities, int lineLength, int samplesToSkip, ColorMap colorMap)
		{
			if (counts == null || escapeVelocities == null)
			{
				FillImageLineSegmentWithWhite(imageData, pixPtr, lineLength);
				return;
			}

			var previousCountVal = counts[0];

			for (var xPtr = 0; xPtr < lineLength; xPtr++)
			{
				var countVal = counts[xPtr + samplesToSkip];

				if (countVal != previousCountVal)
				{
					//NumberOfCountValSwitches++;
					previousCountVal = countVal;
				}

				var escapeVelocity = colorMap.UseEscapeVelocities ? escapeVelocities[xPtr + samplesToSkip] / VALUE_FACTOR : 0;

				if (escapeVelocity > 1.0)
				{
					Debug.WriteLine($"The Escape Velocity is greater that 1.0");
				}

				var offset = pixPtr++ * 4;
				var dest = new Span<byte>(imageData, offset, 4);

				colorMap.PlaceColor(countVal, escapeVelocity, dest);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void FillPngImageLineSegment(ImageLine iLine, int pixPtr, ushort[]? counts, ushort[]? escapeVelocities, int lineLength, int samplesToSkip, ColorMap colorMap)
		{
			if (counts == null || escapeVelocities == null)
			{
				FillPngImageLineSegmentWithWhite(iLine, pixPtr, lineLength);
				return;
			}

			var cComps = new byte[4];
			var dest = new Span<byte>(cComps);

			var previousCountVal = counts[0];

			for (var xPtr = 0; xPtr < lineLength; xPtr++)
			{
				var countVal = counts[xPtr + samplesToSkip];

				if (countVal != previousCountVal)
				{
					//NumberOfCountValSwitches++;
					previousCountVal = countVal;
				}

				var escapeVelocity = colorMap.UseEscapeVelocities ? escapeVelocities[xPtr + samplesToSkip] / VALUE_FACTOR : 0;

				if (escapeVelocity > 1.0)
				{
					Debug.WriteLine($"The Escape Velocity is greater that 1.0");
				}

				colorMap.PlaceColor(countVal, escapeVelocity, dest);

				ImageLineHelper.SetPixel(iLine, pixPtr++, cComps[2], cComps[1], cComps[0]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FillImageLineSegmentWithWhite(Span<byte> imageLine, int pixPtr, int len)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				var offset = pixPtr++ * 4;

				imageLine[offset] = 255;
				imageLine[offset + 1] = 255;
				imageLine[offset + 2] = 255;
				imageLine[offset + 3] = 255;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FillPngImageLineSegmentWithWhite(ImageLine iLine, int pixPtr, int len)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="blockPtr">Which row is being processed.</param>
		/// <param name="invert">True if the Counts Array should be accessed from high to low index values, i.e., from top to bottom. The index into Counts increase from left to right, and from bottom to top.</param>
		/// <param name="extentInBlocksY"></param>
		/// <param name="heightOfFirstBlock"></param>
		/// <param name="heightOfLastBlock"></param>
		/// <param name="blockHeight"></param>
		/// <returns></returns>
		public static (int startingPtr, int endingPtr, int increment) GetNumberOfLines(int blockPtr, bool invert, int extentInBlocksY, int heightOfFirstBlock, int heightOfLastBlock, int blockHeight)
		{
			//var numberOfLines = BitmapHelper.GetNumberOfLines(blockPtrY, imageSize.Height, h, blockSize.Height, canvasControlOffset.Y, out var linesTopSkip);

			int startingLinePtr;
			int numberOfLines;

			if (invert)
			{
				// Invert = true is the normal case. Since in the vertical, Map Coordinates are opposite of screen coordinates.
				// The first row from counts is at the bottom of the image.
				// The last row from counts is at the top of the image.

				// The startingLinePtr starts big and is reduced by numberOfLines

				if (blockPtr == 0)
				{
					// This is the block with the smallest Map Coordinate and the largest Y screen coordinate (aka the first block)

					startingLinePtr = blockHeight - 1; //(heightOfFirstBlock ranges from 1 to 128, startingLinePtr ranges from 127 to 0)
					numberOfLines = heightOfFirstBlock;
				}
				else if (blockPtr == extentInBlocksY - 1)
				{
					// This is the block with the largest Map Coordinate and the smallest Y screen coordinate, (aka the last block)

					startingLinePtr = heightOfLastBlock - 1;
					numberOfLines = heightOfLastBlock;
				}
				else
				{
					startingLinePtr = blockHeight - 1;
					numberOfLines = blockHeight;
				}
			}
			else
			{
				// We are displaying a map section originally generated for an area with a positive Y values, in an area with negative Y values.
				// The content of any given block have either all postive or all negative Y values.
				// The content of these blocks must not be reversed

				// The first row from counts is at the top of the image.
				// The last row from counts is at the bottom of the image.

				// The startingLinePtr starts small and is increased by numberOfLines

				if (blockPtr == 0)
				{
					// This is the block with the smallest Map Coordinate and the largest Y screen coordinate (aka the first block)

					startingLinePtr = 0;
					numberOfLines = heightOfFirstBlock;
				}
				else if (blockPtr == extentInBlocksY - 1)
				{
					// This is the block with the largest Map Coordinate and the smallest Y screen coordinate, (aka the last block)

					startingLinePtr = blockHeight - heightOfLastBlock;
					numberOfLines = heightOfLastBlock;
				}
				else
				{
					startingLinePtr = 0;
					numberOfLines = blockHeight;
				}
			}


			var lineIncrement = invert ? -1 : 1;

			return (startingLinePtr, numberOfLines, lineIncrement);
		}

		// GetSegmentLengths
		public static ValueTuple<int, int>[] GetSegmentLengths(int numberOfWholeBlocksX, int widthOfFirstBlock, int widthOfLastBlock, int blockWidth)
		{
			var segmentLengths = new ValueTuple<int, int>[numberOfWholeBlocksX];

			for (var blockPtrX = 0; blockPtrX < numberOfWholeBlocksX; blockPtrX++)
			{
				int lineLength;
				int samplesToSkip;

				if (blockPtrX == 0)
				{
					samplesToSkip = blockWidth - widthOfFirstBlock;
					lineLength = widthOfFirstBlock;
				}
				else if (blockPtrX == numberOfWholeBlocksX - 1)
				{
					samplesToSkip = 0;
					lineLength = widthOfLastBlock;
				}
				else
				{
					samplesToSkip = 0;
					lineLength = blockWidth;
				}

				segmentLengths[blockPtrX] = new ValueTuple<int, int>(lineLength, samplesToSkip);
			}

			return segmentLengths;
		}



	}
}
