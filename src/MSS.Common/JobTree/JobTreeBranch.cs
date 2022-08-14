using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
	public class JobTreeBranch : IJobTreeBranch
	{
		private readonly JobTreeItem _rootItem;

		#region Constructor

		public JobTreeBranch(JobTreePath jobTreePath) : this(jobTreePath.GetRoot()._rootItem, new List<JobTreeItem>(jobTreePath.Terms))
		{ }

		public JobTreeBranch(JobTreeItem rootItem) : this(rootItem, rootItem.Children.Take(1))
		{ }

		public JobTreeBranch(JobTreeItem rootItem, JobTreeItem term) : this(rootItem, new[] { term })
		{ }

		public JobTreeBranch(JobTreeItem rootItem, IEnumerable<JobTreeItem> terms)
		{
			_rootItem = rootItem;
			Terms = new List<JobTreeItem>(terms);
		}

		#endregion

		#region Public Properties

		public Job? Job => LastTerm?.Job;
		public ObservableCollection<JobTreeItem> Children => GetItemOrRoot().Children;
		public List<JobTreeItem>? AlternateDispSizes => GetItemOrRoot().AlternateDispSizes;

		public List<JobTreeItem> Terms { get; init; }

		public int Count => Terms.Count;

		public bool IsEmpty => !Terms.Any();

		public JobTreeItem? LastTerm => Terms.Count > 0 ? Terms[^1] : null;
		public JobTreeItem? ParentTerm => Terms.Count > 1 ? Terms[^2] : null;
		public JobTreeItem? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		#endregion

		#region Public Methods

		public JobTreeBranch GetRoot()
		{
			return new JobTreeBranch(_rootItem, new List<JobTreeItem>());
		}

		public JobTreePath? GetCurrentPath()
		{
			var result = IsEmpty ? null : new JobTreePath(_rootItem, Terms);
			return result;
		}

		public JobTreeBranch GetParentBranch()
		{
			var result = new JobTreeBranch(_rootItem, Terms.SkipLast(1));
			return result;
		}

		public JobTreePath? GetParentPath()
		{
			var result = Terms.Count > 1 ? new JobTreePath(_rootItem, Terms.SkipLast(1)) : null;
			return result;
		}

		public bool TryGetParentPath([MaybeNullWhen(false)] out JobTreePath parentPath)
		{
			parentPath = Terms.Count > 1 ? new JobTreePath(_rootItem, Terms.SkipLast(1)) : null;
			return parentPath != null;
		}

		public JobTreeItem? GetParentItem()
		{
			return GetParentPath()?.Item;
		}

		public bool TryGetParentItem([MaybeNullWhen(false)] out JobTreeItem parentItem)
		{
			parentItem = GetParentPath()?.Item;
			return parentItem != null;
		}

		public bool TryGetGrandparentPath([MaybeNullWhen(false)] out JobTreePath grandparentPath)
		{
			grandparentPath = Terms.Count > 2 ? new JobTreePath(_rootItem, Terms.SkipLast(2)) : null;
			return grandparentPath != null;
		}

		public JobTreeItem GetItemOrRoot()
		{
			var result = GetCurrentPath()?.Item ?? _rootItem;
			return result;
		}

		public JobTreeItem GetParentItemOrRoot()
		{
			var result = GetParentPath()?.Item ?? _rootItem;
			return result;
		}

		public JobTreePath Combine(JobTreePath jobTreePath)
		{
			return Combine(jobTreePath.Terms);
		}

		public JobTreePath Combine(JobTreeItem jobTreeItem)
		{
			return Combine(new[] { jobTreeItem });
		}

		public JobTreePath Combine(IEnumerable<JobTreeItem> jobTreeItems)
		{
			JobTreePath result;

			if (IsEmpty)
			{
				result = new JobTreePath(_rootItem, jobTreeItems);
			}
			else
			{
				var newTerms = new List<JobTreeItem>(Terms);
				newTerms.AddRange(jobTreeItems);
				result = new JobTreePath(_rootItem, newTerms);
			}

			return result;
		}

		#endregion

		#region Overrides, Conversion Operators and ICloneable Support

		public static implicit operator JobTreeBranch(JobTreePath path)
		{
			return new JobTreeBranch(path);
		}

		public override string ToString()
		{
			return string.Join('\\', Terms);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public JobTreeBranch Clone()
		{
			return new JobTreeBranch(_rootItem, Terms);
		}

		#endregion
	}
}