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
	public class MapSectionGeneratorSimd
	{
		private readonly VecMath9 _vecMath;
		private readonly IIterator _iterator;

		public MapSectionGeneratorSimd(SizeInt blockSize, int limbCount)
		{
			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			_vecMath = new VecMath9(apFixedPointFormat, blockSize.Width);
			_iterator = new IteratorSimd(_vecMath);
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var skipPositiveBlocks = false;
			var skipLowDetailBlocks = false;

			var precision = mapSectionRequest.Precision;
			var (blockPos, screenPos, startingCx, startingCy, delta) = GetCoordinates(mapSectionRequest, _vecMath.ApFixedPointFormat);

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. Limbs: {apFixedPointFormat.LimbCount}.");

			//Debug.WriteLine($"Starting : {screenPos}: {blockPos}, delta: {s3}, #oflimbs: {_apFixedPointFormat.LimbCount}. MapSecReq Precision: {precision}.");

			MapSectionResponse result;

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, startingCx, startingCy, screenPos))
			{
				result = new MapSectionResponse(mapSectionRequest);
			}
			else
			{
				var mapCalcSettings = mapSectionRequest.MapCalcSettings;

				var mapSectionVectors = mapSectionRequest.MapSectionVectors;
				mapSectionRequest.MapSectionVectors = null;

				GenerateMapSection(_iterator, mapSectionVectors, startingCx, startingCy, delta, mapCalcSettings);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

				result = new MapSectionResponse(mapSectionRequest, mapSectionVectors, zValues: null);
				//result.MathOpCounts = iterator.MathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private void GenerateMapSection(IIterator iterator, MapSectionVectors mapSectionVectors, FP31Val startingCx, FP31Val startingCy, FP31Val delta, MapCalcSettings mapCalcSettings)
		{
			var blockSize = mapSectionVectors.BlockSize;
			var rowCount = blockSize.Height;
			var stride = (byte)blockSize.Width;

			var scalarMath9 = new ScalarMath9(_vecMath.ApFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(delta, stride, scalarMath9);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(startingCx, samplePointOffsets, scalarMath9);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(startingCy, samplePointOffsets, scalarMath9);


			//var bx = mapSectionRequest.ScreenPosition.X;
			//var by = mapSectionRequest.ScreenPosition.Y;
			//if (bx == 0 && by == 0 || bx == 3 && by == 4)
			//{
			//	ReportSamplePoints(samplePointOffsets);
			//	ReportSamplePoints(samplePointsX);
			//}

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

		private void GenerateMapRow(IIterator iterator, ref IterationCountsRow itState, Vector256<int> targetIterationsVector)
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

			//iteratorSimd.MathOpCounts.RollUpNumberOfUnusedCalcs(iterationState.GetUnusedCalcs());
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

		// GetCoordinates
		private (BigVector blockPos, PointInt screenPos, FP31Val startingCx, FP31Val startingCy, FP31Val delta)
			GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var dtoMapper = new DtoMapper();

			var blockPos = dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var mapPosition = dtoMapper.MapFrom(mapSectionRequest.Position);
			var samplePointDelta = dtoMapper.MapFrom(mapSectionRequest.SamplePointDelta);

			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			var screenPos = mapSectionRequest.ScreenPosition;

			return (blockPos, screenPos, startingCx, startingCy, delta);
		}

		// Get the Z values
		private (FP31Vectors zRs, FP31Vectors zIs)
			GetZValues(MapSectionRequest mapSectionRequest, int rowNumber, int limbCount, int valueCount)
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

		private void ReportSamplePoints(FP31Val[] values)
		{
			foreach (var value in values)
			{
				Debug.WriteLine($"{FP31ValHelper.GetDiagDisplay("x", value.Mantissa)} {value.Exponent}.");
			}
		}

		private bool ShouldSkipThisSection(bool skipPositiveBlocks, bool skipLowDetailBlocks, FP31Val startingCx, FP31Val startingCy, PointInt screenPosition)
		{
			// Skip positive 'blocks'

			if (skipPositiveBlocks)
			{
				var xSign = startingCx.GetSign();
				var ySign = startingCy.GetSign();

				return xSign && ySign;
			}

			// Move directly to a block where at least one sample point reaches the iteration target.
			else if (skipLowDetailBlocks && (BigInteger.Abs(screenPosition.Y) > 1 || BigInteger.Abs(screenPosition.X) > 3))
			{
				return true;
			}

			return false;
		}

	}
}
