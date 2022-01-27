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
			var subdivionPosition = subdivision.Position;
			var samplePointDelta = subdivision.SamplePointDelta;

			RMapHelper.NormalizeInPlace(ref subdivionPosition, ref samplePointDelta);

			mapPosition = subdivionPosition.Translate(samplePointDelta.Scale(blockPosition.Scale(subdivision.BlockSize)));

			var dtoMapper = new DtoMapper();

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition,
				BlockSize = subdivision.BlockSize,
				Position = dtoMapper.MapTo(mapPosition),
				SamplePointsDelta = dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings
			};

			return mapSectionRequest;
		}

	}
}
