using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public class JobTreeBranch : JobTreePath, ITreeBranch<JobTreeNode, Job>, ICloneable 
	{
		#region Constructor

		// Creates a Branch using a new root
		public JobTreeBranch() : base(new JobTreeNode())
		{ }

		// Creates a Branch with a null path
		public JobTreeBranch(JobTreeNode rootItem) : base(rootItem)
		{ }

		// Creates a Branch with a path consisting of the single term.
		public JobTreeBranch(JobTreeNode rootItem, JobTreeNode term) : this(rootItem, new[] { term })
		{ }

		// Creates a Branch with a path composed of the terms.
		public JobTreeBranch(JobTreeNode rootItem, IEnumerable<JobTreeNode> terms) : base(rootItem, terms)
		{ }

		#endregion

		#region Public Properties

		override public JobTreeNode Node => RootItem;

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
			return new JobTreeBranch(RootItem, Terms);
		}

		#endregion
	}
}