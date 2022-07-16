using MapSectionProviderLib;
using MEngineDataContracts;
using MSetRepo;
using MSS.Common;
using ProtoBuf.Grpc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		// TODO: Have the MapSectionService get the MongoDb connection string from the appsettings.json file.

		private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		private const int MONGO_DB_PORT = 27017;

		public static IMapSectionAdapter MapSectionAdapter { get; private set; }

		public static MapSectionPersistProcessor _mapSectionPersistProcessor;

		static MapSectionService()
		{
			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(MONGO_DB_SERVER, MONGO_DB_PORT);
			_mapSectionPersistProcessor = new MapSectionPersistProcessor(MapSectionAdapter);
			Console.WriteLine($"The MapSection Persist Processor has started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
			var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest);

			Debug.WriteLine($"Adding MapSectionResponse with ID: {mapSectionResponse.MapSectionId} to the MapSection Persist Processor. ");
			_mapSectionPersistProcessor.AddWork(mapSectionResponse);

			mapSectionResponse.IncludeZValues = false;
			return mapSectionResponse;
		}

		//public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CallContext context = default)
		//{
		//	var mapSectionResponse = MapSectionGenerator.GenerateMapSection(mapSectionRequest);
		//	return mapSectionResponse;
		//}
	}
}
