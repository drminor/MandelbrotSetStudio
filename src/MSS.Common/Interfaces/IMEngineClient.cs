using MEngineDataContracts;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMEngineClient
	{
		string EndPointAddress { get; }

		// True if running on the same machine as the Explorer program.
		bool IsLocal { get; }

		Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest);

		MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest);

	}
}