using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetRowGeneratorClient
{
	public class HpMSetRowClient : IDisposable
	{
		private const int MEM_ALLOCATION_ALIGNMENT = 32;
		private const int BYTES_PER_VECTOR = 32;			// Lanes * VALUE_SIZE				(8 x 4)
		private const int COUNTS_BUFFER_SIZE = 512;			// BlockSize.Width * VALUE_SIZE		(128 x 4)  


		private readonly IntPtr _countsBuffer;


		public HpMSetRowClient()
		{
			unsafe
			{
				_countsBuffer = (IntPtr)NativeMemory.AlignedAlloc(COUNTS_BUFFER_SIZE, MEM_ALLOCATION_ALIGNMENT);
			}
		}


		#region Public Methods

		unsafe public bool GenerateMapSectionRow(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings, CancellationToken ct)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			_ = GetSamplePointsX(iterationState, out var spxBuffer);

			// SamplePointY
			_ = GetYPointVecs(iterationState, out var ypBuffer);

			// Counts
			GetCounts(iterationState);

			// Generate a MapSectionRow
			var intResult = HpMSetGeneratorImports.GenerateMapSectionRow(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

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
			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest();
			var success = intResult == 0 ? false : true;

			return success;
		}

		unsafe public bool BaseSimdTest2(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			_ = GetSamplePointsX(iterationState, out var spxBuffer);

			// SamplePointY
			_ = GetYPointVecs(iterationState, out var ypBuffer);

			// Counts
			GetCounts(iterationState);

			// Call BaseSimdTest2
			var intResult = HpMSetGeneratorImports.BaseSimdTest2(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

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
			GetCounts(iterationState);

			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest3(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

			FreeInteropBuffer(ypBuffer);
			FreeInteropBuffer(spxBuffer);

			var allRowSamplesHaveEscaped = intResult == 0 ? false : true;
			Debug.WriteLine($"All row samples have escaped: {allRowSamplesHaveEscaped}.");

			return allRowSamplesHaveEscaped;
		}

		unsafe public bool BaseSimdTest4(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, MapCalcSettings mapCalcSettings)
		{
			var requestStruct = GetRequestStruct(iterationState, apFixedPointFormat, mapCalcSettings);

			// SamplePointsX
			_ = GetSamplePointsX(iterationState, out var spxBuffer);

			// SamplePointY
			_ = GetYPointVecs(iterationState, out var ypBuffer);

			// Counts
			GetCounts(iterationState);

			// Call BaseSimdTest
			var intResult = HpMSetGeneratorImports.BaseSimdTest4(requestStruct, (IntPtr)spxBuffer, (IntPtr)ypBuffer, _countsBuffer);

			// Counts
			PutCounts(iterationState);

			FreeInteropBuffer(ypBuffer);
			FreeInteropBuffer(spxBuffer);

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

		unsafe private byte[] GetSamplePointsX(IIterationState iterationState, out void* spxBuffer)
		{
			var resultBuffer = new byte[iterationState.MapSectionZVectors.BytesPerZValueRow];
			iterationState.FillSamplePointsXBuffer(resultBuffer);
		
			spxBuffer = NativeMemory.AlignedAlloc((nuint)resultBuffer.Length, MEM_ALLOCATION_ALIGNMENT);
			Marshal.Copy(resultBuffer, 0, (IntPtr)spxBuffer, resultBuffer.Length);

			return resultBuffer;
		}

		unsafe private byte[] GetYPointVecs(IIterationState iterationState, out void* ypBuffer)
		{
			var resultBuffer = new byte[iterationState.MapSectionZVectors.LimbCount * BYTES_PER_VECTOR]; 
			iterationState.FillSamplePointYBuffer(resultBuffer);

			ypBuffer = NativeMemory.AlignedAlloc((nuint)resultBuffer.Length, MEM_ALLOCATION_ALIGNMENT);
			Marshal.Copy(resultBuffer, 0, (IntPtr)ypBuffer, resultBuffer.Length);

			return resultBuffer;
		}

		private void GetCounts(IIterationState iterationState)
		{
			var srcSpan = MemoryMarshal.Cast<Vector256<int>, byte>(iterationState.CountsRowV);
			
			//var countsBuffer = NativeMemory.AlignedAlloc(COUNTS_BUFFER_SIZE, MEM_ALLOCATION_ALIGNMENT);

			unsafe
			{
				var dstSpan = new Span<byte>((void*)_countsBuffer, COUNTS_BUFFER_SIZE);
				srcSpan.CopyTo(dstSpan);
			}
		}

		unsafe private void PutCounts(IIterationState iterationState)
		{
			var countsLength = iterationState.MapSectionVectors.BytesPerRow * 2;

			var srcSpan = new Span<byte>((void*)_countsBuffer, countsLength);
			var dstSpan = MemoryMarshal.Cast<Vector256<int>, byte>(iterationState.CountsRowV);
			srcSpan.CopyTo(dstSpan);
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
