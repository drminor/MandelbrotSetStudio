﻿using MEngineDataContracts;
using MSS.Common;
using MSS.Common.APValues;
using MSS.Common.DataTransferObjects;
using MSS.Common.SmxVals;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorSimd
	{
		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var skipPositiveBlocks = false;
			var skipLowDetailBlocks = false;

			//var fixedPointFormat = new ApFixedPointFormat(bitsBeforeBinaryPoint: 8, minimumFractionalBits: precision);

			var howManyLimbs = 2;
			var apFixedPointFormat = new ApFixedPointFormat(howManyLimbs);

			var (blockPos, startingCx, startingCy, delta) = GetCoordinates(mapSectionRequest, apFixedPointFormat);

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. Limbs: {apFixedPointFormat.LimbCount}.");

			MapSectionResponse result;

			// Skip positive 'blocks'
			if (skipPositiveBlocks && startingCx.Sign && startingCy.Sign)
			{
				result = BuildEmptyResponse(mapSectionRequest);
			}

			// Move directly to a block where at least one sample point reaches the iteration target.
			else if (skipLowDetailBlocks && (BigInteger.Abs(blockPos.Y) > 1 || BigInteger.Abs(blockPos.X) > 3))
			{
				result = BuildEmptyResponse(mapSectionRequest);
			}

			// Perform the calculations
			else
			{
				var (counts, escapeVelocities, doneFlags) = GenerateMapSection(mapSectionRequest, blockPos, startingCx, startingCy, delta, apFixedPointFormat, out var mathOpCounts);

				//Debug.WriteLine($"Completed: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). ACarries: {aCarries}, MCarries:{mCarries}.");
				//Debug.WriteLine($"Completed: BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. ACarries: {subSectionGeneratorVector.NumberOfACarries}, MCarries:{subSectionGeneratorVector.NumberOfMCarries}.");
				//Debug.WriteLine($"{s1}, {s2}: Adds: {mathOpCounts.NumberOfACarries}\tSubtracts: {numberOfMCarries}.");

				//Debug.WriteLine($"{s1}, {s2}: {mathOpCounts}");

				var compressedDoneFlags = CompressTheDoneFlags(doneFlags);

				if (compressedDoneFlags.Length != 1 || !compressedDoneFlags[0])
				{
					Debug.WriteLine("WARNING: Some sample points are not complete.");
				}

				result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, compressedDoneFlags, zValues: null);
				result.MathOpCounts = mathOpCounts;
			}

			return result;
		}

		private (ushort[] counts, ushort[] escapeVelocities, bool[] doneFlags) 
			GenerateMapSection(MapSectionRequest mapSectionRequest, BigVector blockPos, Smx startingCx, Smx startingCy, Smx delta, ApFixedPointFormat apFixedPointFormat, out MathOpCounts mathOpCounts)
		{
			var (blockSize, targetIterations, threshold, counts, escapeVelocities, doneFlags) = GetMapParameters(mapSectionRequest);

			mathOpCounts = new MathOpCounts();

			var stride = (byte)blockSize.Width;

			var scalarMath9 = new ScalarMath9(apFixedPointFormat, threshold);
			var vecMath = new VecMath9(apFixedPointFormat, stride, threshold);
			var iteratorSimd = new IteratorSimd(vecMath);

			var samplePointOffsets = scalarMath9.BuildSamplePointOffsets(delta, stride);
			
			var samplePointsX = scalarMath9.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsX2C = Convert(samplePointsX);

			var samplePointsY = scalarMath9.BuildSamplePoints(startingCy, samplePointOffsets);
			var samplePointsY2C = Convert(samplePointsY);

			var cRs = new FP31Deck(samplePointsX2C);

			for (int j = 0; j < samplePointsY.Length; j++)
			{
				var yPoints = Duplicate(samplePointsY2C[j], stride);
				var cIs = new FP31Deck(yPoints);

				//Array.Copy(doneFlags, j * stride, rowDoneFlags, 0, stride);

				vecMath.Refresh();
				vecMath.BlockPosition = blockPos;
				vecMath.RowNumber = j;

				var rowCounts = SubSectionGenerator.GenerateMapSection(vecMath, iteratorSimd, targetIterations, cRs, cIs, out var rowDoneFlags);
				Array.Copy(rowCounts, 0, counts, j * stride, stride);
				Array.Copy(rowDoneFlags, 0, doneFlags, j * stride, stride);

				vecMath.MathOpCounts.NumberOfUnusedCalcs = vecMath.UnusedCalcs.Sum();
				mathOpCounts.Update(vecMath.MathOpCounts);
			}

			return (counts, escapeVelocities, doneFlags);
		}

		private (BigVector blockPos, Smx startingCx, Smx startingCy, Smx delta)
			GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var dtoMapper = new DtoMapper();

			var blockPos = dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var mapPosition = dtoMapper.MapFrom(mapSectionRequest.Position);
			var samplePointDelta = dtoMapper.MapFrom(mapSectionRequest.SamplePointDelta);

			var startingCx = ScalarMathHelper.CreateSmx(mapPosition.X, apFixedPointFormat);
			var startingCy = ScalarMathHelper.CreateSmx(mapPosition.Y, apFixedPointFormat);
			var delta = ScalarMathHelper.CreateSmx(samplePointDelta.Width, apFixedPointFormat);

			return (blockPos, startingCx, startingCy, delta);
		}

		private (SizeInt blockSize, int targetIterations, uint threshold, ushort[] counts, ushort[] escapeVelocities, bool[] doneFlags)
			GetMapParameters(MapSectionRequest mapSectionRequest)
		{
			var blockSize = mapSectionRequest.BlockSize;
			//var precision = mapSectionRequest.Precision;

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 4;

			//var counts = mapSectionRequest.Counts;
			var counts = new ushort[blockSize.NumberOfCells];

			//var escapeVelocities = mapSectionRequest.EscapeVelocities;
			var escapeVelocities = new ushort[blockSize.NumberOfCells];

			//var doneFlags = mapSectionRequest.DoneFlags;
			var doneFlags = new bool[blockSize.NumberOfCells];

			return (blockSize, targetIterations, threshold, counts, escapeVelocities, doneFlags);
		}

		private MapSectionResponse BuildEmptyResponse(MapSectionRequest mapSectionRequest)
		{
			var blockSize = mapSectionRequest.BlockSize;
			var counts = new ushort[blockSize.NumberOfCells];
			var escapeVelocities = new ushort[blockSize.NumberOfCells];

			//var doneFlags = new bool[blockSize.NumberOfCells];
			//var compressedDoneFlags = CompressTheDoneFlags(doneFlags);
			var compressedDoneFlags = new bool[] { false };

			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, compressedDoneFlags, zValues: null);
			return result;
		}

		private bool[] CompressTheDoneFlags(bool[] doneFlags)
		{
			bool[] result;

			if (!doneFlags.Any(x => !x))
			{
				// All reached the target
				result = new bool[] { true };
			}
			else if (!doneFlags.Any(x => x))
			{
				// none reached the target
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = doneFlags;
			}

			return result;
		}

		private FP31Val[] Duplicate(FP31Val fp31Val, int count)
		{
			var result = new FP31Val[count];

			for(int i = 0; i < count; i++)
			{
				result[i] = fp31Val.Clone();
			}

			return result;
		}

		private FP31Val[] Convert(Smx[] smxes)
		{
			var temp = new List<FP31Val>();

			foreach (var smx in smxes)
			{
				temp.Add(Convert(smx));	
			}

			var result = temp.ToArray();

			return result;
		}

		private FP31Val Convert(Smx smx)
		{
			FP31Val result;

			var packedMantissa = FP31ValHelper.TakeLowerHalves(smx.Mantissa);


			if (smx.IsZero)
			{
				if (!smx.Sign)
				{
					Debug.WriteLine("WARNING: Found a value of -0.");
				}

				result = new FP31Val(packedMantissa, smx.Exponent, smx.BitsBeforeBP, smx.Precision);
			}
			else
			{
				var twoCMantissa = FP31ValHelper.ConvertTo2C(packedMantissa, smx.Sign);
				result = new FP31Val(twoCMantissa, smx.Exponent, smx.BitsBeforeBP, smx.Precision);
			}

			return result;
		}

		private void ReportExponents(Smx[] values)
		{
			foreach (var value in values)
			{
				Debug.WriteLine($"{value.Exponent}.");
			}
		}

	}
}
