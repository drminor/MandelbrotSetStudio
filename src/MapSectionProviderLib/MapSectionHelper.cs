using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MapSectionProviderLib
{
	public static class MapSectionHelper
	{
		/// <summary>
		/// Calculate the map position of the section being requested 
		/// and prepare a MapSectionRequest
		/// </summary>
		/// <param name="subdivision"></param>
		/// <param name="blockPosition"></param>
		/// <param name="inverted"></param>
		/// <param name="mapCalcSettings"></param>
		/// <param name="mapPosition"></param>
		/// <returns></returns>
		public static MapSectionRequest CreateRequest(Subdivision subdivision, BigVector blockPosition, bool inverted, MapCalcSettings mapCalcSettings, out RPoint mapPosition)
		{
			mapPosition = GetMapPosition(subdivision, blockPosition);

			var dtoMapper = new DtoMapper();
			var blockPosForDataTransfer = dtoMapper.MapTo(blockPosition);
			var posForDataTransfer = dtoMapper.MapTo(mapPosition);
			var spdForDataTransfer = dtoMapper.MapTo(subdivision.SamplePointDelta);

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosForDataTransfer,
				BlockSize = subdivision.BlockSize,
				Position = posForDataTransfer,
				SamplePointsDelta = spdForDataTransfer,
				MapCalcSettings = mapCalcSettings,
				Inverted = inverted
			};

			return mapSectionRequest;
		}

		public static RPoint GetMapPosition(Subdivision subdivision, BigVector blockPosition)
		{
			var nrmSubdivionPosition = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var nrmSamplePointDelta);

			// Multiply the blockPosition by the blockSize
			var numberOfSamplePointsFromSubOrigin = blockPosition.Scale(subdivision.BlockSize); // TODO: Rewrite to scale blockOffset (vector) by blockSize (size) to get a new vector.

			// Convert sample points to map coordinates.
			var mapDistance = nrmSamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin); // TODO: scale vector by size to get new vector

			// Add the map distance to the sub division origin
			var mapPosition = nrmSubdivionPosition.Translate(mapDistance); // Translate Point by vector.

			return mapPosition;
		}

	}
}
