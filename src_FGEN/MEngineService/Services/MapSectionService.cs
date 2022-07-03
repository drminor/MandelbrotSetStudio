using MapSectionProviderLib;
using MEngineDataContracts;
using MSetRepo;
using MSS.Common;
using ProtoBuf.Grpc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		public static IMapSectionAdapter MapSectionAdapter { get; private set; }

		public static MapSectionPersistProcessor _mapSectionPersistProcessor;

		static MapSectionService()
		{
			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(MONGO_DB_CONN_STRING);
			_mapSectionPersistProcessor = new MapSectionPersistProcessor(MapSectionAdapter);
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
			var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest);

			var cpyWithNoZValues = mapSectionResponse.Clone(stripZValues: true);

			Debug.WriteLine($"Adding MapSectionResponse with ID: {mapSectionResponse.MapSectionId} to the MapSectionPersist Processor. ");
			_mapSectionPersistProcessor.AddWork(mapSectionResponse);

			return cpyWithNoZValues;
		}

		//public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CallContext context = default)
		//{
		//	var mapSectionResponse = MapSectionGenerator.GenerateMapSection(mapSectionRequest);
		//	return mapSectionResponse;
		//}
	}
}
