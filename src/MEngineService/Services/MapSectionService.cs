using MapSectionProviderLib;
using MEngineDataContracts;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
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

		private static MapSectionVectorsPool _mapSectionVectorsPool;
		private static MapSectionZVectorsPool _mapSectionZVectorsPool;
		private static MapSectionVectorProvider _mapSectionVectorProvider;

		private static readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		//private static int _sectionCntr;

		static MapSectionService()
		{

			var repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT, "MandelbrotProjects");
			_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;

			_mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionZVectorsPool = new MapSectionZVectorsPool(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionVectorProvider = new MapSectionVectorProvider(_mapSectionVectorsPool, _mapSectionZVectorsPool);

			_mapSectionPersistProcessor = new MapSectionPersistProcessor(_mapSectionAdapter, _mapSectionVectorProvider);

			//_sectionCntr = 0;
			//Console.WriteLine($"The MapSection Persist Processor has started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
			await Task.Delay(100);

			//var stringVals = MapSectionGenerator.GetStringVals(mapSectionRequest);
			//Debug.WriteLine($"The string vals are {stringVals[0]}, {stringVals[1]}, {stringVals[2]}, {stringVals[3]}.");

			//var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest, _mapSectionAdapter);

			////var idStr = string.IsNullOrEmpty(mapSectionResponse.MapSectionId) ? "new" : mapSectionResponse.MapSectionId;
			////if (++_sectionCntr % 10 == 0)
			////{
			////	Debug.WriteLine($"Adding MapSectionResponse with ID: {idStr} to the MapSection Persist Processor. Generated {_sectionCntr} Map Sections.");
			////}
			////_mapSectionPersistProcessor.AddWork(mapSectionResponse);

			//if (++_sectionCntr % 10 == 0)
			//{
			//	Debug.WriteLine($"Generated {_sectionCntr} Map Sections.");
			//}

			//mapSectionResponse.IncludeZValues = false;

			var mapSectionResponse = new MapSectionResponse(mapSectionRequest, isCancelled: true);

			return mapSectionResponse;
		}

		public Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest, CallContext context = default)
		{
			throw new NotImplementedException();
		}

		public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest, CallContext context = default)
		{
			throw new NotImplementedException();
		}
	}
}
