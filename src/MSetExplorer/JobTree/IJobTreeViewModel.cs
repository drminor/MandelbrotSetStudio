﻿using MongoDB.Bson;
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

		IJobOwner? CurrentProject { get; set; }
		Job? CurrentJob { get; set; }
		public JobPathType? GetCurrentPath();

		JobTreeNode? SelectedViewItem { get; set; }

		bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job);
		public JobPathType? GetPath(ObjectId jobId);

		ObservableCollection<JobTreeNode>? JobItems { get; }

		bool MarkBranchAsPreferred(ObjectId jobId);
		long DeleteBranch(ObjectId jobId, NodeSelectionType deleteType, out long numberOfMapSectionsDeleted);
		string GetDetails(ObjectId jobId);
	}
}