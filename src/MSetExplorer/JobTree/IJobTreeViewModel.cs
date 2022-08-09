using MongoDB.Bson;
using MSS.Common;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		IJobOwner? CurrentProject { get; set; }
		Job? CurrentJob { get; set; }

		JobTreeItem? SelectedViewItem { get; set; }

		bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job);
		public JobTreePath? GetCurrentPath();
		public JobTreePath? GetPath(ObjectId jobId);

		ObservableCollection<JobTreeItem>? JobItems { get; }

		bool RestoreBranch(ObjectId jobId);
		long DeleteBranch(ObjectId jobId, out long numberOfMapSectionsDeleted);
		string GetDetails(ObjectId jobId);
	}
}