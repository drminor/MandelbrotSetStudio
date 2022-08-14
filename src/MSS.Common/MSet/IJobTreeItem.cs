using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSS.Common
{
	public interface IJobTreeItem
	{
		ObservableCollection<JobTreeItem> Children { get; init; }
		bool IsHome { get; }
		bool IsOrphan { get; }
		bool IsRoot { get; init; }
		Job Job { get; init; }
		JobTreeItem? ParentNode { get; }

		JobTreeItem AddJob(Job job);
		List<JobTreeItem> GetAncestors();
		int GetSortPosition(Job job);
		bool Move(JobTreeItem destination);
		bool Remove(JobTreeItem jobTreeItem);
	}
}