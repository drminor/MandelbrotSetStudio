using MEngineDataContracts;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMEngineClient
	{
		string EndPointAddress { get; }

		Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest);
	}
}