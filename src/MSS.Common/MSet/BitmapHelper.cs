using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSS.Common.MSet
{
	public class BitmapHelper
	{
		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public long LoadPixelArray(MapSection mapSection, ColorMap colorMap, byte[] imageData)
		{
			bool drawInverted = !mapSection.IsInverted;

			var mapSectionVectors = mapSection.MapSectionVectors;

			if (mapSectionVectors == null) return 0;

			Debug.Assert(mapSectionVectors.ReferenceCount > 0, "Getting the Pixel Array from a MapSectionVectors whose RefCount is < 1.");

			var errors = 0L;
			var useEscapeVelocities = colorMap.UseEscapeVelocities;

			var rowCount = mapSectionVectors.BlockSize.Height;
			var colCount = mapSectionVectors.BlockSize.Width;
			var maxRowIndex = rowCount - 1;

			var pixelStride = colCount * BYTES_PER_PIXEL;

			//var backBuffer = mapSectionVectors.BackBuffer;

			Debug.Assert(imageData.Length == mapSectionVectors.BlockSize.NumberOfCells * BYTES_PER_PIXEL);

			var counts = mapSectionVectors.Counts;
			var previousCountVal = counts[0];

			var resultRowPtr = drawInverted ? maxRowIndex * pixelStride : 0;
			var resultRowPtrIncrement = drawInverted ? -1 * pixelStride : pixelStride;
			var sourcePtrUpperBound = rowCount * colCount;

			if (useEscapeVelocities)
			{
				var escapeVelocities = mapSectionVectors.EscapeVelocities;

				CheckMissingEscapeVelocities(escapeVelocities);

				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < colCount; colPtr++)
					{
						var countVal = counts[sourcePtr];
						//TrackValueSwitches(countVal, ref previousCountVal);

						var escapeVelocity = escapeVelocities[sourcePtr] / VALUE_FACTOR;
						CheckEscapeVelocity(escapeVelocity);

						var destination = new Span<byte>(imageData, resultPtr, BYTES_PER_PIXEL);
						errors += colorMap.PlaceColor(countVal, escapeVelocity, destination);

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}
			else
			{
				// The main for loop on GetPixel Array 
				// is for each row of pixels (0 -> 128)
				//		for each pixel in that row (0, -> 128)
				// each new row advanced the resultRowPtr to the pixel byte address at column 0 of the current row.
				// if inverted, the first row = 127 * # of bytes / Row (Pixel stride)

				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < colCount; colPtr++)
					{
						var countVal = counts[sourcePtr];
						TrackValueSwitches(countVal, ref previousCountVal);

						var destination = new Span<byte>(imageData, resultPtr, BYTES_PER_PIXEL);
						errors += colorMap.PlaceColor(countVal, escapeVelocity: 0, destination);

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}

			//mapSectionVectors.BackBufferIsLoaded = true;

			return errors;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long FillImageLineSegment(MapSection mapSection, ColorMap colorMap, byte[] imageData, int pixPtr, int linePtr, int lineLength, int samplesToSkip)
		{
			var errors = 0L;

			var counts = mapSection.GetOneLineFromCountsBlock(linePtr);
			var escapeVelocities = mapSection.GetOneLineFromEscapeVelocitiesBlock(linePtr);

			if (counts == null || escapeVelocities == null)
			{
				FillImageLineSegmentWithWhite(imageData, pixPtr, lineLength);
				return errors;
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

				errors += colorMap.PlaceColor(countVal, escapeVelocity, dest);
			}

			return errors;
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

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void TrackValueSwitches(ushort countVal, ref ushort previousCountVal)
		{
			if (countVal != previousCountVal)
			{
				NumberOfCountValSwitches++;
				previousCountVal = countVal;
			}
		}

		[Conditional("DEBUG2")]
		private void CheckEscapeVelocity(double escapeVelocity)
		{
			if (escapeVelocity > 1.0)
			{
				Debug.WriteLine($"WARNING: The Escape Velocity is greater than 1.0");
			}
		}

		[Conditional("DEBUG2")]
		private void CheckMissingEscapeVelocities(ushort[] escapeVelocities)
		{
			if (!escapeVelocities.Any(x => x > 0))
			{
				Debug.WriteLine("No EscapeVelocities Found.");
			}
		}

		#endregion
	}
}
