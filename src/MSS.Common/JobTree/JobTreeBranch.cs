using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MSS.Common
{
	using JobNodeType = JobTreeNode;

	public class JobTreeBranch : JobTreePath, ITreeBranch<JobTreeNode, Job>, ICloneable 
	{
		#region Constructor

		// Creates a Branch using a new root
		public JobTreeBranch() : base()
		{ }

		// Creates a Branch with a null path
		public JobTreeBranch(JobNodeType rootItem) : base(rootItem)
		{ }

		// Creates a Branch with a path consisting of the single term.
		public JobTreeBranch(JobNodeType rootItem, JobNodeType term) : this(rootItem, new[] { term })
		{ }

		// Creates a Branch with a path composed of the terms.
		public JobTreeBranch(JobNodeType rootItem, IEnumerable<JobNodeType> terms) : base(rootItem, terms)
		{ }

		#endregion

		#region Public Properties

		//override public ObservableCollection<JobNodeType> Children => new(Node.Children.Select(x => x.Node));

		override public JobNodeType Node => _rootItem;

		#endregion

		#region Overrides, Conversion Operators and ICloneable Support

		//public static implicit operator JobTreeBranch(JobTreePath path)
		//{
		//	return new JobTreeBranch(path);
		//}

		public override string ToString()
		{
			return string.Join('\\', Terms);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public override JobTreeBranch Clone()
		{
			return new JobTreeBranch(_rootItem, Terms);
		}

		#endregion
	}
}