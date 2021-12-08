using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionProvider2 : IMapSectionProvider
	{
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;

		public MapSectionProvider2(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
		{

			try
			{
				var mapSectionResponse = await _mapSectionRepo.GetMapSectionAsync(subdivision.Id.ToString(), blockPosition);

				if (mapSectionResponse is null)
				{
					Debug.WriteLine($"Generating MapSection for block: {blockPosition}.");
					var mapSectionRequest = MapSectionHelper.CreateRequest(subdivision, blockPosition, mapCalcSettings);
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

	}
}
