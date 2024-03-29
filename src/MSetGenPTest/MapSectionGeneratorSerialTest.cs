﻿using MEngineDataContracts;
using MSetGenP;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System.Numerics;

namespace MSetGenPTest
{
	public class MapSectionGeneratorSerialTest
	{
		[Fact]
		public void ScalarGeneratateSectionResponse()
		{
			var xPos = new long[] { 0, -414219082 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 67781838 };
			var precision = RMapConstants.DEFAULT_PRECISION;
			var extent = 128;
			var samplePointDelta = new RSize(1, 1, -36);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			var generatorScalar = new MapSectionGeneratorScalar();
			var reponse = generatorScalar.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		[Fact]
		public void ScalarGeneratateSectionResponseNearZero()
		{
			//Precision: 65, BP: X: -1, 0, Y: 0

			var xPos = new long[] { 0, -1 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 0 };
			var precision = RMapConstants.DEFAULT_PRECISION;
			var extent = 128;
			var samplePointDelta = new RSize(1, 1, -8);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			var generatorScalar = new MapSectionGeneratorScalar();
			var reponse = generatorScalar.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		[Fact]
		public void ScalarGeneratateSectionDirect()
		{
			//Precision: 65, BP: X: -1, 0, Y: 0

			var xPos = new long[] { 0, -1 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 0 };
			var precision = RMapConstants.DEFAULT_PRECISION;
			var extent = 128;
			var samplePointDelta = new RSize(1, 1, -8);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			request.Position = new RPointDto(new BigInteger[] { 4, 3 }, -1);

			var generatorScalar = new MapSectionGeneratorScalar();
			var reponse = generatorScalar.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		[Fact]
		public void VectorGeneratateSectionResponse()
		{
			var xPos = new long[] { 0, -414219082 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 67781838 };
			var precision = RMapConstants.DEFAULT_PRECISION;
			var extent = 128;
			var samplePointDelta = new RSize(1, 1, -36);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			var generatorVector = new MapSectionGeneratorVector();
			var reponse = generatorVector.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		[Fact]
		public void VectorGeneratateSectionResponseNearZero()
		{
			//Precision: 65, BP: X: -1, 0, Y: 0

			var xPos = new long[] { 0, -1 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 0 };
			var precision = RMapConstants.DEFAULT_PRECISION;
			var extent = 128;
			var samplePointDelta = new RSize(1, 1, -8);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			var generatorScalar = new MapSectionGeneratorVector();
			var reponse = generatorScalar.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		[Fact]
		public void VectorGeneratateSectionDirect()
		{
			//Precision: 65, BP: X: -1, 0, Y: 0

			var xPos = new long[] { 0, -1 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 0 };
			var precision = RMapConstants.DEFAULT_PRECISION;
			var extent = 128;
			var samplePointDelta = new RSize(1, 1, -8);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			request.Position = new RPointDto(new BigInteger[] { 4, 3 }, -1);

			var generatorScalar = new MapSectionGeneratorVector();
			var reponse = generatorScalar.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		//[Fact]
		//public void CallIntrincsSample()
		//{
		//	var simdSamples = new SimdSamples();

		//	var nums = Enumerable.Range(0, 21).ToArray();
		//	simdSamples.SumVectorized(nums);
		//}

		//[Fact]
		//public void CallVectorSample()
		//{
		//	var simdSamples = new SimdSamples();

		//	simdSamples.Sample();
		//}

		#region Support Methods

		private MapSectionRequest AssembleRequest(long[] xPos, long[] yPos, int precision, int extent, RSize samplePointDelta, MapCalcSettings mapCalcSettings)
		{
			var blockPosValues = new long[2][] {xPos, yPos};
			var blockPositionDto = new BigVectorDto(blockPosValues);

			var samplePointDeltaDto = new RSizeDto(samplePointDelta.Values, samplePointDelta.Exponent);
			var blockSize = new SizeInt(extent);
			var mapPositionDto = GetMapPosition(blockPositionDto, samplePointDeltaDto, blockSize);

			MapSectionRequest request = new MapSectionRequest();
			request.SubdivisionId = "TestA";
			request.BlockPosition = blockPositionDto;
			request.SamplePointDelta = samplePointDeltaDto;
			request.Position = mapPositionDto;
			request.Precision = precision;
			request.BlockSize = blockSize;
			request.MapCalcSettings = mapCalcSettings;

			request.HasEscapedFlags = new bool[blockSize.NumberOfCells];

			return request;
		}

		private RPointDto GetMapPosition(BigVectorDto blockPositionDto, RSizeDto samplePointDeltaDto, SizeInt blockSize)
		{
			var blockPosition = new DtoMapper().MapFrom(blockPositionDto);
			var samplePointDelta = new DtoMapper().MapFrom(samplePointDeltaDto);

			var result = GetMapPosition(blockPosition, samplePointDelta, blockSize);
			var resultDto = new DtoMapper().MapTo(result);

			return resultDto;
		}

		private RPoint GetMapPosition(BigVector blockPosition, RSize samplePointDelta, SizeInt blockSize)
		{
			// Multiply the blockPosition by the blockSize
			var numberOfSamplePointsFromSubOrigin = blockPosition.Scale(blockSize);

			// Convert sample points to map coordinates.
			var mapDistance = samplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);

			var result = new RPoint(mapDistance);

			return result;
		}

		#endregion


		/*

		SamplePointDelta: 1, -36

		XPos:
			Hi: 0
			Lo: -414219082

		YPos
			Hi: 0
			Lo: 67781838

		*/

	}
}
