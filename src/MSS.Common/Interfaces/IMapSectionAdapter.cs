using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionAdapter
	{
		Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, bool includeZValues);

		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse);

		long? ClearMapSections(ObjectId subdivisionId);

		long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false);

		//void AddCreatedDateToAllMapSections();

		bool TryGetSubdivision(RSize samplePointDelta, SizeInt blockSize, [MaybeNullWhen(false)] out Subdivision subdivision);
		void InsertSubdivision(Subdivision subdivision);
	}
}
