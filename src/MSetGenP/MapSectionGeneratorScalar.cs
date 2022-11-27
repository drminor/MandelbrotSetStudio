using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;

namespace MSetGenP
{
	public class MapSectionGeneratorScalar
	{
		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapPositionDto = mapSectionRequest.Position;
			var samplePointDeltaDto = mapSectionRequest.SamplePointDelta;
			var blockSize = mapSectionRequest.BlockSize;
			var precision = mapSectionRequest.Precision;

			var targetExponent = samplePointDeltaDto.Exponent - 20;
			var smxMathHelper = new SmxMathHelper(targetExponent);

			var startingCx = smxMathHelper.CreateSmxFromDto(mapPositionDto.X, mapPositionDto.Exponent, precision);
			var startingCy = smxMathHelper.CreateSmxFromDto(mapPositionDto.Y, mapPositionDto.Exponent, precision);
			var delta = smxMathHelper.CreateSmxFromDto(samplePointDeltaDto.Width, samplePointDeltaDto.Exponent, precision);

			var s1 = RValueHelper.ConvertToString(startingCx.GetRValue());
			var s2 = RValueHelper.ConvertToString(startingCy.GetRValue());
			var s3 = RValueHelper.ConvertToString(delta.GetRValue());

			var blockPos = mapSectionRequest.BlockPosition;
			Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;

			var counts = GenerateMapSection(smxMathHelper, startingCx, startingCy, delta, blockSize, targetIterations, threshold);
			var doneFlags = CalculateTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues: null);

			return result;
		}

		private ushort[] GenerateMapSection(SmxMathHelper smxMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations, uint threshold)
		{
			var result = new ushort[blockSize.NumberOfCells];

			var stride = blockSize.Width;
			var samplePointOffsets = smxMathHelper.BuildSamplePointOffsets(delta, stride);
			//ReportExponents(samplePointOffsets);
			var samplePointsX = smxMathHelper.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = smxMathHelper.BuildSamplePoints(startingCy, samplePointOffsets);

			var iterator = new IteratorScalar(smxMathHelper, targetIterations);

			for (int j = 0; j < samplePointsY.Length; j++)
			{
				for (int i = 0; i < samplePointsX.Length; i++)
				{
					var cntr = iterator.Iterate(samplePointsX[i], samplePointsY[j], threshold);
					result[j * stride + i] = cntr;
				}
			}

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
