using MSetRowGeneratorClient;
using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorDepthFirst : IMapSectionGenerator
	{
		private readonly bool USE_C_IMPLEMENTATION;

		#region Private Properties

		private SamplePointBuilder _samplePointBuilder;

		private FP31VecMath _fp31VecMath;
		private IteratorDepthFirst _iterator;
		//private MSetGeneratorLib.MapSectionGenerator _mapSectionGenerator;

		private Vector256<uint>[] _crs;
		private Vector256<uint>[] _cis;
		private Vector256<uint>[] _zrs;
		private Vector256<uint>[] _zis;

		private Vector256<uint>[] _resultZrs;
		private Vector256<uint>[] _resultZis;

		private readonly Vector256<int> _justOne;

		#endregion

		#region Constructor

		public MapSectionGeneratorDepthFirst(int limbCount, SizeInt blockSize)
		{
			USE_C_IMPLEMENTATION = true;

			_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSize));

			_fp31VecMath = _samplePointBuilder.GetVecMath(limbCount);
			_iterator = new IteratorDepthFirst(_fp31VecMath);
			//_mapSectionGenerator = new MSetGeneratorLib.MapSectionGenerator();

			_crs = _fp31VecMath.GetNewLimbSet();
			_cis = _fp31VecMath.GetNewLimbSet();
			_zrs = _fp31VecMath.GetNewLimbSet();
			_zis = _fp31VecMath.GetNewLimbSet();

			_resultZrs = _fp31VecMath.GetNewLimbSet();
			_resultZis = _fp31VecMath.GetNewLimbSet();

			_justOne = Vector256.Create(1);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var (currentLimbCount, limbCountForThisRequest) = GetMathAndAllocateTempVars(mapSectionRequest);
			var coords = GetCoordinates(mapSectionRequest, _fp31VecMath.ApFixedPointFormat);
			ReportLimbCountUpdate(coords, currentLimbCount, limbCountForThisRequest, mapSectionRequest.Precision);

			var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest);

			if (ShouldSkipThisSection(skipPositiveBlocks: false, skipLowDetailBlocks: false, coords))
			 {
				return new MapSectionResponse(mapSectionRequest, requestCompleted: false, allRowsHaveEscaped: false, mapSectionVectors, mapSectionZVectors);
			}

			var stopwatch = Stopwatch.StartNew();
			//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);

			var (samplePointsX, samplePointsY) = _samplePointBuilder.BuildSamplePoints(coords);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			var mapCalcSettings = mapSectionRequest.MapCalcSettings;
			_iterator.Threshold = (uint)mapCalcSettings.Threshold;
			_iterator.IncreasingIterations = mapSectionRequest.IncreasingIterations;
			_iterator.MathOpCounts.Reset();
			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);

			var iterationState = new IterationStateDepthFirst(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, targetIterationsVector);

			var completed = GeneratorOrUpdateRows(_iterator, iterationState, ct, out var allRowsHaveEscaped);
			stopwatch.Stop();
			var result = new MapSectionResponse(mapSectionRequest, completed, allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors);
			mapSectionRequest.GenerationDuration = stopwatch.Elapsed;
			UpdateRequestWithMops(mapSectionRequest, _iterator, iterationState);
			//ReportResults(coords, mapSectionRequest, result, ct);

			return result;
		}

		private bool GeneratorOrUpdateRows(IteratorDepthFirst iterator, IterationStateDepthFirst iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			bool completed;

			if (_iterator.IncreasingIterations)
			{
				completed = UpdateMapSectionRows(_iterator, iterationState, ct, out allRowsHaveEscaped);
			}
			else
			{
				if (USE_C_IMPLEMENTATION)
				{
					completed = HighPerfGenerateMapSectionRows(_iterator, iterationState, ct, out allRowsHaveEscaped);
				}
				else
				{
					completed = GenerateMapSectionRows(_iterator, iterationState, ct, out allRowsHaveEscaped);
				}
			}

			return completed;
		}

		private bool HighPerfGenerateMapSectionRows(IteratorDepthFirst iterator, IterationStateDepthFirst iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			var mSetRowClient = new HpMSetRowClient();

			allRowsHaveEscaped = false;

			if (ct.IsCancellationRequested)
			{
				return false;
			}

			allRowsHaveEscaped = true;

			for (var rowNumber = 0; rowNumber < iterationState.RowCount; rowNumber++)
			{
				iterationState.SetRowNumber(rowNumber);

				var mapCalcSettings = new MapCalcSettings(iterationState.TargetIterationsVector.GetElement(0), (int) iterator.Threshold, requestsPerJob: 4);

				var allRowSamplesHaveEscaped = mSetRowClient.GenerateMapSectionRow(iterationState, _fp31VecMath.ApFixedPointFormat, mapCalcSettings, ct);
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

		private bool GenerateMapSectionRows(IteratorDepthFirst iterator, IterationStateDepthFirst iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
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
					var allSamplesHaveEscaped = GenerateMapCol(idx, iterator, ref iterationState);

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

		private bool UpdateMapSectionRows(IteratorDepthFirst iterator, IterationStateDepthFirst iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
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

		#endregion

		#region Generate One Vector

		private bool GenerateMapCol(int idx, IteratorDepthFirst iterator, ref IterationStateDepthFirst iterationState)
		{
			var hasEscapedFlagsV = Vector256<int>.Zero;
			var doneFlagsV = Vector256<int>.Zero;

			var countsV = Vector256<int>.Zero;
			var resultCountsV = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			_cis = iterationState.CiLimbSet;

			ClearLimbSet(_zrs);
			ClearLimbSet(_zis);
			ClearLimbSet(_resultZrs);
			ClearLimbSet(_resultZis);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;

			iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
			var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			while (compositeIsDone != -1)
			{
				iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
				compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);
			}

			TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV, iterationState.UsedCalcs, iterationState.UnusedCalcs);

			iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			iterationState.CountsRowV[idx] = resultCountsV;

			iterationState.UpdateZrLimbSet(idx, _resultZrs);
			iterationState.UpdateZiLimbSet(idx, _resultZis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			return compositeAllEscaped == -1;
		}

		private bool UpdateMapCol(int idx, IteratorDepthFirst iterator, ref IterationStateDepthFirst iterationState)
		{
			var hasEscapedFlagsV = iterationState.HasEscapedFlagsRowV[idx];
			var doneFlagsV = iterationState.DoneFlags[idx];

			var countsV = iterationState.CountsRowV[idx];
			var resultCountsV = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			_cis = iterationState.CiLimbSet;

			iterationState.FillZrLimbSet(idx, _zrs);
			iterationState.FillZiLimbSet(idx, _zis);

			Array.Copy(_zrs, _resultZrs, _resultZrs.Length);
			Array.Copy(_zis, _resultZis, _resultZis.Length);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;

			iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
			var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			while (compositeIsDone != -1)
			{
				iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
				compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);
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
			Vector256<uint>[] zRs, Vector256<uint>[] resultZRs, 
			Vector256<uint>[] zIs, Vector256<uint>[] resultZIs,
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

				// Save the current ZValues.
				for (var limbPtr = 0; limbPtr < _resultZrs.Length; limbPtr++)
				{
					resultZRs[limbPtr] = Avx2.BlendVariable(zRs[limbPtr].AsInt32(), resultZRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
					resultZIs[limbPtr] = Avx2.BlendVariable(zIs[limbPtr].AsInt32(), resultZIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
				}
			}

			return compositeIsDone;
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

			var currentLimbCount = _fp31VecMath.LimbCount;
			var limbCountForThisRequest = mapSectionRequest.MapSectionZVectors.LimbCount;

			if (currentLimbCount != limbCountForThisRequest)
			{
				_fp31VecMath = _samplePointBuilder.GetVecMath(limbCountForThisRequest);
				_iterator = new IteratorDepthFirst(_fp31VecMath);

				_crs = _fp31VecMath.GetNewLimbSet();
				_cis = _fp31VecMath.GetNewLimbSet();
				_zrs = _fp31VecMath.GetNewLimbSet();
				_zis = _fp31VecMath.GetNewLimbSet();

				_resultZrs = _fp31VecMath.GetNewLimbSet();
				_resultZis = _fp31VecMath.GetNewLimbSet();
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
		private void UpdateRequestWithMops(MapSectionRequest mapSectionRequest, IteratorDepthFirst iterator, IterationStateDepthFirst iterationState)
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
