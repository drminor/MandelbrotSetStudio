using MSS.Types.APValues;
using MSS.Types;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorDepthFirst : IMapSectionGenerator
	{
		#region Private Properties

		private SamplePointBuilder _samplePointBuilder;

		private FP31VecMath _fp31VecMath;
		private IteratorDepthFirst _iterator;

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
			_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSize));

			//var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			//_fp31VecMath = new FP31VecMath(apFixedPointFormat);
			_fp31VecMath = _samplePointBuilder.GetVecMath(limbCount);
			_iterator = new IteratorDepthFirst(_fp31VecMath);

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
				//var apFixedPointFormat = new ApFixedPointFormat(limbCountForThisRequest);
				//_fp31VecMath = new FP31VecMath(apFixedPointFormat);
				_fp31VecMath = _samplePointBuilder.GetVecMath(limbCountForThisRequest);
				_iterator = new IteratorDepthFirst(_fp31VecMath);

				_crs = _fp31VecMath.GetNewLimbSet();
				_cis = _fp31VecMath.GetNewLimbSet();
				_zrs = _fp31VecMath.GetNewLimbSet();
				_zis = _fp31VecMath.GetNewLimbSet();

				_resultZrs = _fp31VecMath.GetNewLimbSet();
				_resultZis = _fp31VecMath.GetNewLimbSet();
			}

			var coords = GetCoordinates(mapSectionRequest, _fp31VecMath.ApFixedPointFormat);

			if (currentLimbCount != limbCountForThisRequest)
			{
				if (coords.ScreenPos.IsZero())
				{
					Debug.WriteLine($"Changing Limbcount from {currentLimbCount} to {limbCountForThisRequest}. Precision: {mapSectionRequest.Precision}. " +
						$"\nCx: {coords.StartingCx.ToStringDec()}; Cy: {coords.StartingCy.ToStringDec()}." +
						$"\nCx: {coords.StartingCx}; Cy: {coords.StartingCy}" +
						$"\nCx: {coords.GetStartingCxStringVal()}; Cy: {coords.GetStartingCyStringVal()}.");
				}
			}

			if (ShouldSkipThisSection(skipPositiveBlocks: false, skipLowDetailBlocks: false, coords))
			{
				return new MapSectionResponse(mapSectionRequest);
			}

			var stopwatch = Stopwatch.StartNew();
			//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);

			////var stride = (byte)mapSectionRequest.BlockSize.Width;
			//var fP31ScalarMath = new FP31ScalarMath(_fp31VecMath.ApFixedPointFormat);
			////var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(coords.Delta, stride, fP31ScalarMath);
			//var samplePointOffsets = _samplePointCache.GetSamplePointOffsets(coords.Delta);
			//var samplePointsX = SamplePointBuilder.BuildSamplePoints(coords.StartingCx, samplePointOffsets, fP31ScalarMath);
			//var samplePointsY = SamplePointBuilder.BuildSamplePoints(coords.StartingCy, samplePointOffsets, fP31ScalarMath);

			var (samplePointsX, samplePointsY) = _samplePointBuilder.BuildSamplePoints(coords);

			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest);

			var mapCalcSettings = mapSectionRequest.MapCalcSettings;
			_iterator.Threshold = (uint)mapCalcSettings.Threshold;
			_iterator.IncreasingIterations = mapSectionRequest.IncreasingIterations;
			_iterator.MathOpCounts.Reset();
			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);

			var iterationState = new IterationStateDepthFirst(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, targetIterationsVector);

			var allRowsHaveEscaped = GenerateMapSectionRows(_iterator, iterationState, ct);

			stopwatch.Stop();

			var result = new MapSectionResponse(mapSectionRequest, allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors, ct.IsCancellationRequested);
			mapSectionRequest.GenerationDuration = stopwatch.Elapsed;
			UpdateRequestWithMops(mapSectionRequest, _iterator, iterationState);
			//ReportResults(coords, mapSectionRequest, result, ct);

			return result;
		}

		[Conditional("PERF")]
		private void UpdateRequestWithMops(MapSectionRequest mapSectionRequest, IteratorDepthFirst iterator, IterationStateDepthFirst iterationState)
		{
			mapSectionRequest.MathOpCounts = iterator.MathOpCounts.Clone();
			mapSectionRequest.MathOpCounts.RollUpNumberOfCalcs(iterationState.RowUsedCalcs, iterationState.RowUnusedCalcs);
		}

		private bool GenerateMapSectionRows(IteratorDepthFirst iterator, IterationStateDepthFirst iterationState, CancellationToken ct)
		{
			var allRowsHaveEscaped = true;

			var rowNumber = iterationState.GetNextRowNumber();
			while(rowNumber != null && !ct.IsCancellationRequested)
			{ 
				var allRowSamplesHaveEscaped = true;

				for (var idxPtr = 0; idxPtr < iterationState.InPlayList.Length && !ct.IsCancellationRequested; idxPtr++)
				{
					var idx = iterationState.InPlayList[idxPtr];
					var allSamplesHaveEscaped = GenerateMapCol(idx, iterator, ref iterationState);

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

				rowNumber = iterationState.GetNextRowNumber();
			}

			return allRowsHaveEscaped;
		}

		#endregion

		#region Generate One Vector

		private bool GenerateMapCol(int idx, IteratorDepthFirst iterator, ref IterationStateDepthFirst iterationState)
		{
			var hasEscapedFlagsV = iterationState.HasEscapedFlagsRowV[idx];
			var doneFlagsV = iterationState.DoneFlags[idx];

			var countsV = iterationState.CountsRowV[idx];
			var resultCountsV = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			iterationState.FillCiLimbSet(idx, _cis);
			iterationState.FillZrLimbSet(idx, _zrs);
			iterationState.FillZiLimbSet(idx, _zis);

			Array.Copy(_zrs, _resultZrs, _resultZrs.Length);
			Array.Copy(_zis, _resultZis, _resultZis.Length);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;

			iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
			var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			while (compositeIsDone != -1)
			{
				iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec);
				compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);
			}

			TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV, ref iterationState);

			iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			iterationState.CountsRowV[idx] = resultCountsV;

			iterationState.UpdateZrLimbSet(idx, _resultZrs);
			iterationState.UpdateZiLimbSet(idx, _resultZis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			return compositeAllEscaped == -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int UpdateCounts(Vector256<int> escapedFlagsVec, ref Vector256<int> countsV, ref Vector256<int> resultCountsV, ref Vector256<int> hasEscapedFlagsV, ref Vector256<int> doneFlagsV, Vector256<int> targetIterationsV)
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
					_resultZrs[limbPtr] = Avx2.BlendVariable(_zrs[limbPtr].AsInt32(), _resultZrs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
					_resultZis[limbPtr] = Avx2.BlendVariable(_zis[limbPtr].AsInt32(), _resultZis[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
				}
			}

			return compositeIsDone;
		}


		[Conditional("PERF")]
		private void TallyUsedAndUnusedCalcs(int idx, Vector256<int> originalCountsV, Vector256<int> newCountsV, Vector256<int> resultCountsV, ref IterationStateDepthFirst iterationState)
		{
			iterationState.UsedCalcs[idx] = Avx2.Subtract(resultCountsV, originalCountsV);
			iterationState.UnusedCalcs[idx] = Avx2.Subtract(newCountsV, resultCountsV);
		}

		#endregion

		#region Support Methods

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
			var mapSectionVectors = mapSectionRequest.MapSectionVectors ?? throw new ArgumentNullException("The MapSectionVectors is null.");
			mapSectionRequest.MapSectionVectors = null;

			var mapSectionZVectors = mapSectionRequest.MapSectionZVectors ?? throw new ArgumentNullException("The MapSectionZVectors is null.");
			mapSectionRequest.MapSectionZVectors = null;

			return (mapSectionVectors, mapSectionZVectors);
		}

		private bool[] CompressHasEscapedFlags(int[] hasEscapedFlags)
		{
			bool[] result;

			if (!hasEscapedFlags.Any(x => !(x == 0)))
			{
				// All have escaped
				result = new bool[] { true };
			}
			else if (!hasEscapedFlags.Any(x => x > 0))
			{
				// none have escaped
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = hasEscapedFlags.Select(x => x == 0 ? false : true).ToArray();
			}

			return result;
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
			// Skip positive 'blocks'

			if (skipPositiveBlocks)
			{
				var xSign = coords.StartingCx.GetSign();
				var ySign = coords.StartingCy.GetSign();

				return xSign && ySign;
			}

			// Move directly to a block where at least one sample point reaches the iteration target.
			else if (skipLowDetailBlocks && (BigInteger.Abs(coords.ScreenPos.Y) > 1 || BigInteger.Abs(coords.ScreenPos.X) > 3))
			{
				return true;
			}

			return false;
		}

		#endregion
	}
}
