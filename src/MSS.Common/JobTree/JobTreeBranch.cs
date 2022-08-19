using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public class JobTreeBranch : JobTreePath, ITreeBranch<JobTreeItem, Job>, ICloneable 
	{
		#region Constructor

		// Creates a Branch with a null path
		public JobTreeBranch(JobTreeItem rootItem) : base(rootItem)
		{ }

		// Creates a Branch with a path consisting of the single term.
		public JobTreeBranch(JobTreeItem rootItem, JobTreeItem term) : this(rootItem, new[] { term })
		{ }

		// Creates a Branch with a path composed of the terms.
		public JobTreeBranch(JobTreeItem rootItem, IEnumerable<JobTreeItem> terms) : base(rootItem, terms)
		{ }

		#endregion
	}
}