using MEngineDataContracts;
using MSetGenP;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;

namespace MSetGenPTest
{
	public class MapSectionGeneratorSerialTest
	{
		[Fact]
		public void SimpleGeneratateSectionResponse()
		{
			var xPos = new long[] { 0, -414219082 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
			var yPos = new long[] { 0, 67781838 };
			var precision = 55;
			var extent = 16;
			var samplePointDelta = new RSize(1, 1, -36);
			var mapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, requestsPerJob: 4);
			var request = AssembleRequest(xPos, yPos, precision, extent, samplePointDelta, mapCalcSettings);

			var reponse = new MapSectionGeneratorScalar().GenerateMapSection(request);

			Assert.NotNull(reponse);
		}

		//[Fact]
		//public void TrimLeadingZeros_FromZeroValuedSmx_Returns_Zero()
		//{
		//	var mantissa = new ulong[] { 0 };

		//	var trimmedMantissa = SmxMathHelper.TrimLeadingZeros(mantissa);

		//	Assert.Equal(1, trimmedMantissa.Length);
		//}

		//[Fact]
		//public void TrimLeadingZeros_FromSmxWithOneNonZeroDigit_Returns_Same()
		//{
		//	var mantissa = new ulong[] { 1 };

		//	var trimmedMantissa = SmxMathHelper.TrimLeadingZeros(mantissa);

		//	Assert.True(trimmedMantissa.Length == 1 && trimmedMantissa[0] == 1);
		//}

		#region Support Methods

		//private MapSectionRequest BuildTestRequest()
		//{
		//	var xPos = new long[] { 0, -414219082 }; // Big-Endian, MSB first  // TODO: Update to use Little-Endian
		//	var yPos = new long[] { 0, 67781838 };
		//	var precision = 55;

		//	var samplePointDelta = new RSize(1, 1, -36);
		//	var mapCalcSettings = new MapCalcSettings(targetIterations: 400, threshold: 4, requestsPerJob: 4);

		//	var result = AssembleRequest(xPos, yPos, precision, samplePointDelta, mapCalcSettings);

		//	return result;
		//}

		private MapSectionRequest AssembleRequest(long[] xPos, long[] yPos, int precision, int extent, RSize samplePointDelta, MapCalcSettings mapCalcSettings)
		{
			var blockPosValues = new long[2][] {xPos, yPos};
			var blockPositionDto = new BigVectorDto(blockPosValues);

			var samplePointDeltaDto = new RSizeDto(samplePointDelta.Values, samplePointDelta.Exponent);
			var blockSize = new SizeInt(extent, extent);
			var mapPositionDto = GetMapPosition(blockPositionDto, samplePointDeltaDto, blockSize);

			MapSectionRequest request = new MapSectionRequest();
			request.SubdivisionId = "TestA";
			request.BlockPosition = blockPositionDto;
			request.SamplePointDelta = samplePointDeltaDto;
			request.Position = mapPositionDto;
			request.Precision = precision;
			request.BlockSize = blockSize;
			request.MapCalcSettings = mapCalcSettings;

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
