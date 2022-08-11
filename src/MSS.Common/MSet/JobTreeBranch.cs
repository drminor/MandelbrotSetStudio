using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
	public class JobTreeBranch : IJobTreeBranch
	{
		#region Constructor

		public JobTreeBranch(JobTreeItem root) : this(root, root.Children.Take(1))
		{ }

		public JobTreeBranch(JobTreeItem root, JobTreeItem term) : this(root, new[] { term })
		{ }

		public JobTreeBranch(JobTreeItem root, IEnumerable<JobTreeItem> terms)
		{
			Tree = root;
			Terms = new List<JobTreeItem>(terms);
		}

		#endregion

		#region Public Properties

		public JobTreeItem Tree { get; }

		public List<JobTreeItem> Terms { get; init; }

		public int Count => Terms.Count;

		public bool IsEmpty => !Terms.Any();

		public JobTreeItem? LastTerm => Terms.Count > 0 ? Terms[^1] : null;
		public JobTreeItem? ParentTerm => Terms.Count > 1 ? Terms[^2] : null;
		public JobTreeItem? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		#endregion

		#region Public Methods

		public JobTreeBranch GetRootBranch()
		{
			return new JobTreeBranch(Tree, new List<JobTreeItem>());
		}

		public JobTreePath? GetCurrentPath()
		{
			var result = IsEmpty ? null : new JobTreePath(this);
			return result;
		}

		public JobTreePath? GetParentPath()
		{
			var result = Terms.Count > 1 ? new JobTreePath(Tree, Terms.SkipLast(1)) : null;
			return result;
		}

		public bool TryGetParentPath([MaybeNullWhen(false)] out JobTreePath parentPath)
		{
			parentPath = Terms.Count > 1 ? new JobTreePath(Tree, Terms.SkipLast(1)) : null;
			return parentPath != null;
		}

		JobTreeItem? GetParentItem()
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
			grandparentPath = Terms.Count > 2 ? new JobTreePath(Tree, Terms.SkipLast(2)) : null;
			return grandparentPath != null;
		}

		public JobTreeItem GetItemOrRoot()
		{
			var result = GetCurrentPath()?.Item ?? Tree;
			return result;
		}

		public JobTreeItem GetParentItemOrRoot()
		{
			var result = GetParentPath()?.LastTerm ?? Tree;
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
			var result = Clone();
			result.Terms.AddRange(jobTreeItems);
			return result;
		}

		#endregion

		#region Overrides, Conversion Operators and ICloneable Support

		//public static implicit operator List<JobTreeItem>?(JobTreePath? jobTreePath) => jobTreePath == null ? null : jobTreePath.Terms;

		//public static explicit operator JobTreePath(List<JobTreeItem> terms) => new JobTreePath(terms);

		public override string ToString() => string.Join('\\', Terms);

		object ICloneable.Clone()
		{
			return Clone();
		}

		public JobTreePath Clone()
		{
			return new JobTreePath(Tree, Terms);
		}

		#endregion

	}
}