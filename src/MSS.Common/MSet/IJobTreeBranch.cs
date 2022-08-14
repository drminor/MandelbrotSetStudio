using MongoDB.Bson;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	public interface IJobTreeBranch : ICloneable
	{
		//JobTreeItem RootItem { get; }

		Job? Job { get; }
		ObservableCollection<JobTreeItem> Children { get; }
		List<JobTreeItem>? AlternateDispSizes { get; }

		int Count { get; }
		bool IsEmpty { get; }

		List<JobTreeItem> Terms { get; init; }

		JobTreeItem? LastTerm { get; }
		JobTreeItem? ParentTerm { get; }
		JobTreeItem? GrandparentTerm { get; }

		JobTreePath? GetCurrentPath();
		JobTreePath? GetParentPath();
		JobTreeBranch GetParentBranch();

		JobTreeBranch GetRoot();
		JobTreeItem GetItemOrRoot();
		JobTreeItem GetParentItemOrRoot();

		bool TryGetParentItem([MaybeNullWhen(false)] out JobTreeItem parentItem);
		bool TryGetParentPath([MaybeNullWhen(false)] out JobTreePath parentPath);
		bool TryGetGrandparentPath([MaybeNullWhen(false)] out JobTreePath grandparentPath);

		JobTreePath Combine(JobTreeItem jobTreeItem);
		JobTreePath Combine(JobTreePath jobTreePath);
		JobTreePath Combine(IEnumerable<JobTreeItem> jobTreeItems);

		//JobTreeItem GetItemOrRoot();
		//bool TryGetParentBranch([MaybeNullWhen(false)] out JobTreeBranch parentPath);

	}
}