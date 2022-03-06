using MEngineDataContracts;
using MSS.Types;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionAdapter
	{

		MapSectionResponse? GetMapSection(string mapSectionId);

		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVector blockPosition);
		Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId);

		Task<string> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		long? ClearMapSections(string subdivisionId);
	}
}
