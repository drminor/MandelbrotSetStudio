using MEngineDataContracts;
using MSS.Types;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionRepo
	{

		MapSectionResponse? GetMapSection(string mapSectionId);

		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, RVector blockPosition);
		Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId);

		Task<string> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		long? ClearMapSections(string subdivisionId);
	}
}
