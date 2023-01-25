using MEngineDataContracts;
using MSS.Common;
using MSS.Common.APValues;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using MSS.Types.MSet;

namespace MSetGeneratorPrototype
{
	public class MapSectionGenerator : IMapSectionGenerator
	{
		private readonly FP31VectorsMath _fp31VectorsMath;
		private readonly IteratorSimd _iterator;

		#region Constructor

		public MapSectionGenerator(SizeInt blockSize, int limbCount)
		{
			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			_fp31VectorsMath = new FP31VectorsMath(apFixedPointFormat, blockSize.Width);
			_iterator = new IteratorSimd(_fp31VectorsMath);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var skipPositiveBlocks = false;
			var skipLowDetailBlocks = false;

			var coords = GetCoordinates(mapSectionRequest, _fp31VectorsMath.ApFixedPointFormat);

			MapSectionResponse result;

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, coords))
			{
				result = new MapSectionResponse(mapSectionRequest);
			}
			else
			{
				var mapCalcSettings = mapSectionRequest.MapCalcSettings;

				var mapSectionVectors = mapSectionRequest.MapSectionVectors;
				mapSectionRequest.MapSectionVectors = null;

				//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);
				GenerateMapSection(_iterator, mapSectionVectors, coords, mapCalcSettings);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

				result = new MapSectionResponse(mapSectionRequest, mapSectionVectors, zValues: null);
				//result.MathOpCounts = _iterator.MathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private void GenerateMapSection(IteratorSimd iterator, MapSectionVectors mapSectionVectors, IteratorCoords coords, MapCalcSettings mapCalcSettings)
		{
			var blockSize = mapSectionVectors.BlockSize;
			var rowCount = blockSize.Height;
			var stride = (byte)blockSize.Width;

			var scalarMath = new FP31ScalarMath(_fp31VectorsMath.ApFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(coords.Delta, stride, scalarMath);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(coords.StartingCx, samplePointOffsets, scalarMath);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(coords.StartingCy, samplePointOffsets, scalarMath);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			iterator.Threshold = (uint)mapCalcSettings.Threshold;
			iterator.Crs.UpdateFrom(samplePointsX);
			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);

			var iterationCountsRow = new IterationCountsRow(mapSectionVectors);

			for (int rowNumber = 0; rowNumber < rowCount; rowNumber++)
			{
				iterationCountsRow.SetRowNumber(rowNumber);

				// Load C & Z value decks
				var yPoint = samplePointsY[rowNumber];
				iterator.Cis.UpdateFrom(yPoint);

				//var (zRs, zIs) = GetZValues(mapSectionRequest, rowNumber, apFixedPointFormat.LimbCount, stride);
				iterator.Zrs.ClearManatissMems();
				iterator.Zis.ClearManatissMems();
				iterator.ZValuesAreZero = true;

				GenerateMapRow(iterator, ref iterationCountsRow, targetIterationsVector);
			}
		}

		#endregion

		#region Generate Map Row

		private void GenerateMapRow(IteratorSimd iterator, ref IterationCountsRow itState, Vector256<int> targetIterationsVector)
		{
			while (itState.InPlayList.Length > 0)
			{
				var escapedFlags = iterator.Iterate(itState.InPlayList, itState.InPlayListNarrow);

				var vectorsNoLongerInPlay = UpdateCounts(escapedFlags, ref itState, targetIterationsVector);
				if (vectorsNoLongerInPlay.Count > 0)
				{
					itState.UpdateTheInPlayList(vectorsNoLongerInPlay);
				}
			}

			//_iterator.MathOpCounts.RollUpNumberOfUnusedCalcs(itState.GetUnusedCalcs());
		}

		private List<int> UpdateCounts(Vector256<int>[] escapedFlagVectors, ref IterationCountsRow itState, Vector256<int> targetIterationsVector)
		{
			var toBeRemoved = new List<int>();
			var justOne = Vector256.Create(1);

			for (var idxPtr = 0; idxPtr < itState.InPlayList.Length; idxPtr++)
			{
				var idx = itState.InPlayList[idxPtr];

				var doneFlagsV = itState.DoneFlags[idx];
				var countsV = itState.Counts[idx];

				// Increment all counts
				var countsVt = Avx2.Add(countsV, justOne);

				// Take the incremented count, only if the doneFlags is false for each vector position.
				countsV = Avx2.BlendVariable(countsVt.AsByte(), countsV.AsByte(), doneFlagsV.AsByte()).AsInt32(); // use First if Zero, second if 1
				itState.Counts[idx] = countsV;

				var unusedCalcsV = itState.UnusedCalcs[idx];

				// Increment all unused calculations
				var unusedCalcsVt = Avx2.Add(unusedCalcsV, justOne);

				// Take the incremented unusedCalc, only if the doneFlags is true for each vector position.
				itState.UnusedCalcs[idx] = Avx2.BlendVariable(unusedCalcsV.AsByte(), unusedCalcsVt.AsByte(), doneFlagsV.AsByte()).AsInt32();

				var hasEscapedFlagsV = itState.HasEscapedFlags[idx];

				// Apply the new escapeFlags, only if the doneFlags is false for each vector position
				var updatedHaveEscapedFlagsV = Avx2.BlendVariable(escapedFlagVectors[idx].AsByte(), hasEscapedFlagsV.AsByte(), doneFlagsV.AsByte()).AsInt32();
				itState.HasEscapedFlags[idx] = updatedHaveEscapedFlagsV;

				// Compare the new Counts with the TargetIterations
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsVector);

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(updatedHaveEscapedFlagsV, targetReachedCompVec);
				var updatedDoneFlagsV = Avx2.BlendVariable(doneFlagsV.AsByte(), Vector256<int>.AllBitsSet.AsByte(), escapedOrReachedVec.AsByte()).AsInt32();

				itState.DoneFlags[idx] = updatedDoneFlagsV;

				var compositeIsDone = Avx2.MoveMask(updatedDoneFlagsV.AsByte());

				if (compositeIsDone == -1)
				{
					toBeRemoved.Add(idx);
				}
			}

			return toBeRemoved;
		}

		#endregion

		#region Support Methods

		private IteratorCoords GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var dtoMapper = new DtoMapper();

			var blockPos = dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var mapPosition = dtoMapper.MapFrom(mapSectionRequest.Position);
			var samplePointDelta = dtoMapper.MapFrom(mapSectionRequest.SamplePointDelta);

			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			var screenPos = mapSectionRequest.ScreenPosition;

			return new IteratorCoords(blockPos, screenPos, startingCx, startingCy, delta);
		}

		private (FP31Vectors zRs, FP31Vectors zIs) GetZValues(MapSectionRequest mapSectionRequest, int rowNumber, int limbCount, int valueCount)
		{
			var zRs = new FP31Vectors(limbCount, valueCount);
			var zIs = new FP31Vectors(limbCount, valueCount);

			return (zRs, zIs);
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
				result = hasEscapedFlags.Select(x => x == 0 ? false: true).ToArray();
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
