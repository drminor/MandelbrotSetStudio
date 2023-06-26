using MongoDB.Bson;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;


namespace MSS.Common
{
	public interface IMapSectionAdapter : IMapSectionDuplicator, IMapSectionDeleter
	{
		void CreateCollections();
		void DropMapSections();
		void DropMapSectionsAndSubdivisions();

		Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionVectors mapSectionVectors, CancellationToken ct);
		MapSectionResponse? GetMapSection(ObjectId mapSectionId, MapSectionVectors mapSectionVectors);
		MapSectionResponse? GetMapSection(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionVectors mapSectionVectors);

		ObjectId? GetMapSectionId(ObjectId subdivisionId, BigVector blockPosition);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);
		Task<long?> UpdateCountValuesAync(MapSectionResponse mapSectionResponse);

		Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse, BigVector blockPosition, bool isInverted);

		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct);
		Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<long?> UpdateZValuesAync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<long?> DeleteZValuesAync(ObjectId mapSectionId);

		bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [NotNullWhen(true)] out Subdivision? subdivision);
		Subdivision InsertSubdivision(Subdivision subdivision);

		IList<ObjectId> GetMapSectionIds(ObjectId jobId, JobOwnerType jobOwnerType);


	}

}
