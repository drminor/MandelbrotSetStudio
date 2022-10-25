using MEngineDataContracts;
using MSetGenP;
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
		public void SimpleGeneratateSectionResponse()
		{
			var request = BuildTestRequest();
			var reponse = MapSectionGeneratorSerial.GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		private MapSectionRequest BuildTestRequest()
		{
			var xPos = new long[] { 0, -414219082 }; // Big-Endian  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 67781838 };

			var blockPosValues = new long[2][];
			blockPosValues[0] = xPos;
			blockPosValues[1] = yPos;

			var blockPositionDto = new BigVectorDto(blockPosValues);
			var samplePointDeltaDto = new RSizeDto(new BigInteger[] { 1, 1 }, -36);
			var blockSize = new SizeInt(128, 128);
			var mapPositionDto = GetMapPosition(blockPositionDto, samplePointDeltaDto, blockSize);

			MapSectionRequest request = new MapSectionRequest();
			request.SubdivisionId = "TestA";
			request.BlockPosition = blockPositionDto;
			request.SamplePointDelta = samplePointDeltaDto;
			request.Position = mapPositionDto;
			request.BlockSize = blockSize;
			request.MapCalcSettings = new MapCalcSettings(targetIterations: 400, threshold: 4, requestsPerJob: 4);

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
