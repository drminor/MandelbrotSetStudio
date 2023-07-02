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
		void CreateIndexes();
		void DropMapSections();
		void DropMapSectionsAndSubdivisions();

		Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionVectors mapSectionVectors, CancellationToken ct);
		MapSectionResponse? GetMapSection(ObjectId mapSectionId, MapSectionVectors mapSectionVectors);
		MapSectionResponse? GetMapSection(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionVectors mapSectionVectors);

		ObjectId? GetMapSectionId(ObjectId subdivisionId, BigVector blockPosition);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);
		Task<long?> UpdateCountValuesAync(MapSectionResponse mapSectionResponse);

		//Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse, bool isInverted);
		Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse, bool isInverted, JobOwnerType jobOwnerType, JobType jobType);

		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct);
		Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<long?> UpdateZValuesAync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<long?> DeleteZValuesAync(ObjectId mapSectionId);

		bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [NotNullWhen(true)] out Subdivision? subdivision);
		Subdivision InsertSubdivision(Subdivision subdivision);

		IList<ObjectId> GetMapSectionIds(ObjectId jobId, JobOwnerType jobOwnerType);
		bool InsertIfNotFoundJobMapSection(ObjectId mapSectionId, ObjectId subdivisionId, ObjectId originalSourceSubdivisionId, ObjectId jobId, JobOwnerType jobOwnerType, bool isInverted, bool refIsHard, out ObjectId jobMapSectionId);

		IEnumerable<ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> GetMapSectionAndSubdivisionIdsForAllJobMapSections();
		IEnumerable<ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> GetJobAndSubdivisionIdsForAllJobMapSections();

		ObjectId? GetSubdivisionId(ObjectId mapSectionId);

		long DeleteJobMapSectionsInList(IEnumerable<ObjectId> jobMapSectionIds);

		IEnumerable<ObjectId> GetAllMapSectionIds();

		IEnumerable<ObjectId> GetJobMapSectionIds(IEnumerable<ObjectId> mapSectionIds);

		long DeleteMapSectionsInList(IList<ObjectId> mapSectionIds);

		IEnumerable<Subdivision> GetAllSubdivisions();
		IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsForAllJobs();
		IEnumerable<ObjectId> GetSubdivisionIdsForAllJobs();

		IEnumerable<ObjectId> GetSubdivisionIdsForAllMapSections();
		long DeleteSubdivisionsInList(IList<ObjectId> subdivisionIds);

	}

}
