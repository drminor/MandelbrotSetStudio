using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MapSectionProviderLib
{
	public static class MapSectionHelper
	{
		public static MapSectionRequest CreateRequest(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
		{
			var subdivionsPosition = subdivision.Position;
			var samplePointDelta = subdivision.SamplePointDelta;

			RMapHelper.NormalizeInPlace(ref subdivionsPosition, ref samplePointDelta);

			var position = subdivionsPosition.Translate(samplePointDelta.Scale(blockPosition.Scale(subdivision.BlockSize)));

			var dtoMapper = new DtoMapper();

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition,
				BlockSize = subdivision.BlockSize,
				Position = dtoMapper.MapTo(position),
				SamplePointsDelta = dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings
			};

			return mapSectionRequest;
		}

	}
}
