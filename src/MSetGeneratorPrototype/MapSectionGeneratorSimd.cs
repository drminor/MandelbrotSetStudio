using MEngineDataContracts;
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

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, startingCx, startingCy, blockPos))
			{
				result = BuildEmptyResponse(mapSectionRequest);
			}
			else
			{
				var (hasEscapedFlags, counts, escapeVelocities) = GenerateMapSection(mapSectionRequest, blockPos, startingCx, startingCy, delta, apFixedPointFormat, out var mathOpCounts);

				//Debug.WriteLine($"Completed: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). ACarries: {aCarries}, MCarries:{mCarries}.");
				//Debug.WriteLine($"Completed: BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. ACarries: {subSectionGeneratorVector.NumberOfACarries}, MCarries:{subSectionGeneratorVector.NumberOfMCarries}.");
				//Debug.WriteLine($"{s1}, {s2}: Adds: {mathOpCounts.NumberOfACarries}\tSubtracts: {numberOfMCarries}.");

				//Debug.WriteLine($"{s1}, {s2}: {mathOpCounts}");

				var compressedHasEscapedFlags = CompressHasEscapedFlags(hasEscapedFlags);

				//if (compressedDoneFlags.Length != 1 || !compressedDoneFlags[0])
				//{
				//	Debug.WriteLine("WARNING: Some sample points are not complete.");
				//}

				result = new MapSectionResponse(mapSectionRequest, compressedHasEscapedFlags, counts, escapeVelocities, zValues: null);
				result.MathOpCounts = mathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private (bool[] hasEscapedFlags, ushort[] counts, ushort[] escapeVelocities) 
			GenerateMapSection(MapSectionRequest mapSectionRequest, BigVector blockPos, Smx startingCx, Smx startingCy, Smx delta, ApFixedPointFormat apFixedPointFormat, out MathOpCounts mathOpCounts)
		{
			var (blockSize, targetIterations, threshold, hasEscapedFlags, counts, escapeVelocities) = GetMapParameters(mapSectionRequest);
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

			for (int rowNumber = 0; rowNumber < samplePointsY.Length; rowNumber++)
			{
				var cIs = new FP31Deck(samplePointsY2C[rowNumber], stride);

				var resultIndex = rowNumber * stride;
				var hasEscapedSpan = new Span<bool>(hasEscapedFlags, resultIndex, stride);
				var countsSpan = new Span<ushort>(counts, resultIndex, stride);
				var escapeVelocitiesSpan = new Span<ushort>(escapeVelocities, resultIndex, stride);

				var zValues = GetZValues(mapSectionRequest, rowNumber, apFixedPointFormat.LimbCount, stride);

				var samplePointValues = new SamplePointValues(cRs, cIs, zValues.zRs, zValues.zIs, hasEscapedSpan, countsSpan, escapeVelocitiesSpan);

				var unusedCalcs = SubSectionGenerator.GenerateMapSection(samplePointValues, iteratorSimd, blockPos, rowNumber, targetIterations);

				vecMath.MathOpCounts.NumberOfUnusedCalcs += unusedCalcs.Sum();
			}

			mathOpCounts = vecMath.MathOpCounts;

			return (hasEscapedFlags, counts, escapeVelocities);
		}

		// GetCoordinates
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

		// Get Map Parameters
		private (SizeInt blockSize, int targetIterations, uint threshold, bool[] hasEscapedFlags, ushort[] counts, ushort[] escapeVelocities)
			GetMapParameters(MapSectionRequest mapSectionRequest)
		{
			var blockSize = mapSectionRequest.BlockSize;
			//var precision = mapSectionRequest.Precision;

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 4;

			//var doneFlags = mapSectionRequest.DoneFlags;
			var hasEscapedFlags = new bool[blockSize.NumberOfCells];
			
			//var counts = mapSectionRequest.Counts;
			var counts = new ushort[blockSize.NumberOfCells];

			//var escapeVelocities = mapSectionRequest.EscapeVelocities;
			var escapeVelocities = new ushort[blockSize.NumberOfCells];

			return (blockSize, targetIterations, threshold, hasEscapedFlags, counts, escapeVelocities);
		}

		// Get the Z values
		private (FP31Deck zRs, FP31Deck zIs)
			GetZValues(MapSectionRequest mapSectionRequest, int rowNumber, int valueCount, int limbCount)
		{
			var zRs = new FP31Deck(limbCount, valueCount);
			var zIs = new FP31Deck(limbCount, valueCount);

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

		private bool[] CompressHasEscapedFlags(bool[] doneFlags)
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

		private FP31Deck BuildSamplePointOffsets(FP31Val delta, byte extent, VecMath9 vecMath9)
		{
			var result = new FP31Deck(delta.LimbCount, extent);
			//var resultLimbs = result.GetLimbVectorsUW();

			var vResult = new FP31Deck(delta.LimbCount, FP31Deck.Lanes);

			// Create a vector, vec2 that has 1, 2, 3, 4, 5, 6 and 7 times the value of delta.
			var vec1 = new FP31Deck(delta, FP31Deck.Lanes);
			var vec2 = vec1.Clone();

			//vecMath9.Add(vec1, vec2, vResult);
			//vec2.SetMantissa(1, vResult.GetMantissa(0));

			//vecMath9.Add(vec1, vec2, vResult);
			//vec2.SetMantissa(2, vResult.GetMantissa(1));

			//vecMath9.Add(vec1, vec2, vResult);
			//vec2.SetMantissa(3, vResult.GetMantissa(2));

			//result.MantissaMemories[] .SetMantissa() = vec2;

			// Create the result value so that each vector value is set to vec2

			// Update the result by limb, by vector
			// For each succesive vector, create a vector that is a duplicate of the right-most value from the last vector
			// get the limb values from that vector, and add this to the next set of limb values



			//for (var i = 0; i < extent; i++)
			//{
			//	var samplePointOffset = Multiply(delta, (byte)i);
			//	CheckForceExpResult(samplePointOffset, "BuildSPOffsets");
			//	result[i] = samplePointOffset;
			//}

			return result;
		}

		private FP31Deck BuildSamplePoints(FP31Val startValue, FP31Deck samplePointOffsets)
		{
			var result = new FP31Deck(startValue.LimbCount, samplePointOffsets.ValueCount);

			//for (var i = 0; i < samplePointOffsets.ValueCount; i++)
			//{
			//	var samplePointSa = Add(startValue, samplePointOffsets[i], "add spd offset to start value");
			//	result[i] = samplePointSa;
			//}

			return result;
		}

		private void ReportExponents(Smx[] values)
		{
			foreach (var value in values)
			{
				Debug.WriteLine($"{value.Exponent}.");
			}
		}

		private bool ShouldSkipThisSection(bool skipPositiveBlocks, bool skipLowDetailBlocks, Smx startingCx, Smx startingCy, BigVector blockPos)
		{
			// Skip positive 'blocks'
			if (skipPositiveBlocks && startingCx.Sign && startingCy.Sign)
			{
				return true;
			}

			// Move directly to a block where at least one sample point reaches the iteration target.
			else if (skipLowDetailBlocks && (BigInteger.Abs(blockPos.Y) > 1 || BigInteger.Abs(blockPos.X) > 3))
			{
				return true;
			}

			return false;
		}

	}
}
