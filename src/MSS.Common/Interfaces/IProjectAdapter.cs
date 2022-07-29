﻿using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	public interface IProjectAdapter
	{
		void CreateCollections();
		//void DropCollections();
		void WarmUp();

		Project? CreateProject(string name, string? description, IList<Job> jobs, IEnumerable<ColorBandSet> colorBandSets);
		IList<Job> GetAllJobsForProject(ObjectId projectId, IEnumerable<ColorBandSet> colorBandSets);
		IList<ObjectId> GetAllJobIdsForProject(ObjectId projectId);

		bool ProjectExists(string name);
		//bool TryGetProject(ObjectId projectId, [MaybeNullWhen(false)] out Project project);
		bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project);
		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateProjectName(ObjectId projectId, string name);
		bool DeleteProject(ObjectId projectId);
		IEnumerable<IProjectInfo> GetAllProjectInfos();

		Poster? CreatePoster(string name, string? description, ObjectId sourceJobId, IList<Job> jobs, IEnumerable<ColorBandSet> colorBandSets);
		IList<Poster> GetAllPosters();
		IList<Job> GetAllJobsForPoster(ObjectId posterId, IEnumerable<ColorBandSet> colorBandSets);
		IList<ObjectId> GetAllJobIdsForPoster(ObjectId posterId);
		IEnumerable<IPosterInfo> GetAllPosterInfos();

		bool PosterExists(string name);
		bool TryGetPoster(ObjectId posterId, [MaybeNullWhen(false)] out Poster poster);
		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		void UpdatePoster(Poster poster);
		bool DeletePoster(ObjectId posterId);


		Job GetJob(ObjectId jobId);
		void InsertJob(Job job);
		void UpdateJobDetails(Job job);
		bool DeleteJob(ObjectId jobId);

		void InsertColorBandSet(ColorBandSet colorBandSet);
		ColorBandSet? GetColorBandSet(string id);
		IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId);
		bool DeleteColorBandSet(ObjectId colorBandSetId);

		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);
		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		int DeleteUnusedColorBandSets();
	}
}