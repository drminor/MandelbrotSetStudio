using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetRowGeneratorClient
{
	public class HpMSetRowClient
	{
		#region Public Methods

		public bool GenerateMapSectionRow(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings, CancellationToken ct)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Make the call -- TODO: return allRowSamplesHaveEscaped
			//NativeMethods.GenerateMapSectionRow(requestStruct, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			var allRowSamplesHaveEscaped = false;
			return allRowSamplesHaveEscaped;
		}

		#endregion

		#region Support Methods

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

		private MSetRowRequestStruct GetRequestStruct(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var result = new MSetRowRequestStruct();

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

			//result.subdivisionId = ObjectId.Empty.ToString();

			result.blockSizeWidth = iterationState.ValuesPerRow;
			result.blockSizeHeight = iterationState.RowCount;

			result.maxIterations = mapCalcSettings.TargetIterations;

			var thresholdForComparison = GetThresholdValueForCompare(mapCalcSettings.Threshold, apFixedPointFormat);

			result.thresholdForComparison = thresholdForComparison;
			result.iterationsPerStep = -1;

			return result;
		}

		private int GetThresholdValueForCompare(int thresholdRaw, ApFixedPointFormat apFixedPointFormat)
		{
			var fp31Val = FP31ValHelper.CreateFP31Val(new RValue(thresholdRaw, 0), apFixedPointFormat);
			var msl = (int)fp31Val.Mantissa[^1] - 1;

			return msl;
		}

		#endregion

		#region Test Support 

		public bool BaseSimdTest(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length * 5);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Make the call -- TODO: return allRowSamplesHaveEscaped
			var intResult = NativeMethods.BaseSimdTest(requestStruct, countsBuffer);

			Debug.WriteLine($"The intResult is {intResult}.");

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			var allRowSamplesHaveEscaped = false;
			return allRowSamplesHaveEscaped;
		}

		#endregion

	}
}
