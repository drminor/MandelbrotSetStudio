using MEngineDataContracts;
using ProtoBuf.Grpc;
using System.Threading.Tasks;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		public Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
            var mapSectionResponse = MapSectionGenerator.GenerateMapSection(mapSectionRequest);

            return Task.FromResult(mapSectionResponse);
        }

		public ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest, CallContext context = default)
		{
			var mapSectionResponse = MapSectionGenerator.GenerateMapSection(mapSectionRequest);

			return new ValueTask<MapSectionResponse>(mapSectionResponse);
		}

		//ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest, CallContext context = default);
	}
}
