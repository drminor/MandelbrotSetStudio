using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionAdapter
	{
		//MapSectionResponse? GetMapSection(string mapSectionId);
		//Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId);

		MapSectionResponse? GetMapSection(string subdivisionId, BigVectorDto blockPosition);

		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse);

		long? ClearMapSections(string subdivisionId);

		//void AddCreatedDateToAllMapSections();
	}
}
