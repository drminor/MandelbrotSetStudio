using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
	using JobBranchType = ITreeBranch<JobTreeItem, Job>;
	using JobPathType = ITreePath<JobTreeItem, Job>;
	using JobNodeType = JobTreeItem;

	public class JobTreePath : JobPathType
	{
		protected JobNodeType _rootItem;

		#region Constructor

		// Used to create a JobTreeBranch
		protected JobTreePath(JobNodeType rootItem)
		{
			_rootItem = rootItem;
			Terms = new List<JobNodeType>();

			Children = new ObservableCollection<JobNodeType>();
			foreach (var c in rootItem.Children)
			{
				Children.Add(c.Node);
			}
		}

		public JobTreePath(JobNodeType rootItem, JobNodeType term) : this(rootItem, new[] { term })
		{ }

		public JobTreePath(JobNodeType rootItem, IEnumerable<JobNodeType> terms)
		{
			if (!terms.Any())
			{
				throw new ArgumentException("The list of terms cannot be empty when constructing a JobTreePath.", nameof(terms));
			}

			_rootItem = rootItem;

			Terms = terms.ToList();

			var lastTerm = Terms[^1];

			Children = new ObservableCollection<JobNodeType>();
			foreach (var c in lastTerm.Children)
			{
				Children.Add(c.Node);
			}
		}

		// Used by the Clone Method
		private JobTreePath(JobNodeType rootItem, List<JobNodeType> terms, ObservableCollection<JobNodeType> children)
		{
			_rootItem = rootItem;
			Terms = terms;
			Children = children;
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobNodeType> Children { get; private set; }
		//public ObservableCollection<JobNodeType> Children =>
		//	(ObservableCollection<JobNodeType>)
		//		(
		//			IsEmpty
		//			? _rootItem.Children
		//			: Terms[^1].Children
		//		)
		//	.Cast<JobNodeType>();

		public List<JobNodeType> Terms { get; init; }

		public int Count => Terms.Count;

		public bool IsEmpty => !Terms.Any();

		public virtual JobNodeType Node => IsEmpty ? _rootItem : Terms[^1];

		public bool IsRoot => Node.IsRoot;
		public bool IsHome => Node.IsHome;

		public JobNodeType? LastTerm => Terms.Count > 0 ? Terms[^1] : null;
		public JobNodeType? ParentTerm => Terms.Count > 1 ? Terms[^2] : null;
		public JobNodeType? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		public Job? Item => LastTerm?.Item;

		#endregion

		#region Public Methods

		public JobBranchType GetRoot()
		{
			return new JobTreeBranch(_rootItem);
		}

		public JobPathType? GetCurrentPath()
		{
			//var result = IsEmpty ? null : new JobTreePath(_rootItem, Terms);
			//var result = IsEmpty ? null : Clone();
			var result = IsEmpty ? null : this;
			return result;
		}

		public JobBranchType GetParentBranch()
		{
			var result = Count > 1
				? new JobTreeBranch(_rootItem, Terms.SkipLast(1))
				: new JobTreeBranch(_rootItem);

			return result;
		}

		public JobPathType? GetParentPath()
		{
			var result = Terms.Count > 1 ? new JobTreePath(_rootItem, Terms.SkipLast(1)) : null;
			return result;
		}

		public bool TryGetParentPath([MaybeNullWhen(false)] out JobPathType parentPath)
		{
			parentPath = Terms.Count > 1 ? new JobTreePath(_rootItem, Terms.SkipLast(1)) : null;
			return parentPath != null;
		}

		public JobNodeType? GetParentNode()
		{
			return GetParentPath()?.Node;
		}

		public bool TryGetParentNode([MaybeNullWhen(false)] out JobNodeType parentNode)
		{
			parentNode = GetParentPath()?.Node;
			return parentNode != null;
		}

		public bool TryGetGrandparentPath([MaybeNullWhen(false)] out JobPathType grandparentPath)
		{
			grandparentPath = Terms.Count > 2 ? new JobTreePath(_rootItem, Terms.SkipLast(2)) : null;
			return grandparentPath != null;
		}

		// TOOD: Use this new version
		public JobNodeType GetNodeOrRootNew()
		{
			var result = Node;
			return result;
		}

		public JobNodeType GetNodeOrRoot()
		{
			var result = GetCurrentPath()?.Node ?? _rootItem;
			return result;
		}

		public JobNodeType GetParentNodeOrRoot()
		{
			var result = GetParentPath()?.Node ?? _rootItem;
			return result;
		}

		public JobPathType Combine(JobPathType jobTreePath)
		{
			return Combine(jobTreePath.Terms);
		}

		public JobPathType Combine(JobNodeType jobTreeItem)
		{
			return Combine(new[] { jobTreeItem });
		}

		public JobPathType Combine(IEnumerable<JobNodeType> jobTreeItems)
		{
			if (IsEmpty)
			{
				var result = new JobTreePath(_rootItem, jobTreeItems);
				return result;
			}
			else
			{
				var newTerms = new List<JobNodeType>(Terms);
				newTerms.AddRange(jobTreeItems);
				var result = new JobTreePath(_rootItem, newTerms);
				return result;
			}
		}

		#endregion

		public JobPathType CreateSiblingPath(JobNodeType child)
		{
			var parentPath = GetParentPath();

			var result = parentPath == null
				? new JobTreePath(_rootItem, child)
				: parentPath.Combine(child);

			return result;
		}


		#region Overrides, Conversion Operators and ICloneable Support

		//public static implicit operator List<JobTreeItem>?(JobTreePath? jobTreePath) => jobTreePath == null ? null : jobTreePath.Terms;

		//public static explicit operator JobTreePath(List<JobTreeItem> terms) => new JobTreePath(terms);

		public override string ToString()
		{
			return string.Join('\\', Terms.Select(x => x.Item.ToString()));
		}

		object ICloneable.Clone()
		{
			var result = Clone();
			return result;
		}

		public JobTreePath Clone()
		{
			return new JobTreePath(_rootItem.Clone(), new List<JobNodeType>(Terms), new ObservableCollection<JobNodeType>(Children));
		}

		#endregion

	}
}