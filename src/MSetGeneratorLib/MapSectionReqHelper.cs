using MEngineDataContracts;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetGeneratorLib
{
	internal static class MapSectionReqHelper
	{
		public static MapSectionRequestStruct GetRequestStruct(MapSectionRequest mapSectionRequest)
		{
			MapSectionRequestStruct result = new MapSectionRequestStruct();

			result.subdivisionId = mapSectionRequest.SubdivisionId;

			// BlockPosition

			var blockPositionLongs = BigIntegerHelper.ToLongsDeprecated(mapSectionRequest.BlockPosition.Values);

			result.blockPositionX = blockPositionLongs[0];
			result.blockPositionY = blockPositionLongs[1];

			// RPointDto Position

			var positionLongs = BigIntegerHelper.ToLongsDeprecated(mapSectionRequest.MapPosition.Values);

			result.positionX = positionLongs[0];
			result.positionY = positionLongs[1];
			result.positionExponent = mapSectionRequest.MapPosition.Exponent;
			result.positionPrecision = mapSectionRequest.Precision;

			// BlockSize
			result.blockSizeWidth = mapSectionRequest.BlockSize.Width;
			result.blockSizeHeight = mapSectionRequest.BlockSize.Height;

			// RSizeDto SamplePointDelta;

			var spdLongs = BigIntegerHelper.ToLongsDeprecated(mapSectionRequest.SamplePointDelta.Values);

			result.samplePointDeltaWidth = spdLongs[0];
			result.samplePointDeltaHeight = spdLongs[1];
			result.samplePointDeltaExponent = mapSectionRequest.SamplePointDelta.Exponent;

			// MapCalcSettings
			result.maxIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			result.threshold = mapSectionRequest.MapCalcSettings.Threshold;
			result.iterationsPerStep = mapSectionRequest.MapCalcSettings.RequestsPerJob;

			return result;
		}

	}
}
