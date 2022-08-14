using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSS.Common.MSet
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
		int GetNumberOfDirtyJobs();

		//DateTime DateCreatedUtc { get; init; }
		//DateTime LastSavedUtc { get; }
		//DateTime LastAccessedUtc { get; init; }
		//DateTime LastUpdatedUtc { get; }

		List<Job> GetJobs();
		List<ColorBandSet> GetColorBandSets();

		JobTreePath? GetCurrentPath();

		JobTreePath? GetPath(ObjectId jobId);
		Job? GetJob(ObjectId jobId);
		Job? GetParent(Job job);
		List<Job>? GetJobAndDescendants(ObjectId jobId);

		bool RestoreBranch(ObjectId jobId);
		bool DeleteBranch(ObjectId jobId);
		void MarkAsSaved();

		JobTreeItem? SelectedViewItem { get; set; }
	}
}