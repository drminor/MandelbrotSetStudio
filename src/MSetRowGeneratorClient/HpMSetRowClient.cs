using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetRowGeneratorClient
{
	public class HpMSetRowClient : IDisposable
	{
		private const int MEM_ALLOCATION_ALIGNMENT = 32;

		private const int BLOCK_WIDTH = 128;
		private const int VALUE_SIZE = 4;
		private const int LANES = 8;
		private const int MAX_LIMB_COUNT = 4;

		private const int COUNTS_BUFFER_SIZE = BLOCK_WIDTH * VALUE_SIZE;								//	128 x 4  
		private const int SAMPLE_POINTS_X_BUFFER_SIZE = MAX_LIMB_COUNT * BLOCK_WIDTH * VALUE_SIZE;		//	4 x 128 x 4
		private const int SAMPLE_POINT_Y_BUFFER_SIZE = MAX_LIMB_COUNT * LANES * VALUE_SIZE;				//	4 x 8 x 4

		private readonly IntPtr _countsBuffer;
		private readonly IntPtr _samplePointsXBuffer;
		private readonly IntPtr _yPointBuffer;

		public HpMSetRowClient()
		{
			unsafe
			{
				_countsBuffer = (IntPtr)NativeMemory.AlignedAlloc(COUNTS_BUFFER_SIZE, MEM_ALLOCATION_ALIGNMENT);
				_samplePointsXBuffer = (IntPtr)NativeMemory.AlignedAlloc(SAMPLE_POINTS_X_BUFFER_SIZE, MEM_ALLOCATION_ALIGNMENT);
				_yPointBuffer = (IntPtr)NativeMemory.AlignedAlloc(SAMPLE_POINT_Y_BUFFER_SIZE, MEM_ALLOCATION_ALIGNMENT);
			}
		}

		#region Public Methods

		unsafe public bool GenerateMapSectionRow(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings, CancellationToken ct)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			GetSamplePointsX(iterationState);

			// SamplePointY
			GetYPointVecs(iterationState);

			// Counts
			GetCounts(iterationState);

			// Generate a MapSectionRow
			var intResult = HpMSetGeneratorImports.GenerateMapSectionRow(requestStruct, _samplePointsXBuffer, _yPointBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

			//FreeInteropBuffer(ypBuffer);
			//FreeInteropBuffer(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			//Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		#endregion

		#region Test Support 

		unsafe public bool BaseSimdTest(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest();
			var success = intResult == 0 ? false : true;

			return success;
		}

		unsafe public bool BaseSimdTest2(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			GetSamplePointsX(iterationState);

			// SamplePointY
			GetYPointVecs(iterationState);

			// Counts
			GetCounts(iterationState);

			// Call BaseSimdTest2
			var intResult = HpMSetGeneratorImports.BaseSimdTest2(requestStruct, _samplePointsXBuffer, _yPointBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		unsafe public bool BaseSimdTest3(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			GetSamplePointsX(iterationState);

			// SamplePointY
			GetYPointVecs(iterationState);

			// Counts
			GetCounts(iterationState);

			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest3(requestStruct, _samplePointsXBuffer, _yPointBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		unsafe public bool BaseSimdTest4(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			GetSamplePointsX(iterationState);

			// SamplePointY
			GetYPointVecs(iterationState);

			// Counts
			GetCounts(iterationState);

			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest4(requestStruct, _samplePointsXBuffer, _yPointBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		unsafe public bool RoundTripCounts(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);


			ushort cVal = 0;

			for (var i = 0; i < iterationState.MapSectionVectors.Counts.Length; i++)
			{
				if (ushort.MaxValue - cVal < 30)
				{
					cVal = 0;
				}

				cVal += 27;

				iterationState.MapSectionVectors.Counts[i] = cVal;
			}

			// Sum the first row from the original source.
			var totalBefore = iterationState.MapSectionVectors.Counts.Take(iterationState.ValuesPerRow).Sum(x => (long)x);

			// Update the Iterations state's current row of Vector256<int>s
			iterationState.MapSectionVectors.FillCountsRow(0, iterationState.CountsRowV);


			// Load into an array of integers the contents of CountsRowV
			var diagCounts = new int[iterationState.MapSectionVectors.ValuesPerRow];
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, diagCounts);

			// Measure
			var rowSumBefore = diagCounts.Sum();

			// Fill Unmanaged Buffer -- these are used by the C++ imp.
			GetCounts(iterationState);

			// Updated the interation state from this buffer.
			PutCounts(iterationState);

			// Refresh the counts
			Array.Clear(diagCounts);
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, diagCounts);

			// And re-measure.
			var rowSumAfter = diagCounts.Sum();

			return rowSumAfter == rowSumBefore;
		}

		#endregion

		#region Support Methods

		private void GetCounts(IIterationState iterationState)
		{
			var srcSpan = MemoryMarshal.Cast<Vector256<int>, byte>(iterationState.CountsRowV);

			unsafe
			{
				var dstSpan = new Span<byte>((void*)_countsBuffer, COUNTS_BUFFER_SIZE);
				srcSpan.CopyTo(dstSpan);
			}
		}

		private void GetSamplePointsX(IIterationState iterationState)
		{
			var srcSpan = MemoryMarshal.Cast<Vector256<uint>, byte>(iterationState.CrsRowVArray.Mantissas);

			unsafe
			{
				var dstSpan = new Span<byte>((void*)_samplePointsXBuffer, SAMPLE_POINTS_X_BUFFER_SIZE);
				srcSpan.CopyTo(dstSpan);
			}
		}

		private void GetYPointVecs(IIterationState iterationState)
		{
			var srcSpan = MemoryMarshal.Cast<Vector256<uint>, byte>(iterationState.CiLimbSet);

			unsafe
			{
				var dstSpan = new Span<byte>((void*)_yPointBuffer, SAMPLE_POINT_Y_BUFFER_SIZE);
				srcSpan.CopyTo(dstSpan);
			}
		}

		private void PutCounts(IIterationState iterationState)
		{
			var dstSpan = MemoryMarshal.Cast<Vector256<int>, byte>(iterationState.CountsRowV);

			unsafe
			{
				var srcSpan = new Span<byte>((void*)_countsBuffer, COUNTS_BUFFER_SIZE);
				srcSpan.CopyTo(dstSpan);
			}
		}

		unsafe private void FreeInteropBuffer(void* buffer)
		{
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

		#region IDisposable

		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)

					unsafe
					{
						FreeInteropBuffer((void*)_countsBuffer);
						FreeInteropBuffer((void*)_samplePointsXBuffer);
						FreeInteropBuffer((void*)_yPointBuffer);
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null



				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~HpMSetRowClient()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
