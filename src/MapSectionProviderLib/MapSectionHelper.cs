using MEngineDataContracts;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;

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
				throw new NotSupportedException("No support yet for adjusting exponents when the position's exponent is less than the sample size's.");
			}
			else
			{
				subPosition = subdivision.Position;
			}

			var position = new RPoint(blockPosition, subPosition.Exponent).Scale(subdivision.BlockSize).Scale(subdivision.SamplePointDelta).Translate(subPosition);
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
