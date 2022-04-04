using MEngineDataContracts;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionAdapter
	{

		MapSectionResponse? GetMapSection(string mapSectionId);

		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition);
		Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId);

		Task<string> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse);

		long? ClearMapSections(string subdivisionId);
	}
}
