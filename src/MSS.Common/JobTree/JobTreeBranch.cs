using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public class JobTreeBranch : JobTreePath, ITreeBranch<JobTreeNode, Job>, ICloneable 
	{
		#region Constructor

		public JobTreeBranch() : base()
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
	}
}