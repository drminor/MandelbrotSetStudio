using MapSectionGeneratorLib;
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

		private static readonly IMapSectionAdapter _mapSectionAdapter;
		private static readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private static int _sectionCntr;

		static MapSectionService()
		{
			_mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(MONGO_DB_SERVER, MONGO_DB_PORT);
			_mapSectionPersistProcessor = new MapSectionPersistProcessor(_mapSectionAdapter);
			_sectionCntr = 0;
			Console.WriteLine($"The MapSection Persist Processor has started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
			var stringVals = MapSectionGenerator.GetStringVals(mapSectionRequest);

			Debug.WriteLine($"The string vals are {stringVals[0]}, {stringVals[1]}, {stringVals[2]}, {stringVals[3]}.");

			var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest, _mapSectionAdapter);

			var idStr = string.IsNullOrEmpty(mapSectionResponse.MapSectionId) ? "new" : mapSectionResponse.MapSectionId;

			Debug.WriteLine($"Adding MapSectionResponse with ID: {idStr} to the MapSection Persist Processor. ");
			_mapSectionPersistProcessor.AddWork(mapSectionResponse);

			mapSectionResponse.IncludeZValues = false;

			Console.WriteLine($"Returned {++_sectionCntr} sections.");

			return mapSectionResponse;
		}

	}
}
