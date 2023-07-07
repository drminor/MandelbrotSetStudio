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

		public static int GetNumberOfLines(int blockPtrY, int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY, out int linesToSkip)
		{
			int numberOfLines;

			if (blockPtrY == 0)
			{
				// This is the block with the largest y coordinate (aka the last block)
				linesToSkip = 0;
				var numberOfLinesForFirstBlock = GetNumberOfLinesForFirstBlock(imageHeight, numberOfWholeBlocksY, blockHeight, canvasControlOffsetY);
				var numberOfLinesSoFar = numberOfLinesForFirstBlock + (blockHeight * (numberOfWholeBlocksY - 2));
				numberOfLines = imageHeight - numberOfLinesSoFar;
			}
			else if (blockPtrY == numberOfWholeBlocksY - 1)
			{
				// This is the block with the smallest y coordinate (aka the first block)
				numberOfLines = GetNumberOfLinesForFirstBlock(imageHeight, numberOfWholeBlocksY, blockHeight, canvasControlOffsetY);
				linesToSkip = blockHeight - numberOfLines; // (Since the pixel lines are accessed from high to low index, this is measured from index = blockHeight - 1)
			}
			else
			{
				linesToSkip = 0;
				numberOfLines = blockHeight;
			}

			return numberOfLines;

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetNumberOfLinesForFirstBlock(int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY)
		{
			return canvasControlOffsetY + imageHeight - (blockHeight * (numberOfWholeBlocksY - 1));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetSegmentLength(int blockPtrX, int imageWidth, int numberOfWholeBlocksX, int blockWidth, int canvasControlOffsetX, out int samplesToSkip)
		{
			// TODO: Build an array of Segment lengths, once and then re-use for each row.
			int result;

			if (blockPtrX == 0)
			{
				// TODO: Why does this work for the x-axis, but not the y-axis.
				samplesToSkip = canvasControlOffsetX;
				result = blockWidth - canvasControlOffsetX;
			}
			else if (blockPtrX == numberOfWholeBlocksX - 1)
			{
				samplesToSkip = 0;
				result = canvasControlOffsetX + imageWidth - (blockWidth * (numberOfWholeBlocksX - 1));
			}
			else
			{
				samplesToSkip = 0;
				result = blockWidth;
			}

			return result;
		}

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
		public static void FillImageLineSegmentWithWhite(Span<byte> imageLine, int pixPtr, int len)
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
		public static void FillPngImageLineSegmentWithWhite(ImageLine iLine, int pixPtr, int len)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

	}
}
