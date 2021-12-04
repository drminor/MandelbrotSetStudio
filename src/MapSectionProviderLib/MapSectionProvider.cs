using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using System;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionProvider
	{
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;

		public MapSectionProvider(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			return await _mEngineClient.GenerateMapSectionAsync(mapSectionRequest);
		}

	}
}
