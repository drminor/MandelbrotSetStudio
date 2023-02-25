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

			// SamplePointsX
			var samplePointsX = GetSamplePointsX(iterationState);
			var spxBuffer = Marshal.AllocCoTaskMem(samplePointsX.Length);
			Marshal.Copy(samplePointsX, 0, spxBuffer, samplePointsX.Length);

			// SamplePointY
			var yPointVecs = GetYPointVecs(iterationState);
			var ypBuffer = Marshal.AllocCoTaskMem(yPointVecs.Length);
			Marshal.Copy(yPointVecs, 0, ypBuffer, yPointVecs.Length);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Generate a MapSectionRow
			var intResult = NativeMethods.GenerateMapSectionRow(requestStruct, spxBuffer, ypBuffer, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			Marshal.FreeCoTaskMem(ypBuffer);
			Marshal.FreeCoTaskMem(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			//Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;



		}

		#endregion

		#region Test Support 

		public bool BaseSimdTest(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Call BaseSimdTest
			var intResult = NativeMethods.BaseSimdTest(requestStruct, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		public bool BaseSimdTest2(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			var samplePointsX = GetSamplePointsX(iterationState);
			var spxBuffer = Marshal.AllocCoTaskMem(samplePointsX.Length);
			Marshal.Copy(samplePointsX, 0, spxBuffer, samplePointsX.Length);

			// SamplePointY
			var yPointVecs = GetYPointVecs(iterationState);
			var ypBuffer = Marshal.AllocCoTaskMem(yPointVecs.Length);
			Marshal.Copy(yPointVecs, 0, ypBuffer, yPointVecs.Length);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Call BaseSimdTest
			var intResult = NativeMethods.BaseSimdTest2(requestStruct, spxBuffer, ypBuffer, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			Marshal.FreeCoTaskMem(ypBuffer);
			Marshal.FreeCoTaskMem(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		public bool BaseSimdTest3(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			var samplePointsX = GetSamplePointsX(iterationState);
			var spxBuffer = Marshal.AllocCoTaskMem(samplePointsX.Length);
			Marshal.Copy(samplePointsX, 0, spxBuffer, samplePointsX.Length);

			// SamplePointY
			var yPointVecs = GetYPointVecs(iterationState);
			var ypBuffer = Marshal.AllocCoTaskMem(yPointVecs.Length);
			Marshal.Copy(yPointVecs, 0, ypBuffer, yPointVecs.Length);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Call BaseSimdTest
			var intResult = NativeMethods.BaseSimdTest3(requestStruct, spxBuffer, ypBuffer, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			Marshal.FreeCoTaskMem(ypBuffer);
			Marshal.FreeCoTaskMem(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		#endregion

		#region Support Methods

		private byte[] GetSamplePointsX(IIterationState iterationState)
		{
			var buffer = new byte[iterationState.MapSectionZVectors.BytesPerZValueRow];
			iterationState.FillSamplePointsXBuffer(buffer);
			return buffer;
		}

		private const int BYTES_PER_VECTOR = 32;

		private byte[] GetYPointVecs(IIterationState iterationState)
		{
			var buffer = new byte[iterationState.MapSectionZVectors.LimbCount * BYTES_PER_VECTOR]; 
			iterationState.FillSamplePointYBuffer(buffer);

			return buffer;
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

		private void PutCounts(IIterationState iterationState, int rowNumber, byte[] counts)
		{
			iterationState.MapSectionVectors.UpdateFromCountsRow(rowNumber, counts);
		}

		private MSetRowRequestStruct GetRequestStruct(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var result = new MSetRowRequestStruct();

			if (!iterationState.RowNumber.HasValue)
			{
				throw new ArgumentException("The iteration state must have a non-null row number.");
			}

			result.BlockSizeWidth = iterationState.ValuesPerRow;
			result.BlockSizeHeight = iterationState.RowCount;

			result.BitsBeforeBinaryPoint = apFixedPointFormat.BitsBeforeBinaryPoint;
			result.LimbCount = apFixedPointFormat.LimbCount;
			result.NumberOfFractionalBits = apFixedPointFormat.NumberOfFractionalBits;
			result.TotalBits = apFixedPointFormat.TotalBits;
			result.TargetExponent = apFixedPointFormat.TargetExponent;

			result.Lanes = Vector256<int>.Count;
			result.VectorsPerRow = iterationState.VectorsPerRow;

			//result.subdivisionId = ObjectId.Empty.ToString();

			// The RowNumber to calculate
			result.RowNumber = iterationState.RowNumber.Value;

			result.TargetIterations = mapCalcSettings.TargetIterations;

			var thresholdForComparison = GetThresholdValueForCompare(mapCalcSettings.Threshold, apFixedPointFormat);

			result.ThresholdForComparison = thresholdForComparison;
			result.IterationsPerStep = -1;

			return result;
		}

		private int GetThresholdValueForCompare(int thresholdRaw, ApFixedPointFormat apFixedPointFormat)
		{
			var fp31Val = FP31ValHelper.CreateFP31Val(new RValue(thresholdRaw, 0), apFixedPointFormat);
			var msl = (int)fp31Val.Mantissa[^1] - 1;

			return msl;
		}

		#endregion
	}
}
