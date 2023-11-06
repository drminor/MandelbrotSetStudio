using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
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

		public MapSectionReaderWriters GetNewMapSectionReaderWriters();

		Task<MapSectionBytes?> GetMapSectionBytesAsync(ObjectId subdivisionId, VectorLong blockPosition, CancellationToken ct);
		MapSectionBytes? GetMapSectionBytes(ObjectId subdivisionId, VectorLong blockPosition, MapSectionReaderWriter mapSectionReaderWriter);

		ObjectId? GetMapSectionId(ObjectId subdivisionId, VectorLong blockPosition);

		ObjectId? SaveMapSection(MapSectionResponse mapSectionResponse);
		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		long? UpdateCountValues(MapSectionResponse mapSectionResponse);
		Task<long?> UpdateCountValuesAync(MapSectionResponse mapSectionResponse);

		ObjectId? SaveJobMapSection(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType);
		Task<ObjectId?> SaveJobMapSectionAsync(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType);

		bool DoesMapSectionZValuesExist(ObjectId mapSectionId, CancellationToken ct);
		Task<bool> DoesMapSectionZValuesExistAsync(ObjectId mapSectionId, CancellationToken ct);

		ZValues? GetMapSectionZValues(ObjectId mapSectionId);
		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct);

		ObjectId? SaveMapSectionZValues(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);

		long? UpdateZValues(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<long?> UpdateZValuesAync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);

		long? DeleteZValues(ObjectId mapSectionId);
		Task<long?> DeleteZValuesAync(ObjectId mapSectionId);

		bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [NotNullWhen(true)] out Subdivision? subdivision);
		Subdivision InsertSubdivision(Subdivision subdivision);

		IList<ObjectId> GetMapSectionIds(ObjectId jobId);
		
		bool InsertIfNotFoundJobMapSection(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType, out ObjectId jobMapSectionId);

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

		long GetSizeOfCollectionInMB();

		void UpdateJobMapSectionSubdivisionIds(ObjectId jobMapSectionId, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId);

		IEnumerable<ValueTuple<ObjectId, DateTime, ObjectId>> GetMapSectionCreationDatesAndSubIds(IEnumerable<ObjectId> mapSectionIds);

		long? DeleteMapSectionsForJobHavingJobTypes(ObjectId jobId, JobType[] jobTypes);

	}

}
