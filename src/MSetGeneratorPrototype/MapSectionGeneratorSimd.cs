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

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorSimd
	{
		private readonly ApFixedPointFormat _apFixedPointFormat;

		private readonly int _stride;
		private readonly uint _threshold;

		private readonly IIterator _iterator;

		public MapSectionGeneratorSimd()
		{
			var howManyLimbs = 2;
			_apFixedPointFormat = new ApFixedPointFormat(howManyLimbs);
			_stride = 128;
			_threshold = 4u;

			_iterator = new IteratorSimd(_apFixedPointFormat, _stride, _threshold);
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var skipPositiveBlocks = false;
			var skipLowDetailBlocks = false;

			var precision = mapSectionRequest.Precision;

			var (blockPos, startingCx, startingCy, delta) = GetCoordinates(mapSectionRequest, _apFixedPointFormat);
			var screenPos = mapSectionRequest.ScreenPosition;

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. Limbs: {apFixedPointFormat.LimbCount}.");

			Debug.WriteLine($"Starting : {screenPos}: {blockPos}, delta: {s3}, #oflimbs: {_apFixedPointFormat.LimbCount}. MapSecReq Precision: {precision}.");

			MapSectionResponse result;

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, startingCx, startingCy, screenPos))
			{
				result = BuildEmptyResponse(mapSectionRequest);
			}
			else
			{
				result = GenerateMapSection(_iterator, mapSectionRequest, blockPos, startingCx, startingCy, delta, _apFixedPointFormat);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");
			}

			return result;
		}

		// Generate MapSection
		private MapSectionResponse GenerateMapSection(IIterator iterator, MapSectionRequest mapSectionRequest, BigVector blockPos, FP31Val startingCx, FP31Val startingCy, FP31Val delta, ApFixedPointFormat apFixedPointFormat)
		{
			var (blockSize, targetIterations, threshold, hasEscapedFlags, counts, escapeVelocities) = GetMapParameters(mapSectionRequest);
			var rowCount = blockSize.Height;
			var stride = (byte)blockSize.Width;


			var scalarMath9 = new ScalarMath9(apFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(delta, stride, scalarMath9);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(startingCx, samplePointOffsets, scalarMath9);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(startingCy, samplePointOffsets, scalarMath9);

			var bx = mapSectionRequest.ScreenPosition.X;
			var by = mapSectionRequest.ScreenPosition.Y;

			//if (bx == 0 && by == 0 || bx == 3 && by == 4)
			//{
			//	ReportSamplePoints(samplePointOffsets);
			//	ReportSamplePoints(samplePointsX);
			//}

			iterator.Crs = new FP31Vectors(samplePointsX);

			iterator.Threshold = threshold;
			IterationState iterationState = new IterationState(stride, targetIterations);

			for (int rowNumber = 0; rowNumber < rowCount; rowNumber++)
			{
				var resultIndex = rowNumber * stride;

				//  Load Iteration State
				var hasEscapedFlagsRow = new Span<int>(hasEscapedFlags, resultIndex, stride);
				var countsRow = new Span<int>(counts, resultIndex, stride);
				var escapeVelocitiesRow = new Span<int>(escapeVelocities, resultIndex, stride);
				iterationState.LoadRow(hasEscapedFlagsRow, countsRow, escapeVelocitiesRow);

				// Load C & Z value decks
				var yPoint = samplePointsY[rowNumber];
				iterator.Cis = new FP31Vectors(yPoint, stride);

				//var (zRs, zIs) = GetZValues(mapSectionRequest, rowNumber, apFixedPointFormat.LimbCount, stride);
				iterator.Zrs.ClearManatissMems();
				iterator.Zis.ClearManatissMems();
				iterator.ZValuesAreZero = true;

				GenerateMapRow(iterator, iterationState);

				iterationState.Counts.CopyTo(countsRow);
				iterationState.EscapeVelocities.CopyTo(escapeVelocitiesRow);
			}

			var shortCounts = counts.Select(x => (ushort)x).ToArray();
			var shortEscVels = escapeVelocities.Select(x => (ushort)x).ToArray();

			var compressedHasEscapedFlags = CompressHasEscapedFlags(hasEscapedFlags);

			var result = new MapSectionResponse(mapSectionRequest, compressedHasEscapedFlags, shortCounts, shortEscVels, zValues: null);
			result.MathOpCounts = iterator.MathOpCounts;

			return result;
		}

		private void GenerateMapRow(IIterator iteratorSimd, IterationState iterationState)
		{
			var inPlayList = iterationState.InPlayList;

			while (inPlayList.Length > 0)
			{

				var escapedFlags = iteratorSimd.Iterate(inPlayList);

				var vectorsNoLongerInPlay = UpdateCounts(escapedFlags, iterationState);
				inPlayList = iterationState.UpdateTheInPlayList(vectorsNoLongerInPlay);
			}

			iteratorSimd.MathOpCounts.RollUpNumberOfUnusedCalcs(iterationState.UnusedCalcs);
		}

		private List<int> UpdateCounts(Vector256<int>[] escapedFlagVectors, IterationState iterationState)
		{
			var toBeRemoved = new List<int>();

			var justOne = Vector256.Create(1);
			var targetIterationsVector = iterationState.TargetIterationsVector;

			iterationState.GetVectors(out var hasEscapedFlagsVectors, out var countsVectors, out var escapeVelocitiesVectors, out var doneFlagsVectors, out var unusedCalcsVectors);

			var indexes = iterationState.InPlayList;

			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var doneFlagsV = doneFlagsVectors[idx];

				// Increment all counts
				var countsVt = Avx2.Add(countsVectors[idx], justOne);
				// Take the incremented count, only if the doneFlags is false for each vector position.
				var countsV = Avx2.BlendVariable(countsVt.AsByte(), countsVectors[idx].AsByte(), doneFlagsV.AsByte()).AsInt32(); // use First if Zero, second if 1
				countsVectors[idx] = countsV;

				// Increment all unused calculations
				var unusedCalcsVt = Avx2.Add(unusedCalcsVectors[idx], justOne);
				// Take the incremented unusedCalc, only if the doneFlags is true for each vector position.
				var unusedCalcsV = Avx2.BlendVariable(unusedCalcsVectors[idx].AsByte(), unusedCalcsVt.AsByte(), doneFlagsV.AsByte()).AsInt32();
				unusedCalcsVectors[idx] = unusedCalcsV;

				// Apply the new escapeFlags, only if the doneFlags is false for each vector position
				var updatedHaveEscapedFlagsV = Avx2.BlendVariable(escapedFlagVectors[idx].AsByte(), hasEscapedFlagsVectors[idx].AsByte(), doneFlagsV.AsByte()).AsInt32();
				hasEscapedFlagsVectors[idx] = updatedHaveEscapedFlagsV;

				// Compare the new Counts with the TargetIterations
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsVector);

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(updatedHaveEscapedFlagsV, targetReachedCompVec);
				var updatedDoneFlagsV = Avx2.BlendVariable(doneFlagsVectors[idx].AsByte(), Vector256<int>.AllBitsSet.AsByte(), escapedOrReachedVec.AsByte()).AsInt32();

				doneFlagsVectors[idx] = updatedDoneFlagsV;

				var compositeIsDone = Avx2.MoveMask(updatedDoneFlagsV.AsByte());

				if (compositeIsDone == -1)
				{
					toBeRemoved.Add(idx);
				}
			}

			return toBeRemoved;
		}

		// GetCoordinates
		private (BigVector blockPos, FP31Val startingCx, FP31Val startingCy, FP31Val delta)
			GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var dtoMapper = new DtoMapper();

			var blockPos = dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var mapPosition = dtoMapper.MapFrom(mapSectionRequest.Position);
			var samplePointDelta = dtoMapper.MapFrom(mapSectionRequest.SamplePointDelta);

			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			return (blockPos, startingCx, startingCy, delta);
		}

		// Get Map Parameters
		private (SizeInt blockSize, int targetIterations, uint threshold, int[] hasEscapedFlags, int[] counts, int[] escapeVelocities)
			GetMapParameters(MapSectionRequest mapSectionRequest)
		{
			var blockSize = mapSectionRequest.BlockSize;
			//var precision = mapSectionRequest.Precision;

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 4;

			//var doneFlags = mapSectionRequest.DoneFlags;
			var hasEscapedFlags = new int[blockSize.NumberOfCells];
			
			//var counts = mapSectionRequest.Counts;
			var counts = new int[blockSize.NumberOfCells];

			//var escapeVelocities = mapSectionRequest.EscapeVelocities;
			var escapeVelocities = new int[blockSize.NumberOfCells];

			return (blockSize, targetIterations, threshold, hasEscapedFlags, counts, escapeVelocities);
		}

		// Get the Z values
		private (FP31Vectors zRs, FP31Vectors zIs)
			GetZValues(MapSectionRequest mapSectionRequest, int rowNumber, int limbCount, int valueCount)
		{
			var zRs = new FP31Vectors(limbCount, valueCount);
			var zIs = new FP31Vectors(limbCount, valueCount);

			return (zRs, zIs);
		}

		private MapSectionResponse BuildEmptyResponse(MapSectionRequest mapSectionRequest)
		{
			var blockSize = mapSectionRequest.BlockSize;
			var counts = new ushort[blockSize.NumberOfCells];
			var escapeVelocities = new ushort[blockSize.NumberOfCells];

			//var doneFlags = new bool[blockSize.NumberOfCells];
			//var compressedDoneFlags = CompressTheDoneFlags(doneFlags);
			var compressedDoneFlags = new bool[] { false };

			var result = new MapSectionResponse(mapSectionRequest, compressedDoneFlags, counts, escapeVelocities, zValues: null);
			return result;
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
