﻿using MEngineDataContracts;
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
		private readonly IteratorLimbFirst _iterator;

		#region Constructor

		public MapSectionGenerator(SizeInt blockSize, int limbCount)
		{
			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			_fp31VectorsMath = new FP31VectorsMath(apFixedPointFormat, blockSize.Width);
			_iterator = new IteratorLimbFirst(_fp31VectorsMath);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
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
				var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest, _fp31VectorsMath.LimbCount);

				var mapCalcSettings = mapSectionRequest.MapCalcSettings;
				_iterator.Threshold = (uint)mapCalcSettings.Threshold;
				_iterator.IncreasingIterations = mapSectionRequest.IncreasingIterations;

				var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);
				var iterationState = new IterationStateLimbFirst(mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, targetIterationsVector);

				//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);
				GenerateMapSection(_iterator, iterationState, coords, mapCalcSettings);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

				result = new MapSectionResponse(mapSectionRequest);
				result.MapSectionVectors = mapSectionVectors;
				//result.MathOpCounts = _iterator.MathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private void GenerateMapSection(IteratorLimbFirst iterator, IterationStateLimbFirst iterationState, IteratorCoords coords, MapCalcSettings mapCalcSettings)
		{
			var blockSize = iterationState.BlockSize;
			var rowCount = blockSize.Height;
			var stride = (byte)blockSize.Width;

			var scalarMath = new FP31ScalarMath(_fp31VectorsMath.ApFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(coords.Delta, stride, scalarMath);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(coords.StartingCx, samplePointOffsets, scalarMath);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(coords.StartingCy, samplePointOffsets, scalarMath);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			iterator.Crs.UpdateFrom(samplePointsX);

			for (int rowNumber = 0; rowNumber < rowCount; rowNumber++)
			{
				iterationState.SetRowNumber(rowNumber);

				// Load C & Z value decks
				var yPoint = samplePointsY[rowNumber];
				iterator.Cis.UpdateFrom(yPoint);

				//FillZValues(mapSectionZVectors, rowNumber, iterator.Zrs, iterator.Zis);
				iterator.Zrs.ClearManatissMems();
				iterator.Zis.ClearManatissMems();

				GenerateMapRow(iterator, ref iterationState);

				//iterator.Zrs.UpdateFromLimbSet(idx, _zrs);
				//iterator.Zis.UpdateFromLimbSet(idx, _zis);
			}

			iterationState.UpdateTheCountsSource(rowCount - 1);
			iterationState.UpdateTheHasEscapedFlagsSource(rowCount - 1);

		}

		#endregion

		#region Generate Map Row

		private void GenerateMapRow(IteratorLimbFirst iterator, ref IterationStateLimbFirst iterationState)
		{
			iterator.Reset();

			while (iterationState.InPlayList.Length > 0)
			{
				var escapedFlags = iterator.Iterate(iterationState.InPlayList, iterationState.InPlayListNarrow);

				var vectorsNoLongerInPlay = UpdateCounts(escapedFlags, ref iterationState);
				if (vectorsNoLongerInPlay.Count > 0)
				{
					iterationState.UpdateTheInPlayList(vectorsNoLongerInPlay);
				}
			}

			//_iterator.MathOpCounts.RollUpNumberOfUnusedCalcs(itState.GetUnusedCalcs());
		}

		private List<int> UpdateCounts(Vector256<int>[] escapedFlagVectors, ref IterationStateLimbFirst itState)
		{
			var toBeRemoved = new List<int>();
			var justOne = Vector256.Create(1);

			for (var idxPtr = 0; idxPtr < itState.InPlayList.Length; idxPtr++)
			{
				var idx = itState.InPlayList[idxPtr];

				var doneFlagsV = itState.DoneFlags[idx];
				var countsV = itState.CountsRow[idx];

				// Increment all counts
				var countsVt = Avx2.Add(countsV, justOne);

				// Take the incremented count, only if the doneFlags is false for each vector position.
				countsV = Avx2.BlendVariable(countsVt, countsV, doneFlagsV); // use First if Zero, second if 1
				itState.CountsRow[idx] = countsV;

				var unusedCalcsV = itState.UnusedCalcs[idx];

				// Increment all unused calculations
				var unusedCalcsVt = Avx2.Add(unusedCalcsV, justOne);

				// Take the incremented unusedCalc, only if the doneFlags is true for each vector position.
				itState.UnusedCalcs[idx] = Avx2.BlendVariable(unusedCalcsV, unusedCalcsVt, doneFlagsV);

				var hasEscapedFlagsV = itState.HasEscapedFlags[idx];

				// Apply the new escapeFlags, only if the doneFlags is false for each vector position
				var updatedHaveEscapedFlagsV = Avx2.BlendVariable(escapedFlagVectors[idx], hasEscapedFlagsV, doneFlagsV);
				itState.HasEscapedFlags[idx] = updatedHaveEscapedFlagsV;

				// Compare the new Counts with the TargetIterations
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, itState.TargetIterationsVector);

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(updatedHaveEscapedFlagsV, targetReachedCompVec);
				var updatedDoneFlagsV = Avx2.BlendVariable(doneFlagsV, Vector256<int>.AllBitsSet, escapedOrReachedVec);

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

		private void FillZValues(MapSectionZVectors mapSectionZVectors, int rowNumber, FP31Vectors zrVectors, FP31Vectors ziVectors)
		{
			//mapSectionZVectors.FillRRow(zrValArray.Mantissas, rowNumber);
			//mapSectionZVectors.FillIRow(ziValArray.Mantissas, rowNumber);
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
