using MEngineDataContracts;
using System.Threading.Tasks;

namespace MEngineClient
{
	public interface IMEngineClient
	{
		Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest);

		ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest);
	}
}