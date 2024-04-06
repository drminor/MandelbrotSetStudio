using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace MSS.Common.MSet
{
	using JobPathType = ITreePath<JobTreeNode, Job>;

	public interface IJobOwner
	{
		ColorBandSetStore ColorBandSetStore { get; }
		ObjectId Id { get; init; }
		OwnerType OwnerType { get; }	
		string Name { get; set; }
		string? Description { get; set; }

		ObservableCollection<JobTreeNode>? JobNodes { get; }

		Job CurrentJob { get; set; }
		ObjectId CurrentColorBandSetId { get; }
		JobTreeNode? SelectedViewItem { get; set; }

		bool OnFile { get; }
		bool IsDirty { get; }
		bool IsCurrentJobIdChanged { get; }

		DateTime LastAccessedUtc { get; init; }

		ColorBandSetResolutionStrategy ColorBandSetResolutionStrategy { get; set; }
		List<TargetIterationColorMapRecord> GetTargetIterationColorMapRecords();

		int GetNumberOfDirtyJobs();

		IEnumerable<Job> GetJobs();
		List<ColorBandSet> GetColorBandSets();

		ColorBandSet? GetColorBandSet(ObjectId id);

		ColorBandSet? GetColorBandSet(string name, int targetIterations, int? version);

		void Add(ColorBandSet colorBandSet, bool makeDefault);
		//bool RemoveColorBandSet(ColorBandSet colorBandSet/*, ObjectId newId*/);

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