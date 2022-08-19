using MongoDB.Bson;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	using JobPathType = ITreePath<JobTreeItem, Job>;

	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		IJobOwner? CurrentProject { get; set; }
		Job? CurrentJob { get; set; }
		public JobPathType? GetCurrentPath();

		JobTreeItem? SelectedViewItem { get; set; }

		bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job);
		public JobPathType? GetPath(ObjectId jobId);

		ObservableCollection<JobTreeItem>? JobItems { get; }

		bool RestoreBranch(ObjectId jobId);
		long DeleteBranch(ObjectId jobId, out long numberOfMapSectionsDeleted);
		string GetDetails(ObjectId jobId);
	}
}