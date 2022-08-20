using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MSS.Types
{
	public class TreeBranch<U,V> : TreePath<U,V>, ITreeBranch<U, V>, ICloneable where U : class, ITreeNode<U,V> where V : class, IEquatable<V>, IComparable<V>
	{
		#region Constructor

		// Creates a Branch with a null path
		public TreeBranch(U rootItem) : base(rootItem)
		{
		}

		// Creates a Branch with a path consisting of the single term.
		public TreeBranch(U rootItem, U term) : base(rootItem, new[] { term })
		{
		}

		// Creates a Branch with a path composed of the terms.
		public TreeBranch(U rootItem, IEnumerable<U> terms) : base(rootItem, terms)
		{
		}

		#endregion

		#region Public Properties

		override public U Node => _rootItem;

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