using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSS.Common
{
	public interface IJobOwner
	{
		ObjectId Id { get; init; }
		string Name { get; set; }
		string? Description { get; set; }

		ObservableCollection<JobTreeItem>? JobItems { get; }

		Job CurrentJob { get; set; }
		ObjectId CurrentColorBandSetId { get; }

		bool OnFile { get; }
		bool IsDirty { get; }
		bool IsCurrentJobIdChanged { get; }

		//DateTime DateCreatedUtc { get; init; }
		//DateTime LastSavedUtc { get; }
		//DateTime LastAccessedUtc { get; init; }
		//DateTime LastUpdatedUtc { get; }

		List<Job> GetJobs();
		List<ColorBandSet> GetColorBandSets();

		List<JobTreeItem>? GetCurrentPath();
		Job? GetJob(ObjectId jobId);
		List<Job>? GetJobAndDescendants(ObjectId jobId);
		Job? GetParent(Job job);
		List<JobTreeItem>? GetPath(ObjectId jobId);

		bool RestoreBranch(ObjectId jobId);
		bool DeleteBranch(ObjectId jobId);
		void MarkAsSaved();
	}
}