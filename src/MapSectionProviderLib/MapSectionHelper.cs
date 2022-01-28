using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MapSectionProviderLib
{
	public static class MapSectionHelper
	{
		public static MapSectionRequest CreateRequest(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings, out RPoint mapPosition)
		{
			var subPos = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var spd);
			mapPosition = subPos.Translate(spd.Scale(blockPosition.Scale(subdivision.BlockSize)));

			var dtoMapper = new DtoMapper();
			var posForDataTransfer = dtoMapper.MapTo(mapPosition);
			var spdForDataTransfer = dtoMapper.MapTo(subdivision.SamplePointDelta);

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition,
				BlockSize = subdivision.BlockSize,
				Position = posForDataTransfer,
				SamplePointsDelta = spdForDataTransfer,
				MapCalcSettings = mapCalcSettings
			};

			return mapSectionRequest;
		}

	}
}
