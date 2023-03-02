using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorUPointers : IMapSectionGenerator
	{
		#region Private Properties

		private SamplePointBuilder _samplePointBuilder;

		private FP31VecMathUPointers _fp31VecMathUPointers;
		private IteratorUPointers _iterator;

		private Vector256<uint>[] _crs;
		private Vector256<uint>[] _cis;
		private Vector256<uint>[] _zrs;
		private Vector256<uint>[] _zis;

		private Vector256<uint>[] _resultZrs;
		private Vector256<uint>[] _resultZis;

		private readonly Vector256<int> _justOne;

		#endregion

		#region Constructor

		public MapSectionGeneratorUPointers(int limbCount, SizeInt blockSize)
		{
			_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSize));

			//_fp31VecMath = _samplePointBuilder.GetVecMath(limbCount);
			_fp31VecMathUPointers = new FP31VecMathUPointers(new ApFixedPointFormat(limbCount));


				
			_iterator = new IteratorUPointers(_fp31VecMathUPointers);

			_crs = new Vector256<uint>[limbCount];
			_cis = new Vector256<uint>[limbCount];
			_zrs = new Vector256<uint>[limbCount];
			_zis = new Vector256<uint>[limbCount];

			_resultZrs = new Vector256<uint>[limbCount];
			_resultZis = new Vector256<uint>[limbCount];

			_justOne = Vector256.Create(1);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var (currentLimbCount, limbCountForThisRequest) = GetMathAndAllocateTempVars(mapSectionRequest);
			var coords = GetCoordinates(mapSectionRequest, _fp31VecMathUPointers.ApFixedPointFormat);
			ReportLimbCountUpdate(coords, currentLimbCount, limbCountForThisRequest, mapSectionRequest.Precision);

			var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest);

			//var processingSquare1 = coords.ScreenPos.X == 1 && coords.ScreenPos.Y == 1;

			//if (!processingSquare1)
			//{
			//	return new MapSectionResponse(mapSectionRequest, requestCompleted: false, allRowsHaveEscaped: false, mapSectionVectors, mapSectionZVectors);
			//}

			var stopwatch = Stopwatch.StartNew();
			//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);

			var (samplePointsX, samplePointsY) = _samplePointBuilder.BuildSamplePoints(coords);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			var mapCalcSettings = mapSectionRequest.MapCalcSettings;
			_iterator.Threshold = (uint)mapCalcSettings.Threshold;
			_iterator.IncreasingIterations = mapSectionRequest.IncreasingIterations;
			_iterator.MathOpCounts.Reset();
			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);

			var iterationState = new IterationStateUPointers(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, targetIterationsVector);

			var completed = GeneratorOrUpdateRows(_iterator, iterationState, ct, out var allRowsHaveEscaped);
			stopwatch.Stop();
			var result = new MapSectionResponse(mapSectionRequest, completed, allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors);
			mapSectionRequest.GenerationDuration = stopwatch.Elapsed;
			UpdateRequestWithMops(mapSectionRequest, _iterator, iterationState);
			ReportResults(coords, mapSectionRequest, result, ct);

			return result;
		}

		private bool GeneratorOrUpdateRows(IteratorUPointers iterator, IterationStateUPointers iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			bool completed;

			if (_iterator.IncreasingIterations)
			{
				completed = UpdateMapSectionRows(iterator, iterationState, ct, out allRowsHaveEscaped);
			}
			else
			{
				completed = GenerateMapSectionRows(iterator, iterationState, ct, out allRowsHaveEscaped);
			}

			return completed;
		}

		private bool GenerateMapSectionRows(IteratorUPointers iterator, IterationStateUPointers iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			allRowsHaveEscaped = false;

			if (ct.IsCancellationRequested)
			{
				return false;
			}

			allRowsHaveEscaped = true;

			for(var rowNumber = 0; rowNumber < iterationState.RowCount; rowNumber++)
			{
				iterationState.SetRowNumber(rowNumber);

				var allRowSamplesHaveEscaped = true;
				for (var idx = 0; idx < iterationState.VectorsPerRow; idx++)
				{
					var allSamplesHaveEscaped = GenerateMapCol(idx, iterator, iterationState);

					if (!allSamplesHaveEscaped)
					{
						allRowSamplesHaveEscaped = false;
					}
				}

				iterationState.RowHasEscaped[rowNumber] = allRowSamplesHaveEscaped;

				if (!allRowSamplesHaveEscaped)
				{
					allRowsHaveEscaped = false;
				}

				if (ct.IsCancellationRequested)
				{
					allRowsHaveEscaped = false;
					return false;
				}
			}

			// 'Close out' the iterationState
			iterationState.SetRowNumber(iterationState.RowCount);

			Debug.Assert(iterationState.RowNumber == null, $"The iterationState should have a null RowNumber, but instead has {iterationState.RowNumber}.");

			return true;
		}

		#endregion

		#region Generate One Vector Int

		private unsafe bool GenerateMapCol(int idx, IteratorUPointers iterator, IterationStateUPointers iterationState)
		{
			var hasEscapedFlagsV = Vector256<int>.Zero;
			var doneFlagsV = Vector256<int>.Zero;

			var countsV = Vector256<int>.Zero;
			var resultCountsV = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			_cis = iterationState.CiLimbSet;

			var vCrs = GetVecBuffer(_crs);
			var vCis = GetVecBuffer(_cis);
			var vZrs = GetEmptyVecBuffer(iterationState.LimbCount * 32);
			var vZis = GetEmptyVecBuffer(iterationState.LimbCount * 32);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;

			iterator.IterateFirstRound(vCrs, vCis, vZrs, vZis, ref escapedFlagsVec);
			var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			while (compositeIsDone != -1)
			{
				iterator.Iterate(vCrs, vCis, vZrs, vZis, ref escapedFlagsVec);
				compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);
			}

			TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV, iterationState.UsedCalcs, iterationState.UnusedCalcs);

			iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			iterationState.CountsRowV[idx] = resultCountsV;

			iterationState.UpdateZrLimbSet(idx, _resultZrs);
			iterationState.UpdateZiLimbSet(idx, _resultZis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			return compositeAllEscaped == -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int UpdateCounts(Vector256<int> escapedFlagsVec, 
			ref Vector256<int> countsV, ref Vector256<int> resultCountsV,
			ref Vector256<int> hasEscapedFlagsV, ref Vector256<int> doneFlagsV, Vector256<int> targetIterationsV)
		{
			countsV = Avx2.Add(countsV, _justOne);

			// Apply the new escapedFlags, only if the doneFlags is false for each vector position
			hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec, hasEscapedFlagsV, doneFlagsV);

			// Compare the new Counts with the TargetIterations
			var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsV);

			var prevDoneFlagsV = doneFlagsV;

			// If escaped or reached the target iterations, we're done 
			doneFlagsV = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec);

			var compositeIsDone = Avx2.MoveMask(doneFlagsV.AsByte());

			var prevCompositeIsDone = Avx2.MoveMask(prevDoneFlagsV.AsByte());
			if (compositeIsDone != prevCompositeIsDone)
			{
				var justNowDone = Avx2.CompareEqual(prevDoneFlagsV, doneFlagsV);

				// Save the current count 
				resultCountsV = Avx2.BlendVariable(countsV, resultCountsV, justNowDone); // use First if Zero, second if 1

				//// Save the current ZValues.
				//for (var limbPtr = 0; limbPtr < _resultZrs.Length; limbPtr++)
				//{
				//	resultZRs[limbPtr] = Avx2.BlendVariable(zRs[limbPtr].AsInt32(), resultZRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
				//	resultZIs[limbPtr] = Avx2.BlendVariable(zIs[limbPtr].AsInt32(), resultZIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
				//}
			}

			//var justNowDone = Avx2.CompareEqual(prevDoneFlagsV, doneFlagsV);
			//var anyJustNowDone = !Avx.TestC(justNowDone, Vector256<int>.AllBitsSet);

			//if (anyJustNowDone)
			//{

			//	Debug.Assert(Avx2.MoveMask(prevDoneFlagsV.AsByte()) != compositeIsDone, "Test C -- not the same.");


			//	// Save the current count 
			//	resultCountsV = Avx2.BlendVariable(countsV, resultCountsV, justNowDone); // use First if Zero, second if 1

			//	// Save the current ZValues.
			//	for (var limbPtr = 0; limbPtr < _resultZrs.Length; limbPtr++)
			//	{
			//		resultZRs[limbPtr] = Avx2.BlendVariable(zRs[limbPtr].AsInt32(), resultZRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
			//		resultZIs[limbPtr] = Avx2.BlendVariable(zIs[limbPtr].AsInt32(), resultZIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
			//	}
			//}
			//else
			//{
			//	Debug.Assert(Avx2.MoveMask(prevDoneFlagsV.AsByte()) == compositeIsDone, "Test C -- not the same2.");
			//}
			return compositeIsDone;
		}


		private unsafe VecBuffer GetVecBuffer(Vector256<uint>[] limbSet)
		{
			var destBack = MemoryMarshal.Cast<Vector256<uint>, byte>(limbSet);
			var tempArray = destBack.ToArray();

			var result = new VecBuffer(tempArray);
			return result;
		}

		private unsafe VecBuffer GetEmptyVecBuffer(int byteCount)
		{
			var tempArray = new byte[byteCount];
			var result = new VecBuffer(tempArray);
			return result;
		}


		//// horizontal_and. Returns true if all bits are 1
		//static inline bool horizontal_and(Vec256b const a) {
		//return _mm256_testc_si256(a, _mm256_set1_epi32(-1)) != 0;

		#endregion

		#region Update Rows

		private bool UpdateMapSectionRows(IteratorUPointers iterator, IterationStateUPointers iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			allRowsHaveEscaped = false;

			if (ct.IsCancellationRequested)
			{
				return false;
			}

			allRowsHaveEscaped = true;

			var rowNumber = iterationState.GetNextRowNumber();
			while (rowNumber != null)
			{
				var allRowSamplesHaveEscaped = true;

				for (var idxPtr = 0; idxPtr < iterationState.InPlayList.Length; idxPtr++)
				{
					var idx = iterationState.InPlayList[idxPtr];
					var allSamplesHaveEscaped = UpdateMapCol(idx, iterator, ref iterationState);

					if (!allSamplesHaveEscaped)
					{
						allRowSamplesHaveEscaped = false;
					}
				}

				iterationState.RowHasEscaped[rowNumber.Value] = allRowSamplesHaveEscaped;

				if (!allRowSamplesHaveEscaped)
				{
					allRowsHaveEscaped = false;
				}

				if (ct.IsCancellationRequested)
				{
					allRowsHaveEscaped = false;
					return false;
				}

				rowNumber = iterationState.GetNextRowNumber();
			}

			return true;
		}

		private bool UpdateMapCol(int idx, IteratorUPointers iterator, ref IterationStateUPointers iterationState)
		{
			throw new NotImplementedException();
			//var hasEscapedFlagsV = iterationState.HasEscapedFlagsRowV[idx];
			//var doneFlagsV = iterationState.DoneFlags[idx];

			//var countsV = iterationState.CountsRowV[idx];
			//var resultCountsV = countsV;

			//iterationState.FillCrLimbSet(idx, _crs);
			//_cis = iterationState.CiLimbSet;

			//iterationState.FillZrLimbSet(idx, _zrs);
			//iterationState.FillZiLimbSet(idx, _zis);

			//Array.Copy(_zrs, _resultZrs, _resultZrs.Length);
			//Array.Copy(_zis, _resultZis, _resultZis.Length);

			//Vector256<int> escapedFlagsVec = Vector256<int>.Zero;

			//iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
			//var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			//while (compositeIsDone != -1)
			//{
			//	iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
			//	compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);
			//}

			//TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV, iterationState.UsedCalcs, iterationState.UnusedCalcs);

			//iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			//iterationState.CountsRowV[idx] = resultCountsV;

			//iterationState.UpdateZrLimbSet(idx, _resultZrs);
			//iterationState.UpdateZiLimbSet(idx, _resultZis);

			//var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			//return compositeAllEscaped == -1;
		}

		#endregion

		#region Support Methods

		private (int prevLimbCount, int newLimbCount) GetMathAndAllocateTempVars(MapSectionRequest mapSectionRequest)
		{
			if (mapSectionRequest.MapSectionZVectors == null)
			{
				throw new ArgumentNullException("The MapSectionZVectors is null.");
			}

			var currentBlockSize = _samplePointBuilder.BlockSize;
			var blockSizeForThisRequest = mapSectionRequest.BlockSize;

			if (currentBlockSize != blockSizeForThisRequest)
			{
				_samplePointBuilder.Dispose();
				_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSizeForThisRequest));
			}

			var currentLimbCount = _fp31VecMathUPointers.LimbCount;
			var limbCountForThisRequest = mapSectionRequest.MapSectionZVectors.LimbCount;

			if (currentLimbCount != limbCountForThisRequest)
			{
				//_fp31VecMath = _samplePointBuilder.GetVecMath(limbCountForThisRequest);

				_fp31VecMathUPointers = new FP31VecMathUPointers(new ApFixedPointFormat(limbCountForThisRequest));
				_iterator = new IteratorUPointers(_fp31VecMathUPointers);

				_crs = new Vector256<uint>[limbCountForThisRequest];
				_cis = new Vector256<uint>[limbCountForThisRequest];
				_zrs = new Vector256<uint>[limbCountForThisRequest];
				_zis = new Vector256<uint>[limbCountForThisRequest];
				_resultZrs = new Vector256<uint>[limbCountForThisRequest];
				_resultZis = new Vector256<uint>[limbCountForThisRequest];
			}

			return (currentLimbCount, limbCountForThisRequest);
		}

		private IteratorCoords GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var blockPos = mapSectionRequest.BlockPosition;
			var mapPosition = mapSectionRequest.MapPosition;
			var samplePointDelta = mapSectionRequest.SamplePointDelta;

			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			var screenPos = mapSectionRequest.ScreenPosition;

			return new IteratorCoords(blockPos, screenPos, startingCx, startingCy, delta);
		}

		private (MapSectionVectors, MapSectionZVectors) GetMapSectionVectors(MapSectionRequest mapSectionRequest)
		{
			var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
			if (msv == null) throw new ArgumentException("The MapSectionVectors is null.");
			if (mszv == null) throw new ArgumentException("The MapSetionZVectors is null.");

			return (msv, mszv);
		}

		private void ReportLimbCountUpdate(IteratorCoords coords, int currentLimbCount, int limbCountForThisRequest, int precision)
		{
			if (currentLimbCount != limbCountForThisRequest && coords.ScreenPos.IsZero())
			{
				Debug.WriteLine($"Changing Limbcount from {currentLimbCount} to {limbCountForThisRequest}. Precision: {precision}. " +
					$"\nCx: {coords.StartingCx.ToStringDec()}; Cy: {coords.StartingCy.ToStringDec()}." +
					$"\nCx: {coords.StartingCx}; Cy: {coords.StartingCy}" +
					$"\nCx: {coords.GetStartingCxStringVal()}; Cy: {coords.GetStartingCyStringVal()}.");
			}
		}

		private void ReportCoords(IteratorCoords coords, int limbCount, int precision)
		{
			//var s1 = coords.GetStartingCxStringVal();
			//var s2 = coords.GetStartingCyStringVal();
			var s3 = coords.GetDeltaStringVal();

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. Limbs: {apFixedPointFormat.LimbCount}.");

			Debug.WriteLine($"Starting : {coords.ScreenPos}: {coords.BlockPos}, delta: {s3}, #oflimbs: {limbCount}. MapSecReq Precision: {precision}.");
		}

		private void ReportSamplePoints(IteratorCoords coords, FP31Val[] samplePointOffsets, FP31Val[] samplePointsX, FP31Val[] samplePointsY)
		{
			var bx = coords.ScreenPos.X;
			var by = coords.ScreenPos.Y;
			if (bx == 0 && by == 0 || bx == 3 && by == 4)
			{
				ReportSamplePoints(samplePointOffsets);
				ReportSamplePoints(samplePointsX);
			}
		}

		private void ReportSamplePoints(FP31Val[] fP31Vals)
		{
			foreach (var value in fP31Vals)
			{
				Debug.WriteLine($"{FP31ValHelper.GetDiagDisplay("x", value.Mantissa)} {value.Exponent}.");
			}
		}

		private void ReportResults(IteratorCoords coords, MapSectionRequest request, MapSectionResponse result, CancellationToken ct)
		{
			var s1 = coords.GetStartingCxStringVal();
			var s2 = coords.GetStartingCyStringVal();

			Debug.WriteLine($"{s1}, {s2}: {request.MathOpCounts}");

			if (ct.IsCancellationRequested)
			{
				Debug.WriteLine($"The block: {coords.ScreenPos} is cancelled.");
			}
			else
			{
				if (result.AllRowsHaveEscaped)
				{
					Debug.WriteLine($"The entire block: {coords.ScreenPos} is done.");
				}
			}
		}

		private bool ShouldSkipThisSection(bool skipPositiveBlocks, bool skipLowDetailBlocks, IteratorCoords coords)
		{

			//// Skip positive 'blocks'

			//if (skipPositiveBlocks)
			//{
			//	var xSign = coords.StartingCx.GetSign();
			//	var ySign = coords.StartingCy.GetSign();

			//	return xSign && ySign;
			//}

			//// Move directly to a block where at least one sample point reaches the iteration target.
			//else if (skipLowDetailBlocks && (BigInteger.Abs(coords.ScreenPos.Y) > 1 || BigInteger.Abs(coords.ScreenPos.X) > 3))
			//{
			//	return true;
			//}

			if (coords.ScreenPos.X == 1 && coords.ScreenPos.Y == 1)
			{
				return false;
			}


			return true;
		}

		[Conditional("PERF")]
		private void TallyUsedAndUnusedCalcs(int idx, Vector256<int> originalCountsV, Vector256<int> newCountsV, Vector256<int> resultCountsV, Vector256<int>[] usedCalcs, Vector256<int>[] unusedCalcs)
		{
			usedCalcs[idx] = Avx2.Subtract(resultCountsV, originalCountsV);
			unusedCalcs[idx] = Avx2.Subtract(newCountsV, resultCountsV);
		}

		[Conditional("PERF")]
		private void UpdateRequestWithMops(MapSectionRequest mapSectionRequest, IteratorUPointers iterator, IterationStateUPointers iterationState)
		{
			mapSectionRequest.MathOpCounts = iterator.MathOpCounts.Clone();
			mapSectionRequest.MathOpCounts.RollUpNumberOfCalcs(iterationState.RowUsedCalcs, iterationState.RowUnusedCalcs);
		}

		private void ClearLimbSet(Vector256<uint>[] limbSet)
		{
			// Clear instead of copying form source
			for (var i = 0; i < limbSet.Length; i++)
			{
				limbSet[i] = Avx2.Xor(limbSet[i], limbSet[i]);
			}
		}

		#endregion
	}
}
