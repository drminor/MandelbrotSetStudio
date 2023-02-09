using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorDepthFirst : IMapSectionGenerator
	{
		#region Private Properties

		private readonly SamplePointCache _samplePointCache;

		private FP31VecMath _fp31VecMath;
		private IteratorDepthFirst _iterator;

		private Vector256<uint>[] _crs;
		private Vector256<uint>[] _cis;
		private Vector256<uint>[] _zrs;
		private Vector256<uint>[] _zis;

		private readonly Vector256<int> _justOne;
		private readonly Vector256<int> ALL_BITS_SET;

		#endregion

		#region Constructor

		public MapSectionGeneratorDepthFirst(SamplePointCache samplePointCache, int limbCount)
		{
			_samplePointCache = samplePointCache;

			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			_fp31VecMath = new FP31VecMath(apFixedPointFormat);
			_iterator = new IteratorDepthFirst(_fp31VecMath);

			_crs = _fp31VecMath.GetNewLimbSet();
			_cis = _fp31VecMath.GetNewLimbSet();
			_zrs = _fp31VecMath.GetNewLimbSet();
			_zis = _fp31VecMath.GetNewLimbSet();

			ALL_BITS_SET = Vector256<int>.AllBitsSet;
			_justOne = Vector256.Create(1);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var coords = GetCoordinates(mapSectionRequest, _fp31VecMath.ApFixedPointFormat);

			if (ShouldSkipThisSection(skipPositiveBlocks: false, skipLowDetailBlocks: false, coords))
			{
				return new MapSectionResponse(mapSectionRequest);
			}

			var stopwatch = Stopwatch.StartNew();
			//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);

			var stride = (byte)mapSectionRequest.BlockSize.Width;
			var fP31ScalarMath = new FP31ScalarMath(_fp31VecMath.ApFixedPointFormat);

			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(coords.Delta, stride, fP31ScalarMath);
			//var samplePointOffsets = _samplePointCache.GetSamplePointOffsets(coords.Delta);

			var samplePointsX = SamplePointBuilder.BuildSamplePoints(coords.StartingCx, samplePointOffsets, fP31ScalarMath);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(coords.StartingCy, samplePointOffsets, fP31ScalarMath);
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
			var countsV = iterationState.CountsRowV[idx];

			var doneFlagsV = iterationState.DoneFlags[idx];

			iterationState.FillCrLimbSet(idx, _crs);
			iterationState.FillCiLimbSet(idx, _cis);
			iterationState.FillZrLimbSet(idx, _zrs);
			iterationState.FillZiLimbSet(idx, _zis);

			var allDone = false;

			iterator.Reset();
			while (!allDone)
			{
				var escapedFlagsVec = iterator.Iterate(_crs, _cis, _zrs, _zis);

				TallyUsedAndUnusedCalcs(idx, iterationState.DoneFlags, ref iterationState);

				// Increment all counts
				var countsVt = Avx2.Add(countsV, _justOne);

				// Take the incremented count, only if the doneFlags is false for each vector position.
				countsV = Avx2.BlendVariable(countsVt, countsV, doneFlagsV); // use First if Zero, second if 1

				// Apply the new escapedFlags, only if the doneFlags is false for each vector position
				hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec, hasEscapedFlagsV, doneFlagsV);

				// Compare the new Counts with the TargetIterations
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec);
				doneFlagsV = Avx2.BlendVariable(doneFlagsV, ALL_BITS_SET, escapedOrReachedVec);

				var compositeIsDone = Avx2.MoveMask(doneFlagsV.AsByte());
				allDone = compositeIsDone == -1;
			}

			iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			iterationState.CountsRowV[idx] = countsV;

			iterationState.DoneFlags[idx] = doneFlagsV;

			iterationState.UpdateZrLimbSet(idx, _zrs);
			iterationState.UpdateZrLimbSet(idx, _zis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			var result = compositeAllEscaped == -1;

			return result;
		}

		[Conditional("PERF")]
		private void TallyUsedAndUnusedCalcs(int idx, Vector256<int>[] doneFlags, ref IterationStateDepthFirst iterationState)
		{
			iterationState.Calcs[idx]++;

			// Increment all unused calculations
			var unusedCalcsVt = Avx2.Add(iterationState.UnusedCalcs[idx], _justOne);

			// Take the incremented unusedCalc, only if the doneFlags is true for each vector position.
			iterationState.UnusedCalcs[idx] = Avx2.BlendVariable(iterationState.UnusedCalcs[idx], unusedCalcsVt, doneFlags[idx]); // use First if Zero, second if 1
		}

		#endregion

		#region Support Methods

		private IteratorCoords GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var blockPos = mapSectionRequest.BlockPosition;
			var mapPosition = mapSectionRequest.Position;
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

			var mapSectionZVectors = mapSectionRequest.MapSectionZVectors ?? throw new ArgumentNullException("The MapSectionVectors is null.");
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
			var s1 = coords.StartingCx.GetStringValue();
			var s2 = coords.StartingCy.GetStringValue();
			var s3 = coords.Delta.GetStringValue();

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
			var s1 = coords.StartingCx.GetStringValue();
			var s2 = coords.StartingCy.GetStringValue();

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
