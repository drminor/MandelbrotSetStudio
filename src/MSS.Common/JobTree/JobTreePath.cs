using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
	using JobBranchType = ITreeBranch<JobTreeNode, Job>;
	using JobPathType = ITreePath<JobTreeNode, Job>;
	using JobNodeType = JobTreeNode;

	public class JobTreePath : TreePath<JobTreeNode, Job> // ITreePath<JobTreeNode, Job>
	{
		#region Constructors

		// Used to create a JobTreeBranch
		protected JobTreePath() : base(new JobTreeNode())
		{
			//_rootItem = new JobTreeNode();
			//Terms = new List<JobNodeType>();
		}

		// Used to create a JobTreeBranch
		protected JobTreePath(JobNodeType rootItem) : base(rootItem)
		{
			//_rootItem = rootItem;
			//Terms = new List<JobNodeType>();
		}

		public JobTreePath(JobNodeType rootItem, JobNodeType term) : this(rootItem, new[] { term })
		{ }

		public JobTreePath(JobNodeType rootItem, IEnumerable<JobNodeType> terms) : base(rootItem, terms)
		{
			//if (!terms.Any())
			//{
			//	throw new ArgumentException("The list of terms cannot be empty when constructing a JobTreePath.", nameof(terms));
			//}

			//_rootItem = rootItem;
			//Terms = terms.ToList();
		}

		#endregion

		#region Overrides, Conversion Operators and ICloneable Support

		//public static implicit operator List<JobTreeItem>?(JobTreePath? jobTreePath) => jobTreePath == null ? null : jobTreePath.Terms;

		//public static explicit operator JobTreePath(List<JobTreeItem> terms) => new JobTreePath(terms);

		public override string ToString()
		{
			return string.Join('\\', Terms.Select(x => x.Item.ToString()));
		}

		//object ICloneable.Clone()
		//{
		//	var result = Clone();
		//	return result;
		//}

		public override JobTreePath Clone()
		{
			return new JobTreePath(_rootItem.Clone(), new List<JobNodeType>(Terms)/*, new ObservableCollection<JobNodeType>(Children)*/);
		}

		#endregion

	}
}