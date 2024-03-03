using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	public interface IProjectAdapter
	{
		void CreateCollections();
		//void DropCollections();
		bool ProjectCollectionIsEmpty();

		//Project? CreateProject(string name, string? description, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets);
		Project? CreateProject(string name, string? description, List<Job> jobs, List<ColorBandSet> colorBandSets, 
			Dictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration);

		List<Job> GetAllJobsForProject(ObjectId projectId, IEnumerable<ColorBandSet> colorBandSets);
		List<ObjectId> GetAllJobIdsForProject(ObjectId projectId);

		bool ProjectExists(string name, [MaybeNullWhen(false)] out ObjectId projectId);
		bool ProjectExists(ObjectId projectId);
		//bool TryGetProject(ObjectId projectId, [MaybeNullWhen(false)] out Project project);
		bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project);

		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateProjectName(ObjectId projectId, string name);
		void UpdateProjectTargetIterationMap(ObjectId projectId, DateTime lastAccessedUtc, TargetIterationColorMapRecord[] targetIterationColorMapRecords);

		bool DeleteProject(ObjectId projectId);
		IEnumerable<IProjectInfo> GetAllProjectInfos();

		Poster? CreatePoster(string name, string? description, SizeDbl posterSize, ObjectId sourceJobId, List<Job> jobs, List<ColorBandSet> colorBandSets,
			Dictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration);

		List<Poster> GetAllPosters();
		List<Job> GetAllJobsForPoster(ObjectId posterId, IEnumerable<ColorBandSet> colorBandSets);
		IEnumerable<ObjectId> GetAllJobIdsForPoster(ObjectId posterId);

		bool PosterExists(string name, [MaybeNullWhen(false)] out ObjectId posterId);
		bool PosterExists(ObjectId posterId);

		bool TryGetPoster(ObjectId posterId, [MaybeNullWhen(false)] out Poster poster);
		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);

		void UpdatePosterCurrentJobId(ObjectId posterId, ObjectId? currentJobId);
		void UpdatePosterDescription(ObjectId posterId, string name);
		void UpdatePosterName(ObjectId posterId, string name);
		
		void UpdatePosterMapArea(Poster poster);
		bool DeletePoster(ObjectId posterId);
		IEnumerable<IPosterInfo> GetAllPosterInfos();

		Job GetJob(ObjectId jobId);
		ObjectId InsertJob(Job job);
		void UpdateJobDetails(Job job);
		bool DeleteJob(ObjectId jobId);

		void InsertColorBandSet(ColorBandSet colorBandSet);
		ColorBandSet? GetColorBandSet(string id);
		IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId);
		//long DeleteColorBandSetsForProject(ObjectId projectId);
		bool DeleteColorBandSet(ObjectId colorBandSetId);

		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);
		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		int DeleteUnusedColorBandSets();

		ObjectId? GetSubdivisionId(ObjectId jobId);
		//(ObjectId, MapCenterAndDelta)? GetSubdivisionIdAndMapAreaInfo(ObjectId jobId);

		IEnumerable<ValueTuple<ObjectId, ObjectId, OwnerType>> GetJobAndOwnerIdsWithJobOwnerType();

		IEnumerable<ObjectId> GetAllProjectIds();
		IEnumerable<ObjectId> GetAllPosterIds();

		void UpdateJobOwnerType(ObjectId jobId, OwnerType jobOwnerType);

		IEnumerable<JobInfo> GetJobInfosForOwner(ObjectId ownerId, ObjectId currentJobId);
		IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsForOwner(ObjectId ownerId);

		void UpdatePosterDisplayPositionAndZoom(Poster poster);

		bool ColorBandSetExists(string name);
		bool TryGetColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet);
		IEnumerable<ColorBandSetInfo> GetAllColorBandSetInfosForProject(ObjectId projectId);
		ColorBandSetInfo? GetColorBandSetInfo(ObjectId id);
	}
}