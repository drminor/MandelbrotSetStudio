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

		Project? CurrentProject { get; set; }
		Job? CurrentJob { get; set; }

		bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job);
		public IReadOnlyCollection<JobTreeItem>? GetCurrentPath();
		public IReadOnlyCollection<JobTreeItem>? GetPath(ObjectId jobId);

		ObservableCollection<JobTreeItem>? JobItems { get; }

		bool RestoreBranch(ObjectId jobId);
		long DeleteBranch(ObjectId jobId, out long numberOfMapSectionsDeleted);
		string GetDetails(ObjectId jobId);
	}
}