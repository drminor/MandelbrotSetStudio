using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MSS.Common.MSetRepo
{
	public interface IMapSectionAdapter
	{
		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition, bool excludeZValues = false);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse);

		long? ClearMapSections(string subdivisionId);

		long? DeleteMapSectionsSince(DateTime lastSaved, bool overrideRecentGuard = false);

		//void AddCreatedDateToAllMapSections();

		bool TryGetSubdivision(RSize samplePointDelta, SizeInt blockSize, [MaybeNullWhen(false)] out Subdivision subdivision);
		void InsertSubdivision(Subdivision subdivision);
	}
}
