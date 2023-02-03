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

		private readonly FP31VecMath _fp31VecMath;
		private readonly int _limbCount;
		private readonly IteratorSimdDepthFirst _iterator;


		private readonly Vector256<int> _justOne;
		private readonly Vector256<byte> ALL_BITS_SET;

		#endregion

		#region Constructor

		public MapSectionGeneratorDepthFirst(SizeInt blockSize, int limbCount)
		{
			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			_fp31VecMath = new FP31VecMath(apFixedPointFormat);
			_limbCount = limbCount;
			_iterator = new IteratorSimdDepthFirst(_fp31VecMath, blockSize.Width);

			ALL_BITS_SET = Vector256<byte>.AllBitsSet;

			_justOne = Vector256.Create(1);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var skipPositiveBlocks = false;
			var skipLowDetailBlocks = false;

			var coords = GetCoordinates(mapSectionRequest, _fp31VecMath.ApFixedPointFormat);

			MapSectionResponse result;

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, coords))
			{
				result = new MapSectionResponse(mapSectionRequest);
			}
			else
			{
				var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest, _limbCount);

				var mapCalcSettings = mapSectionRequest.MapCalcSettings;
				_iterator.Threshold = (uint)mapCalcSettings.Threshold;
				var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);
				var iterationState = new IterationStateDepthFirst(mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, targetIterationsVector);

				//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);
				var allRowsHaveEscaped = GenerateMapSection(_iterator, iterationState, coords);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

				if (allRowsHaveEscaped)
				{
					Debug.WriteLine($"The entire block: {coords.ScreenPos} is done.");
				}

				result = new MapSectionResponse(mapSectionRequest, allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors);

				//result.MathOpCounts = _iterator.MathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private bool GenerateMapSection(IteratorSimdDepthFirst iterator, IterationStateDepthFirst iterationState, IteratorCoords coords)
		{
			var stride = (byte)iterationState.BlockSize.Width;
			var scalarMath = new FP31ScalarMath(_fp31VecMath.ApFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(coords.Delta, stride, scalarMath);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(coords.StartingCx, samplePointOffsets, scalarMath);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(coords.StartingCy, samplePointOffsets, scalarMath);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			iterationState.CrsRow.UpdateFrom(samplePointsX);
			var allRowsHaveEscaped = true;
			var rowNumber = iterationState.GetNextRowNumber();

			while(rowNumber != null)
			{ 
				// Load C & Z value decks
				var yPoint = samplePointsY[rowNumber.Value];
				iterationState.CisRow.UpdateFrom(yPoint);

				var allRowSamplesHaveEscaped = true;

				for (var idxPtr = 0; idxPtr < iterationState.InPlayList.Length; idxPtr++)
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

				//_iterator.MathOpCounts.RollUpNumberOfUnusedCalcs(itState.GetUnusedCalcs());

				rowNumber = iterationState.GetNextRowNumber();
			}

			return allRowsHaveEscaped;
		}

		#endregion

		#region Generate One Vector

		private bool GenerateMapCol(int idx, IteratorSimdDepthFirst iterator, ref IterationStateDepthFirst iterationState)
		{
			var crs = new Vector256<uint>[_limbCount];
			var cis = new Vector256<uint>[_limbCount];
			var zrs = new Vector256<uint>[_limbCount];
			var zis = new Vector256<uint>[_limbCount];

			iterationState.CrsRow.FillLimbSet(idx, crs);
			iterationState.CisRow.FillLimbSet(idx, cis);

			FillLimbSet(iterationState.ZrsRow, idx, zrs);
			FillLimbSet(iterationState.ZisRow, idx, zis);

			var hasEscapedFlagsV = iterationState.HasEscapedFlagsRow[idx].AsByte();
			var countsV = iterationState.CountsRow[idx];

			var doneFlagsV = iterationState.DoneFlags[idx].AsByte();
			var unusedCalcsV = iterationState.UnusedCalcs[idx];

			var zValuesAreZero = !iterationState.UpdatingIterationsCount;
			var allDone = false;

			while (!allDone)
			{
				var escapedFlagsVec = iterator.Iterate(crs, cis, zrs, zis, zValuesAreZero);
				zValuesAreZero = false;

				// Increment all counts
				var countsVt = Avx2.Add(countsV, _justOne).AsByte();

				// Take the incremented count, only if the doneFlags is false for each vector position.
				countsV = Avx2.BlendVariable(countsVt, countsV.AsByte(), doneFlagsV).AsInt32(); // use First if Zero, second if 1

				// Increment all unused calculations
				var unusedCalcsVt = Avx2.Add(unusedCalcsV, _justOne).AsByte();

				// Take the incremented unusedCalc, only if the doneFlags is true for each vector position.
				unusedCalcsV = Avx2.BlendVariable(unusedCalcsV.AsByte(), unusedCalcsVt, doneFlagsV).AsInt32();

				// Apply the new escapeFlags, only if the doneFlags is false for each vector position
				hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec.AsByte(), hasEscapedFlagsV, doneFlagsV);

				// Compare the new Counts with the TargetIterations
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector).AsByte();

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec).AsByte();
				doneFlagsV = Avx2.BlendVariable(doneFlagsV, ALL_BITS_SET, escapedOrReachedVec);

				var compositeIsDone = Avx2.MoveMask(doneFlagsV);
				allDone = compositeIsDone == -1;
			}

			iterationState.HasEscapedFlagsRow[idx] = hasEscapedFlagsV.AsInt32();
			iterationState.CountsRow[idx] = countsV;

			iterationState.DoneFlags[idx] = doneFlagsV.AsInt32();
			iterationState.UnusedCalcs[idx] = unusedCalcsV;

			UpdateFromLimbSet(iterationState.ZrsRow, idx, zrs);
			UpdateFromLimbSet(iterationState.ZisRow, idx, zis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV);

			var result = compositeAllEscaped == -1;

			return result;

		}

		private void FillLimbSet(Span<Vector256<uint>> source, int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * _limbCount;

			for (var i = 0; i < _limbCount; i++)
			{
				limbSet[i] = source[vecPtr++];
			}
		}

		public void UpdateFromLimbSet(Span<Vector256<uint>> destination, int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * _limbCount;

			for (var i = 0; i < _limbCount; i++)
			{
				destination[vecPtr++] = limbSet[i];
			}
		}

		#endregion

		#region Support Methods

		private IteratorCoords GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			//var blockPos = dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			//var mapPosition = dtoMapper.MapFrom(mapSectionRequest.Position);
			//var samplePointDelta = dtoMapper.MapFrom(mapSectionRequest.SamplePointDelta);

			var blockPos = mapSectionRequest.BlockPosition;
			var mapPosition = mapSectionRequest.Position;
			var samplePointDelta = mapSectionRequest.SamplePointDelta;


			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			var screenPos = mapSectionRequest.ScreenPosition;

			return new IteratorCoords(blockPos, screenPos, startingCx, startingCy, delta);
		}

		private (MapSectionVectors, MapSectionZVectors) GetMapSectionVectors(MapSectionRequest mapSectionRequest, int limbCount)
		{
			var mapSectionVectors = mapSectionRequest.MapSectionVectors ?? throw new ArgumentNullException("The MapSectionVectors is null.");
			mapSectionRequest.MapSectionVectors = null;

			//if (mapSectionRequest.IncreasingIterations && mapSectionRequest.MapSectionZVectors == null) 
			//{
			//	throw new ArgumentNullException("The MapSectionZVectors is null.");
			//}

			var mapSectionZVectors = mapSectionRequest.MapSectionZVectors ?? throw new ArgumentNullException("The MapSectionVectors is null."); //new MapSectionZVectors(mapSectionRequest.BlockSize, limbCount);
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
