using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Types.DataTransferObjects;
using System;
using System.Threading.Tasks;

namespace MSS.Common.MSetRepo
{
	public interface IMapSectionAdapter
	{
		//MapSectionResponse? GetMapSection(string subdivisionId, BigVectorDto blockPosition, bool returnOnlyCounts = false);

		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition, bool excludeZValues = false);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse);


		long? ClearMapSections(string subdivisionId);

		long? DeleteMapSectionsSince(DateTime lastSaved, bool overrideRecentGuard = false);

		//void AddCreatedDateToAllMapSections();
	}
}
