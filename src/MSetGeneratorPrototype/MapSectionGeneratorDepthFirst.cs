﻿using MEngineDataContracts;
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
		private readonly FP31VecMath _fp31VecMath;
		private readonly IteratorSimdDepthFirst _iterator;

		private readonly Vector256<int> _justOne;

		private readonly Vector256<byte> ALL_BITS_SET;

		#region Constructor

		public MapSectionGeneratorDepthFirst(SizeInt blockSize, int limbCount)
		{
			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			_fp31VecMath = new FP31VecMath(apFixedPointFormat);
			_iterator = new IteratorSimdDepthFirst(_fp31VecMath, blockSize.Width);

			_crs = new Vector256<uint>[limbCount];
			_cis = new Vector256<uint>[limbCount];
			_zrs = new Vector256<uint>[limbCount];
			_zis = new Vector256<uint>[limbCount];

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
				var mapCalcSettings = mapSectionRequest.MapCalcSettings;

				//var mapSectionVectors = mapSectionRequest.MapSectionVectors ?? new MapSectionVectors(mapSectionRequest.BlockSize);
				var mapSectionVectors = mapSectionRequest.MapSectionVectors ?? throw new ArgumentNullException("The MapSectionVectors is null.");
				mapSectionRequest.MapSectionVectors = null;

				var mapSectionZVectors = mapSectionRequest.MapSectionZVectors ?? new MapSectionZVectors(mapSectionRequest.BlockSize, _fp31VecMath.LimbCount);
				mapSectionRequest.MapSectionZVectors = null;


				//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);
				GenerateMapSection(_iterator, mapSectionVectors, mapSectionZVectors, coords, mapCalcSettings);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

				result = new MapSectionResponse(mapSectionRequest);
				result.MapSectionVectors = mapSectionVectors;
				result.MapSectionZVectors = mapSectionZVectors;
				//result.MathOpCounts = _iterator.MathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private void GenerateMapSection(IteratorSimdDepthFirst iterator, MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors, IteratorCoords coords, MapCalcSettings mapCalcSettings)
		{
			var blockSize = mapSectionVectors.BlockSize;
			var rowCount = blockSize.Height;
			var stride = (byte)blockSize.Width;

			var scalarMath = new FP31ScalarMath(_fp31VecMath.ApFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(coords.Delta, stride, scalarMath);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(coords.StartingCx, samplePointOffsets, scalarMath);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(coords.StartingCy, samplePointOffsets, scalarMath);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			iterator.Threshold = (uint)mapCalcSettings.Threshold;
			iterator.Crs.UpdateFrom(samplePointsX);
			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);

			var itState = new IterationCountsRow(mapSectionVectors);

			for (int rowNumber = 0; rowNumber < rowCount; rowNumber++)
			{
				itState.SetRowNumber(rowNumber);

				// Load C & Z value decks
				var yPoint = samplePointsY[rowNumber];
				iterator.Cis.UpdateFrom(yPoint);

				FillZValues(mapSectionZVectors, rowNumber, iterator.Zrs, iterator.Zis);
				//iterator.Zrs.ClearManatissMems();
				//iterator.Zis.ClearManatissMems();
				iterator.ZValuesAreZero = true;

				for (var idxPtr = 0; idxPtr < itState.InPlayList.Length; idxPtr++)
				{
					var idx = itState.InPlayList[idxPtr];
					GenerateMapCol(idx, iterator, ref itState, targetIterationsVector);
				}

				//iterator.Zrs.m mapSectionZVectors.Zrs

				mapSectionZVectors.UpdateRRowFrom(iterator.Zrs.Mantissas, rowNumber);
				mapSectionZVectors.UpdateIRowFrom(iterator.Zis.Mantissas, rowNumber);

				//_iterator.MathOpCounts.RollUpNumberOfUnusedCalcs(itState.GetUnusedCalcs());
			}
		}

		#endregion

		#region Generate One Vector

		private Vector256<uint>[] _crs;
		private Vector256<uint>[] _cis;
		private Vector256<uint>[] _zrs;
		private Vector256<uint>[] _zis;


		private void GenerateMapColOld(int idx, IteratorSimdDepthFirst iterator, ref IterationCountsRow itState, Vector256<int> targetIterationsVector)
		{
			iterator.Crs.FillLimbSet(idx, _crs);
			iterator.Cis.FillLimbSet(idx, _cis);
			iterator.Zrs.FillLimbSet(idx, _zrs);
			iterator.Zis.FillLimbSet(idx, _zis);

			iterator.ZValuesAreZero = true;

			var hasEscapedFlagsV = itState.HasEscapedFlags[idx].AsByte();
			var countsV = itState.Counts[idx];

			var doneFlagsV = itState.DoneFlags[idx].AsByte();
			var unusedCalcsV = itState.UnusedCalcs[idx];

			var allDone = false;

			while (!allDone)
			{
				var escapedFlagsVec = iterator.Iterate(_crs, _cis, _zrs, _zis);

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
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsVector).AsByte();

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec).AsByte();
				doneFlagsV = Avx2.BlendVariable(doneFlagsV, ALL_BITS_SET, escapedOrReachedVec);

				var compositeIsDone = Avx2.MoveMask(doneFlagsV);
				allDone = compositeIsDone == -1;
			}

			itState.HasEscapedFlags[idx] = hasEscapedFlagsV.AsInt32();
			itState.Counts[idx] = countsV;

			itState.DoneFlags[idx] = doneFlagsV.AsInt32();
			itState.UnusedCalcs[idx] = unusedCalcsV;
		}

		private void GenerateMapCol(int idx, IteratorSimdDepthFirst iterator, ref IterationCountsRow itState, Vector256<int> targetIterationsVector)
		{
			iterator.Crs.FillLimbSet(idx, _crs);
			iterator.Cis.FillLimbSet(idx, _cis);
			iterator.Zrs.FillLimbSet(idx, _zrs);
			iterator.Zis.FillLimbSet(idx, _zis);

			iterator.ZValuesAreZero = true;

			var hasEscapedFlagsV = itState.HasEscapedFlags[idx].AsByte();
			var countsV = itState.Counts[idx];

			var doneFlagsV = itState.DoneFlags[idx].AsByte();
			var unusedCalcsV = itState.UnusedCalcs[idx];

			var allDone = false;

			while (!allDone)
			{
				var escapedFlagsVec = iterator.Iterate(_crs, _cis, _zrs, _zis);

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
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsVector).AsByte();

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var escapedOrReachedVec = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec).AsByte();
				doneFlagsV = Avx2.BlendVariable(doneFlagsV, ALL_BITS_SET, escapedOrReachedVec);

				var compositeIsDone = Avx2.MoveMask(doneFlagsV);
				allDone = compositeIsDone == -1;
			}

			itState.HasEscapedFlags[idx] = hasEscapedFlagsV.AsInt32();
			itState.Counts[idx] = countsV;

			itState.DoneFlags[idx] = doneFlagsV.AsInt32();
			itState.UnusedCalcs[idx] = unusedCalcsV;

			iterator.Zrs.UpdateFromLimbSet(idx, _zrs);
			iterator.Zis.UpdateFromLimbSet(idx, _zis);
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

		private void FillZValues(MapSectionZVectors mapSectionZVectors, int rowNumber, FP31ValArray zrValArray, FP31ValArray ziValArray)
		{
			mapSectionZVectors.FillRRow(zrValArray.Mantissas, rowNumber);
			mapSectionZVectors.FillIRow(ziValArray.Mantissas, rowNumber);
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
