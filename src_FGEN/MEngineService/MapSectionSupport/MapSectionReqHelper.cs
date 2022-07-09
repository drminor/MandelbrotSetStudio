using MEngineDataContracts;

namespace MEngineService
{
	internal static class MapSectionReqHelper
	{
		public static MapSectionRequestStruct GetRequestStruct(MapSectionRequest mapSectionRequest)
		{
			MapSectionRequestStruct result = new MapSectionRequestStruct();

			result.subdivisionId = mapSectionRequest.SubdivisionId;

			// BlockPosition
			result.blockPositionX = mapSectionRequest.BlockPosition.X;
			result.blockPositionY = mapSectionRequest.BlockPosition.Y;

			// RPointDto Position
			result.positionX = mapSectionRequest.Position.X;
			result.positionY = mapSectionRequest.Position.Y;
			result.positionExponent = mapSectionRequest.Position.Exponent;

			// BlockSize
			result.blockSizeWidth = mapSectionRequest.BlockSize.Width;
			result.blockSizeHeight = mapSectionRequest.BlockSize.Height;

			// RSizeDto SamplePointsDelta;
			result.samplePointDeltaWidth = mapSectionRequest.SamplePointsDelta.Width;
			result.samplePointDeltaHeight = mapSectionRequest.SamplePointsDelta.Height;
			result.samplePointDeltaExponent = mapSectionRequest.SamplePointsDelta.Exponent;

			// MapCalcSettings
			result.maxIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			result.threshold = mapSectionRequest.MapCalcSettings.Threshold;
			result.iterationsPerStep = mapSectionRequest.MapCalcSettings.RequestsPerJob;

			return result;
		}

	}
}
