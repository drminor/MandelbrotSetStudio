using System;
using System.Collections.Generic;

namespace MSS.Types
{
	public class TreeBranch<U,V> : TreePath<U,V>, ITreeBranch<U, V>, ICloneable where U : class, ITreeNode<U,V> where V : class, IEquatable<V>, IComparable<V>
	{
		#region Constructor

		//public JobTreeBranch(ITreePath<V> jobTreePath) : this(jobTreePath.GetRoot()._rootItem, new List<JobTreeItem>(jobTreePath.Terms))
		//{ }

		//public TreeBranch(U rootItem) : this(rootItem, rootItem.Children.Take(1))
		//{ }

		//public TreeBranch(V homeJob)
		//{
		//	//if (homeJob.ParentJobId != null)
		//	//{
		//	//	Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has a non-null ParentJobId. Setting the ParentJobId to null.");
		//	//	homeJob.ParentJobId = null;
		//	//}

		//	//if (homeJob.TransformType != TransformType.Home)
		//	//{
		//	//	Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has TransformType of {homeJob.TransformType}. Expecting the TransformType to be {nameof(TransformType.Home)}.");
		//	//}


		//	_rootItem = (U) (ITreeItem<V>)  new TreeItem<V>(homeJob, null);
		//	Terms = new List<U>();

		//	Terms.Add(_rootItem);
		//	Children = new ObservableCollection<U>(_rootItem.Children.Cast<U>().ToList());
		//}


		public TreeBranch(U rootItem, U term) : base(rootItem, new[] { term })
		{
		}

		public TreeBranch(U rootItem, IEnumerable<U> terms) : base(rootItem, terms)
		{
		}

		public TreeBranch(U rootItem) : base(rootItem)
		{
		}

		#endregion

		#region Public Properties

		//public new ObservableCollection<ITreeItem<V>> Children { get; set; }

		#endregion

		#region Public Methods


		//public ITreePath<U,V> Combine(IEnumerable<U> jobTreeItems)
		//{
		//	if (IsEmpty)
		//	{
		//		var result = new TreePath<U,V>(_rootItem, jobTreeItems);
		//		return (ITreePath<U, V>)result;
		//	}
		//	else
		//	{
		//		var newTerms = new List<U>(Terms);
		//		newTerms.AddRange(jobTreeItems);
		//		var result = new TreePath<U,V>(_rootItem, newTerms);
		//		return (ITreePath<U, V>)result;
		//	}
		//}

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

		public new ITreeBranch<U,V> Clone()
		{
			return new TreeBranch<U, V>(_rootItem, Terms);
		}

		#endregion
	}
}