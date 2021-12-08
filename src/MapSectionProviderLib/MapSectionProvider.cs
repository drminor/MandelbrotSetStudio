﻿using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionProvider
	{
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;
		private readonly DtoMapper _dtoMapper;

		public MapSectionProvider(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_dtoMapper = new DtoMapper();
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
		{
			//var x = _mapSectionRepo.GetMapSection("61b006afff54dd8025814e9b");

			try
			{
				var mapSectionResponse = await _mapSectionRepo.GetMapSectionAsync(subdivision.Id.ToString(), blockPosition);

				if (mapSectionResponse is null)
				{
					Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
					var mapSectionRequest = CreateRequest(subdivision, blockPosition, mapCalcSettings);
					mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(mapSectionRequest);
					var mapSectionId = await _mapSectionRepo.SaveMapSectionAsync(mapSectionResponse);

					mapSectionResponse.MapSectionId = mapSectionId;
				}

				return mapSectionResponse;
			} 
			catch (Exception e)
			{
				Debug.WriteLine($"Got Exception: {e}.");
				throw;
			}
		}

		private MapSectionRequest CreateRequest(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
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

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition,
				BlockSize = subdivision.BlockSize,
				Position = _dtoMapper.MapTo(position),
				SamplePointsDelta = _dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings
			};

			return mapSectionRequest;

		}

	}
}
