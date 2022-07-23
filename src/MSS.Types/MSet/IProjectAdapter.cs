using MongoDB.Bson;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public interface IProjectAdapter
	{
		void CreateCollections();
		//void DropCollections();
		void WarmUp();

		Project? CreateProject(string name, string? description, IList<Job> jobs, IEnumerable<ColorBandSet> colorBandSets);
		IList<Job> GetAllJobsForProject(ObjectId projectId, IEnumerable<ColorBandSet> colorBandSets);
		IList<ObjectId> GetAllJobsIdsForProject(ObjectId projectId);


		IList<Poster> GetAllPosters();
		bool PosterExists(string name);
		bool TryGetPoster(ObjectId posterId, [MaybeNullWhen(false)] out Poster poster);
		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		void CreatePoster(Poster poster);
		void UpdatePoster(Poster poster);
		void DeletePoster(ObjectId posterId);

		bool ProjectExists(string name);
		bool TryGetProject(ObjectId projectId, [MaybeNullWhen(false)] out Project project);
		bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project);
		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateProjectName(ObjectId projectId, string name);
		bool DeleteProject(ObjectId projectId);
		IEnumerable<IProjectInfo> GetAllProjectInfos();

		Job GetJob(ObjectId jobId);
		void InsertJob(Job job);
		void UpdateJobDetails(Job job);

		void InsertColorBandSet(ColorBandSet colorBandSet);
		ColorBandSet? GetColorBandSet(string id);
		IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId);

		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);
		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		int DeleteUnusedColorBandSets();
	}
}