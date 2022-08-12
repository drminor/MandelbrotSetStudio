using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
	public class JobTreePath : IJobTreeBranch
	{
		private readonly JobTreeItem _rootItem;

		#region Constructor

		public JobTreePath(JobTreeItem rootItem) : this (rootItem, rootItem.Children.Take(1))
		{ }

		public JobTreePath(JobTreeItem rootItem, JobTreeItem term) : this(rootItem, new[] { term })
		{ }

		public JobTreePath(JobTreeItem rootItem, IEnumerable<JobTreeItem> terms)
		{
			_rootItem = rootItem;
			Terms = terms.Any() ? new List<JobTreeItem>(terms) : throw new ArgumentException("The list of terms cannot be empty when constructing a JobTreePath.", nameof(terms));
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobTreeItem> Children => _rootItem.Children;

		public List<JobTreeItem> Terms { get; init; }

		public int Count => Terms.Count;

		public bool IsEmpty => false;

		public JobTreeItem Item => Terms[^1];

		public JobTreeItem? LastTerm => Item;
		public JobTreeItem? ParentTerm => Terms.Count > 1 ?  Terms[^2] : null;
		public JobTreeItem? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		public Job Job => Terms[^1].Job;
		public TransformType TransformType => Job.TransformType;

		public bool IsRoot => Terms[^1].IsRoot;
		public bool IsHome => Terms[^1].IsHome;

		public bool IsActiveAlternate => Terms[^1].IsActiveAlternate;
		public bool IsParkedAlternate => Terms[^1].IsParkedAlternate;

		#endregion

		#region Public Methods

		public JobTreeBranch GetRoot()
		{
			return new JobTreeBranch(_rootItem, new List<JobTreeItem>());
		}

		JobTreePath? IJobTreeBranch.GetCurrentPath()
		{
			return Clone();
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

		public JobTreePath GetParentPathUnsafe()
		{
			var result = new JobTreePath(_rootItem, Terms.SkipLast(1));
			return result;
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

		JobTreeItem IJobTreeBranch.GetItemOrRoot()
		{
			return Item;
		}

		public JobTreeItem GetParentItemOrRoot()
		{
			var result = GetParentPath()?.LastTerm ?? _rootItem;
			return result;
		}

		public JobTreePath Combine(JobTreePath jobTreePath)
		{
			return Combine(jobTreePath.Terms);
		}

		public JobTreePath Combine(JobTreeItem jobTreeItem)
		{
			return Combine( new[] { jobTreeItem });
		}

		public JobTreePath Combine(IEnumerable<JobTreeItem> jobTreeItems)
		{
			var result = Clone();
			result.Terms.AddRange(jobTreeItems);
			return result;
		}

		public JobTreePath CreateSiblingPath(JobTreeItem child)
		{
			var parentPath = GetParentPath();
			var result = parentPath == null ? new JobTreePath(_rootItem, child) : parentPath.Combine(child);
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
			return new JobTreePath(_rootItem, Terms);
		}

		#endregion

	}
}