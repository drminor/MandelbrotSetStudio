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
		private const int BYTES_PER_VECTOR = 32; // Also used for alignment

		#region Public Methods

		unsafe public bool GenerateMapSectionRow(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings, CancellationToken ct)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			_ = GetSamplePointsX(iterationState, out var spxBuffer);

			// SamplePointY
			_ = GetYPointVecs(iterationState, out var ypBuffer);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber, out var countsBuffer);

			// Generate a MapSectionRow
			var intResult = HpMSetGeneratorImports.GenerateMapSectionRow(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, (IntPtr)countsBuffer);

			// Counts
			Marshal.Copy((IntPtr)countsBuffer, counts, 0, counts.Length);
			FreeInteropBuffer(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			//Marshal.FreeCoTaskMem(ypBuffer);
			//Marshal.FreeCoTaskMem(spxBuffer);
			FreeInteropBuffer(ypBuffer);
			FreeInteropBuffer(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			//Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		#endregion

		#region Test Support 

		unsafe public bool BaseSimdTest(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber, out var countsBuffer);

			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest(requestStruct, (IntPtr)countsBuffer);

			// Counts
			Marshal.Copy((IntPtr)countsBuffer, counts, 0, counts.Length);
			FreeInteropBuffer(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;

			return allRowSamplesHaveEscaped;
		}

		unsafe public bool BaseSimdTest2(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			_ = GetSamplePointsX(iterationState, out var spxBuffer);

			// SamplePointY
			_ = GetYPointVecs(iterationState, out var ypBuffer);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber, out var countsBuffer);

			// Call BaseSimdTest2
			var intResult = HpMSetGeneratorImports.BaseSimdTest2(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, (IntPtr)countsBuffer);

			// Counts
			Marshal.Copy((IntPtr)countsBuffer, counts, 0, counts.Length);
			FreeInteropBuffer(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			//Marshal.FreeCoTaskMem(ypBuffer);
			//Marshal.FreeCoTaskMem(spxBuffer);
			FreeInteropBuffer(ypBuffer);
			FreeInteropBuffer(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		unsafe public bool BaseSimdTest3(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			_ = GetSamplePointsX(iterationState, out var spxBuffer);

			// SamplePointY
			_ = GetYPointVecs(iterationState, out var ypBuffer);

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber, out var countsBuffer);

			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest3(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, (IntPtr)countsBuffer);

			// Counts
			Marshal.Copy((IntPtr)countsBuffer, counts, 0, counts.Length);
			FreeInteropBuffer(countsBuffer);
			PutCounts(iterationState, requestStruct.RowNumber, counts);

			//Marshal.FreeCoTaskMem(ypBuffer);
			//Marshal.FreeCoTaskMem(spxBuffer);
			FreeInteropBuffer(ypBuffer);
			FreeInteropBuffer(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		#endregion

		#region Support Methods

		unsafe private byte[] GetSamplePointsX(IIterationState iterationState, out void* spxBuffer)
		{
			var resultBuffer = new byte[iterationState.MapSectionZVectors.BytesPerZValueRow];
			iterationState.FillSamplePointsXBuffer(resultBuffer);

			//spxBuffer = Marshal.AllocCoTaskMem(resultBuffer.Length);
			//Marshal.Copy(resultBuffer, 0, spxBuffer, resultBuffer.Length);
			
			spxBuffer = NativeMemory.AlignedAlloc((nuint)resultBuffer.Length, BYTES_PER_VECTOR);
			Marshal.Copy(resultBuffer, 0, (IntPtr)spxBuffer, resultBuffer.Length);

			return resultBuffer;
		}

		unsafe private byte[] GetYPointVecs(IIterationState iterationState, out void* ypBuffer)
		{
			var resultBuffer = new byte[iterationState.MapSectionZVectors.LimbCount * BYTES_PER_VECTOR]; 
			iterationState.FillSamplePointYBuffer(resultBuffer);

			//ypBuffer =  Marshal.AllocCoTaskMem(resultBuffer.Length);
			//Marshal.Copy(resultBuffer, 0, ypBuffer, resultBuffer.Length);

			ypBuffer = NativeMemory.AlignedAlloc((nuint)resultBuffer.Length, BYTES_PER_VECTOR);
			Marshal.Copy(resultBuffer, 0, (IntPtr)ypBuffer, resultBuffer.Length);


			return resultBuffer;
		}

		unsafe private byte[] GetCounts(IIterationState iterationState, int rowNumber, out void* countsBuffer)
		{
			// MapSectionVectors uses a VALUE_SIZE of 2
			// The Generator needs these shorts converted to ints
			var countsLength = iterationState.MapSectionVectors.BytesPerRow * 2;
			Debug.Assert(countsLength == iterationState.MapSectionZVectors.BytesPerRow, "Counts Length MisMatch");

			var resultBuffer = new byte[countsLength];
			iterationState.FillCountsRow(rowNumber, resultBuffer);

			//countsBuffer = Marshal.AllocCoTaskMem(resultBuffer.Length);
			//Marshal.Copy(resultBuffer, 0, countsBuffer, resultBuffer.Length);

			countsBuffer = NativeMemory.AlignedAlloc((nuint)resultBuffer.Length, BYTES_PER_VECTOR);
			Marshal.Copy(resultBuffer, 0, (IntPtr)countsBuffer, resultBuffer.Length);

			return resultBuffer;
		}

		private void PutCounts(IIterationState iterationState, int rowNumber, byte[] counts)
		{
			iterationState.UpdateFromCountsRow(rowNumber, counts);
		}

		unsafe private void FreeInteropBuffer(void* buffer)
		{
			//Marshal.FreeCoTaskMem(buffer);
			NativeMemory.AlignedFree(buffer);
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
