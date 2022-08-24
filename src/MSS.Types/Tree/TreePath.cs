using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MSS.Types
{
	public class TreePath<U, V> : TreeBranch<U,V>, ITreePath<U,V>, ICloneable  where U : class, ITreeNode<U,V> where V : class, IEquatable<V>, IComparable<V>
	{
		#region Constructor

		// Creates a Branch with a null path
		public TreePath(U rootItem) : base(rootItem)
		{ }

		public TreePath(U rootItem, U term) : base(rootItem, new[] { term })
		{ }

		public TreePath(U rootItem, IEnumerable<U> terms) : base(rootItem, terms)
		{
			if (!terms.Any())
			{
				throw new ArgumentException("The list of terms cannot be empty when constructing a TreePath.", nameof(terms));
			}
		}

		#endregion

		#region Public Properties

		public U Node => Terms[^1];
		public V Item => Node.Item;

		#endregion

		#region Public Methods

		public override ITreePath<U, V>? GetCurrentPath()
		{
			// TODO: Consider using clone when returing the CurrentPath
			//var result = IsEmpty ? null : Clone();
			var result = IsEmpty ? null : this;
			return result;
		}

		public ITreePath<U, V> CreateSiblingPath(U term)
		{
			var parentPath = GetParentPath();

			var result = parentPath == null
				? new TreePath<U, V>(RootItem, term)
				: parentPath.Combine(term);

			return result;
		}

		#endregion

		#region Overrides, Conversion Operators and ICloneable Support

		//public static implicit operator List<JobTreeItem>?(JobTreePath? jobTreePath) => jobTreePath == null ? null : jobTreePath.Terms;
		//public static explicit operator JobTreePath(List<JobTreeItem> terms) => new JobTreePath(terms);

		object ICloneable.Clone()
		{
			var result = Clone();
			return result;
		}

		public override TreePath<U, V> Clone()
		{
			var result = base.Clone();
			return (TreePath<U, V>)result;
			//return new TreePath<U,V>((U)RootItem.Clone(), new List<U>(Terms));
		}

		#endregion

	}
}