using MEngineDataContracts;
using System.Threading.Tasks;

namespace MEngineClient
{
	public interface IMEngineClient
	{
		string EndPointAddress { get; }

		//Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest);
		ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest);
	}
}