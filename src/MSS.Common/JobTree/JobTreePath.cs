using System.Collections.Generic;
using System.Linq;

namespace MSS.Common
{
	public class JobTreePath : /*TreePath<JobTreeNode, Job>*/ JobTreeBranch
	{
		#region Constructors

		// Used to create a JobTreeBranch
		protected JobTreePath() : base(new JobTreeNode())
		{
			//_rootItem = new JobTreeNode();
			//Terms = new List<JobNodeType>();
		}

		// Used to create a JobTreeBranch
		protected JobTreePath(JobTreeNode rootItem) : base(rootItem)
		{
			//_rootItem = rootItem;
			//Terms = new List<JobNodeType>();
		}

		public JobTreePath(JobTreeNode rootItem, JobTreeNode term) : this(rootItem, new[] { term })
		{ }

		public JobTreePath(JobTreeNode rootItem, IEnumerable<JobTreeNode> terms) : base(rootItem, terms)
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
			return new JobTreePath(RootItem.Clone(), new List<JobTreeNode>(Terms)/*, new ObservableCollection<JobNodeType>(Children)*/);
		}

		#endregion

	}
}