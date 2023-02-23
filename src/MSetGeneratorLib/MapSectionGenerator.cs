using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorLib
{
	public class MapSectionGenerator
	{
		public bool GenerateMapSection(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, uint threshold, CancellationToken ct)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, threshold);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Make the call -- TODO: return allRowSamplesHaveEscaped
			NativeMethods.GenerateMapSection(requestStruct, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			var allRowSamplesHaveEscaped = false;
			return allRowSamplesHaveEscaped;
		}

		private byte[] GetCounts(IIterationState iterationState, int rowNumber)
		{
			//for(var i = 0; i < 64; i++)
			//{
			//	iterationState.MapSectionVectors.Counts[i] = (ushort)i;
			//}

			var buffer = new byte[iterationState.MapSectionVectors.BytesPerRow * 2]; // Using Vector of uints, not ushorts
			iterationState.MapSectionVectors.FillCountsRow(rowNumber, buffer);

			return buffer;
		}

		private MapSectionRequestStruct GetRequestStruct(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, uint threshold)
		{
			var result = new MapSectionRequestStruct();

			if (!iterationState.RowNumber.HasValue)
			{
				throw new ArgumentException("The iteration state must have a non-null row number.");
			}

			result.RowNumber = iterationState.RowNumber.Value;

			result.BitsBeforeBinaryPoint = apFixedPointFormat.BitsBeforeBinaryPoint;
			result.LimbCount = apFixedPointFormat.LimbCount;
			result.NumberOfFractionalBits = apFixedPointFormat.NumberOfFractionalBits;
			result.TotalBits = apFixedPointFormat.TotalBits;
			result.TargetExponent = apFixedPointFormat.TargetExponent;

			result.Lanes = Vector256<int>.Count;
			result.VectorsPerRow = iterationState.VectorsPerRow;

			result.subdivisionId = ObjectId.Empty.ToString();

			result.blockSizeWidth = iterationState.ValuesPerRow;
			result.blockSizeHeight = iterationState.RowCount;

			result.maxIterations = iterationState.TargetIterationsVector.GetElement(0);
			result.threshold = (int)threshold;
			result.iterationsPerStep = -1;

			return result;
		}

	}
}
