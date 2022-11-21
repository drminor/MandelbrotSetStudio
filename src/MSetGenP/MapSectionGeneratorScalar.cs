﻿using MEngineDataContracts;
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
			var precision = mapSectionRequest.Precision; // + 20;

			var smxMathHelper = new SmxMathHelper(precision);

			var startingCx = smxMathHelper.CreateSmxFromDto(mapPositionDto.X, mapPositionDto.Exponent, precision);
			var startingCy = smxMathHelper.CreateSmxFromDto(mapPositionDto.Y, mapPositionDto.Exponent, precision);
			var delta = smxMathHelper.CreateSmxFromDto(samplePointDeltaDto.Width, samplePointDeltaDto.Exponent, precision);

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			var counts = GenerateMapSection(smxMathHelper, startingCx, startingCy, delta, blockSize, targetIterations);
			var doneFlags = CalculateTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues: null);

			return result;
		}

		//private ushort[] GenerateMapSection2(Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations)
		//{
		//	var s1 = RValueHelper.ConvertToString(startingCx.GetRValue());
		//	var s2 = RValueHelper.ConvertToString(startingCy.GetRValue());
		//	var s3 = RValueHelper.ConvertToString(delta.GetRValue());

		//	Debug.WriteLine($"Value of C at origin: real: {s1}, imaginary: {s2}. Delta: {s3}. Precision: {startingCx.Precision}");

		//	var result = new ushort[blockSize.NumberOfCells];

		//	var stride = blockSize.Width;
		//	var samplePointOffsets = BuildSamplePointOffsets(delta, stride);

		//	ReportExponents(samplePointOffsets);

		//	var samplePointsX = BuildSamplePoints(startingCx, samplePointOffsets);
		//	var samplePointsY = BuildSamplePoints(startingCy, samplePointOffsets);

		//	var spx = new FPValues(samplePointsX);
		//	var spy = new FPValues(samplePointsY);	

		//	var iterator = new IteratorScalar(targetIterations);

		//	for (int j = 0; j < samplePointsY.Length; j++)
		//	{
		//		for (int i = 0; i < samplePointsX.Length; i++)
		//		{
		//			//var cntr = iterator.Iterate(samplePointsX[i], samplePointsY[j]);
		//			var cntr = iterator.Iterate(spx, i, spy, j);
		//			result[j * stride + i] = cntr;
		//		}
		//	}

		//	return result;
		//}

		private ushort[] GenerateMapSection(SmxMathHelper smxMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations)
		{
			var s1 = RValueHelper.ConvertToString(startingCx.GetRValue());
			var s2 = RValueHelper.ConvertToString(startingCy.GetRValue());
			var s3 = RValueHelper.ConvertToString(delta.GetRValue());

			Debug.WriteLine($"Value of C at origin: real: {s1}, imaginary: {s2}. Delta: {s3}. Precision: {startingCx.Precision}");

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
					var cntr = iterator.Iterate(samplePointsX[i], samplePointsY[j]);
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
