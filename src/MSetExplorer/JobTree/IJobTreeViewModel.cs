using MongoDB.Bson;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;

namespace MSetExplorer
{
	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		ObservableCollection<JobTreeNode>? JobNodes { get; }

		IJobOwner? CurrentProject { get; set; }
		Job? CurrentJob { get; set; }
		public JobPathType? GetCurrentPath();

		JobTreeNode? SelectedViewItem { get; set; }

		bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job);
		public JobPathType? GetPath(ObjectId jobId);

		bool MarkBranchAsPreferred(ObjectId jobId);
		long DeleteJobs(JobPathType path, NodeSelectionType selectionType, out long numberOfMapSectionsDeleted);
		string GetDetails(ObjectId jobId);
	}
}