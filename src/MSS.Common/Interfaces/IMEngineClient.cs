using MEngineDataContracts;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMEngineClient
	{
		string EndPointAddress { get; }

		//ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest);

		Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest);

		//MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest);
	}
}