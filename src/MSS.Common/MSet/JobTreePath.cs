using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public class JobTreePath : ICloneable
	{
		#region Constructor

		public JobTreePath(JobTreeItem root) : this (root, root.Children.Take(1))
		{ }

		public JobTreePath(JobTreeItem root, JobTreeItem term) : this(root, new[] { term })
		{ }

		public JobTreePath(JobTreeItem root, IEnumerable<JobTreeItem> terms)
		{
			Tree = root;
			Terms = new List<JobTreeItem>(terms);
		}

		#endregion

		#region Public Properties

		public JobTreeItem Tree { get; }

		public List<JobTreeItem> Terms { get; init; }

		public int Count => Terms.Count;

		public bool IsEmpty => Terms.Count == 0;

		public JobTreeItem? LastTerm => Terms.Count > 0 ? Terms[^1] : null;

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

		public JobTreePath GetRootPath()
		{
			return new JobTreePath(Tree, new List<JobTreeItem>());
		}

		public JobTreePath? GetParentPath()
		{
			var result = Terms.Count > 0 ? new JobTreePath(Tree, Terms.SkipLast(1)) : null;
			return result;
		}

		public JobTreePath? GetGrandparentPath()
		{
			var result = Terms.Count > 1 ? new JobTreePath(Tree, Terms.SkipLast(2)) : null;
			return result;
		}

		public JobTreePath GetParentPathUnsafe()
		{
			return new JobTreePath(Tree, Terms.SkipLast(1));
		}

		public JobTreePath GetGrandparentPathUnSafe()
		{
			return new JobTreePath(Tree, Terms.SkipLast(2));
		}

		public JobTreeItem GetItemUnsafe()
		{
			return Terms[^1];
		}

		public JobTreeItem GetParentItemUnsafe()
		{
			return Terms[^2];
		}

		public JobTreeItem GetGrandparentComponentUnsafe()
		{
			return Terms[^3];
		}

		public JobTreeItem GetItemOrRoot()
		{
			var result = IsEmpty ? Tree : GetItemUnsafe();
			return result;
		}

		public JobTreeItem GetParentItemOrRoot()
		{
			var result = GetParentPath()?.LastTerm ?? Tree;
			return result;
		}

		public JobTreePath CreatePath(JobTreeItem item)
		{
			var result = new JobTreePath(Tree, item);
			return result;
		}

		public JobTreePath CreateSiblingPath(JobTreePath path, JobTreeItem item)
		{
			var result = path.IsEmpty ? new JobTreePath(Tree, item) : path.GetParentPathUnsafe().Combine(item);
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