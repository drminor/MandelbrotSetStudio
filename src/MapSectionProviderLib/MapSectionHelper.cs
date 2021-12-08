using MEngineDataContracts;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MapSectionProviderLib
{
	public static class MapSectionHelper
	{
		public static MapSectionRequest CreateRequest(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
		{
			RPoint subPosition;

			if (subdivision.Position.Exponent > subdivision.SamplePointDelta.Exponent)
			{
				int diff = subdivision.Position.Exponent - subdivision.SamplePointDelta.Exponent;
				subPosition = new RPoint(subdivision.Position.X * diff, subdivision.Position.Y * diff, subdivision.SamplePointDelta.Exponent);
			}
			else if (subdivision.Position.Exponent < subdivision.SamplePointDelta.Exponent)
			{
				subPosition = subdivision.Position;
			}
			else
			{
				subPosition = subdivision.Position;
			}

			var position = new RPoint(
				subPosition.X + blockPosition.X * subdivision.SamplePointDelta.Width * subdivision.BlockSize.Width,
				subPosition.Y + blockPosition.Y * subdivision.SamplePointDelta.Height * subdivision.BlockSize.Height,
				subdivision.SamplePointDelta.Exponent);

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
