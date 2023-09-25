﻿using MongoDB.Bson;
using MSS.Types;
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

		Task<MapSectionBytes?> GetMapSectionBytesAsync(ObjectId subdivisionId, MapBlockOffset blockPosition, CancellationToken ct);
		MapSectionBytes? GetMapSectionBytes(ObjectId subdivisionId, MapBlockOffset blockPosition);

		ObjectId? GetMapSectionId(ObjectId subdivisionId, MapBlockOffset blockPosition);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);
		Task<long?> UpdateCountValuesAync(MapSectionResponse mapSectionResponse);

		Task<ObjectId?> SaveJobMapSectionAsync(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType);

		Task<bool> DoesMapSectionZValuesExistAsync(ObjectId mapSectionId, CancellationToken ct);
		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct);
		Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
		Task<long?> UpdateZValuesAync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId);
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
