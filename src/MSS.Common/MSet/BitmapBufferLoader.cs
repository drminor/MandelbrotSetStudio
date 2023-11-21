using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common.MSet
{
	public class BitmapBufferLoader
	{
		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public long LoadPixelArray(MapSection mapSection, ColorMap colorMap)
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

			var backBuffer = mapSectionVectors.BackBuffer;

			Debug.Assert(backBuffer.Length == mapSectionVectors.BlockSize.NumberOfCells * BYTES_PER_PIXEL);

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

						var destination = new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL);
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

						var destination = new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL);
						errors += colorMap.PlaceColor(countVal, escapeVelocity: 0, destination);

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}

			mapSectionVectors.BackBufferIsLoaded = true;

			return errors;
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
