using MEngineDataContracts;
using ProtoBuf.Grpc;
using System.Threading.Tasks;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		public Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
            var mapSectionGenerator = new MapSectionGenerator();
            var mapSectionResponse = mapSectionGenerator.GenerateMapSection(mapSectionRequest);
            return Task.FromResult(mapSectionResponse);
        }
	}
}
