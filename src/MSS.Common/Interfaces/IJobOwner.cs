using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace MSS.Common.MSet
{
	using JobPathType = ITreePath<JobTreeNode, Job>;

	public interface IJobOwner
	{
		ObjectId Id { get; init; }
		JobOwnerType JobOwnerType { get; }	
		string Name { get; set; }
		string? Description { get; set; }

		ObservableCollection<JobTreeNode>? JobNodes { get; }

		Job CurrentJob { get; set; }
		ObjectId CurrentColorBandSetId { get; }
		JobTreeNode? SelectedViewItem { get; set; }

		bool OnFile { get; }
		bool IsDirty { get; }
		bool IsCurrentJobIdChanged { get; }
		int GetNumberOfDirtyJobs();

		//DateTime DateCreatedUtc { get; init; }
		//DateTime LastSavedUtc { get; }
		//DateTime LastAccessedUtc { get; init; }
		//DateTime LastUpdatedUtc { get; }

		IEnumerable<Job> GetJobs();
		List<ColorBandSet> GetColorBandSets();

		JobPathType? GetCurrentPath();

		JobPathType? GetPath(ObjectId jobId);
		Job? GetJob(ObjectId jobId);
		Job? GetParent(Job job);
		//List<Job>? GetJobAndDescendants(ObjectId jobId);

		bool MarkBranchAsPreferred(ObjectId jobId);

		IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType);

		void MarkAsSaved();
	}
}