using MongoDB.Bson;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		Project? CurrentProject { get; set; }
		Job? CurrentJob { get; }

		public IReadOnlyCollection<JobTreeItem>? GetCurrentPath();
		public IReadOnlyCollection<JobTreeItem>? GetPath(ObjectId jobId);

		ObservableCollection<JobTreeItem>? JobItems { get; }

		bool RaiseNavigateToJobRequested(ObjectId jobId);

		bool RestoreBranch(ObjectId jobId);

		bool DeleteBranch(ObjectId jobId);

		string GetDetails(ObjectId jobId);
	}
}