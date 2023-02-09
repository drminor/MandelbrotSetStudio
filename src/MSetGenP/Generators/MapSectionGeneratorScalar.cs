using MEngineDataContracts;
using MSS.Common;
using MSetGenP.Types;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using System.Diagnostics;
using System.Linq;
using MSS.Types.MSet;

namespace MSetGenP
{
	public class MapSectionGeneratorScalar
	{
		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapPosition = mapSectionRequest.Position;
			var samplePointDelta = mapSectionRequest.SamplePointDelta;
			var blockSize = mapSectionRequest.BlockSize;
			var precision = mapSectionRequest.Precision;

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 4;

			//var fixedPointFormat = new ApFixedPointFormat(8, precision);
			//var fixedPointFormat = new ApFixedPointFormat(8, 129);

			//var fixedPointFormat = new ApFixedPointFormat(bitsBeforeBinaryPoint: RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: precision);
			var fixedPointFormat = new ApFixedPointFormat(limbCount: 2);


			var scalarMath = new ScalarMath(fixedPointFormat, threshold);

			//var dtoMapper = new DtoMapper();
			//var mapPosition = dtoMapper.MapFrom(mapPositionDto);
			//var samplePointDelta = dtoMapper.MapFrom(samplePointDeltaDto);

			var startingCx = scalarMath.CreateSmx(mapPosition.X);
			var startingCy = scalarMath.CreateSmx(mapPosition.Y);
			var delta = scalarMath.CreateSmx(samplePointDelta.Width);

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			//var blockPos = mapSectionRequest.BlockPosition;
			var blockPos = mapSectionRequest.BlockPosition;

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");

			var counts = GenerateMapSection(scalarMath, startingCx, startingCy, delta, blockSize, targetIterations, 
				out var mathOpCounts);
			
			var doneFlags = CalculateTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[blockSize.NumberOfCells];

			// TODO: Fix the MapSectionGeneratorScaler to produce MapSectionVectors.
			var result = new MapSectionResponse(mapSectionRequest);

			Debug.WriteLine($"{s1}, {s2}: {mathOpCounts}");

			return result;
		}

		private ushort[] GenerateMapSection(ScalarMath smxMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations,
			out MathOpCounts mathOpCounts)
		{

			mathOpCounts = new MathOpCounts();

			var use2CVersion = true;

			var result = new ushort[blockSize.NumberOfCells];

			var stride = (byte)blockSize.Width;
			var samplePointOffsets = smxMathHelper.BuildSamplePointOffsets(delta, stride);
			//ReportExponents(samplePointOffsets);
			var samplePointsX = smxMathHelper.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = smxMathHelper.BuildSamplePoints(startingCy, samplePointOffsets);

			var iterator = new IteratorScalar(smxMathHelper, targetIterations);

			for (int j = 0; j < samplePointsY.Length; j++)
			{
				var y = samplePointsY[j];
				var resultPtr = j * stride;

				for (int i = 0; i < samplePointsX.Length; i++)
				{
					//if (i == 63) Debug.WriteLine("Here");

					var x = samplePointsX[i];

					//var cntr = iterator.Iterate(x, y);
					//var cntr = iterator.IterateSmx2C(x, y);

					var cntr = use2CVersion ? iterator.IterateSmx2C(x, y) : iterator.Iterate(x, y);

					result[resultPtr + i] = cntr;
				}
			}

			mathOpCounts.NumberOfSplits = iterator.NumberOfSplits;
			mathOpCounts.NumberOfGetCarries = iterator.NumberOfGetCarries;
			mathOpCounts.NumberOfGrtrThanOps = iterator.NumberOfIsGrtrOpsSc;

			return result;
		}

		private void ReportExponents(Smx[] values)
		{
			foreach(var value in values)
			{
				Debug.WriteLine($"{value.Exponent}.");
			}
		}

		private bool[] CalculateTheDoneFlags(ushort[] counts, int targetIterations)
		{
			bool[] result;

			if (!counts.Any(x => x < targetIterations))
			{
				// All reached the target
				result = new bool[] { true };
			}
			else if (!counts.Any(x => x >= targetIterations))
			{
				// none reached the target
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = counts.Select(x => x >= targetIterations).ToArray();
			}

			return result;
		}
	}
}
