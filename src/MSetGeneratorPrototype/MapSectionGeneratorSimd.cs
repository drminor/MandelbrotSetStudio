using MEngineDataContracts;
using MSS.Common;
using MSS.Common.APValues;
using MSS.Common.DataTransferObjects;
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

			var precision = mapSectionRequest.Precision;
			var fpmTest = new ApFixedPointFormat(bitsBeforeBinaryPoint: 8, minimumFractionalBits: precision);

			var howManyLimbs = 3;
			var apFixedPointFormat = new ApFixedPointFormat(howManyLimbs);

			var (blockPos, startingCx, startingCy, delta) = GetCoordinates(mapSectionRequest, apFixedPointFormat);
			var screenPos = mapSectionRequest.ScreenPosition;

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. Limbs: {apFixedPointFormat.LimbCount}.");

			Debug.WriteLine($"Starting : {screenPos}: {blockPos}, delta: {s3}, #oflimbs: {apFixedPointFormat.LimbCount}. MapSecReq Precision: {precision}. Test # of Limbs: {fpmTest.LimbCount}.");


			MapSectionResponse result;

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, startingCx, startingCy, screenPos))
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
			GenerateMapSection(MapSectionRequest mapSectionRequest, BigVector blockPos, FP31Val startingCx, FP31Val startingCy, FP31Val delta, ApFixedPointFormat apFixedPointFormat, out MathOpCounts mathOpCounts)
		{
			var (blockSize, targetIterations, threshold, hasEscapedFlags, counts, escapeVelocities) = GetMapParameters(mapSectionRequest);
			var rowCount = blockSize.Height;
			var stride = (byte)blockSize.Width;

			var vecMath = new VecMath9(apFixedPointFormat, stride, threshold);
			var iteratorSimd = new IteratorSimd(vecMath);

			var scalarMath9 = new ScalarMath9(apFixedPointFormat);
			var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(delta, stride, scalarMath9);
			var samplePointsX = SamplePointBuilder.BuildSamplePoints(startingCx, samplePointOffsets, scalarMath9);
			var samplePointsY = SamplePointBuilder.BuildSamplePoints(startingCy, samplePointOffsets, scalarMath9);

			var bx = mapSectionRequest.ScreenPosition.X;
			var by = mapSectionRequest.ScreenPosition.Y;

			if (bx == 0 && by == 0 || bx == 3 && by == 4)
			{
				ReportSamplePoints(samplePointOffsets);
				ReportSamplePoints(samplePointsX);
			}

			var cRs = new FP31Deck(samplePointsX);

			for (int rowNumber = 0; rowNumber < rowCount; rowNumber++)
			{
				var yPoint = samplePointsY[rowNumber];
				//var fp31YPoint = new FP31Val(yPoint, apFixedPointFormat.TargetExponent, apFixedPointFormat.BitsBeforeBinaryPoint, startingCx.Precision);
				var cIs = new FP31Deck(yPoint, stride);

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
			GetZValues(MapSectionRequest mapSectionRequest, int rowNumber, int limbCount, int valueCount)
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
