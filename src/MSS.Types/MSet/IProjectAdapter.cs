using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public interface IProjectAdapter
	{

		//public void InsertColorBandSet(ColorBandSet colorBandSet);

		//void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		//void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		//void UpdateColorBandSetDetails(ColorBandSet colorBandSet);

		//public void InsertJob(Job job);
		//void UpdateJobDetails(Job job);

		//void UpdateProjectName(ObjectId projectId, string name);
		//void UpdateProjectDescription(ObjectId projectId, string? description);
		//void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);

		//void CreatePoster(Poster poster);
		//void UpdatePoster(Poster poster);

		Project? CreateNewProject(string name, string? description, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets);
		void CreatePoster(Poster poster);
		Project? CreateProject(string name, string? description, ObjectId currentJobId);
		long? DeleteMapSectionsSince(DateTime lastSaved);
		void DeletePoster(ObjectId posterId);
		void DeleteProject(ObjectId projectId);
		int DeleteUnusedColorBandSets();
		void DropSubdivisionsAndMapSectionsCollections();
		IEnumerable<Job> GetAllJobsForProject(ObjectId projectId, IEnumerable<ColorBandSet> colorBandSets);
		IList<Poster> GetAllPosters();
		IEnumerable<IProjectInfo> GetAllProjectInfos();
		ColorBandSet? GetColorBandSet(string id);
		IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId);
		Job GetJob(ObjectId jobId);
		void InsertColorBandSet(ColorBandSet colorBandSet);
		void InsertJob(Job job);
		bool PosterExists(string name);
		bool ProjectExists(string name);
		bool TryGetPoster(ObjectId posterId, [MaybeNullWhen(false)] out Poster poster);
		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		bool TryGetProject(ObjectId projectId, [MaybeNullWhen(false)] out Project project);
		bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project);
		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);
		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		void UpdateJobDetails(Job job);
		void UpdatePoster(Poster poster);
		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateProjectName(ObjectId projectId, string name);

	}
}