﻿using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MapSectionProviderLib
{
	public class MapSectionHelper
	{
		private readonly DtoMapper _dtoMapper;

		public MapSectionHelper(DtoMapper dtoMapper)
		{
			_dtoMapper = dtoMapper;
		}

		/// <summary>
		/// Calculate the map position of the section being requested 
		/// and prepare a MapSectionRequest
		/// </summary>
		/// <param name="subdivision"></param>
		/// <param name="repokPosition"></param>
		/// <param name="isInverted"></param>
		/// <param name="mapCalcSettings"></param>
		/// <param name="mapPosition"></param>
		/// <returns></returns>
		public MapSectionRequest CreateRequest(Subdivision subdivision, BigVector repokPosition, bool isInverted, MapCalcSettings mapCalcSettings)
		{
			var mapPosition = GetMapPosition(subdivision, repokPosition);
			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = _dtoMapper.MapTo(repokPosition),
				BlockSize = subdivision.BlockSize,
				Position = _dtoMapper.MapTo(mapPosition),
				SamplePointsDelta = _dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings,
				IsInverted = isInverted
			};

			return mapSectionRequest;
		}

		public RPoint GetMapPosition(Subdivision subdivision, BigVector blockPosition)
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
