using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public class TreePath<U, V> : ITreePath<U,V> where U : class, ITreeNode<U,V> where V : class, IEquatable<V>, IComparable<V>
	{
		protected U _rootItem;

		#region Constructor

		// Used to create a TreeBranch
		protected TreePath(U rootItem)
		{
			_rootItem = rootItem;
			Terms = new List<U>();
		}

		public TreePath(U rootItem, U term) : this(rootItem, new[] { term })
		{ }

		public TreePath(U rootItem, IEnumerable<U> terms)
		{
			if (!terms.Any())
			{
				throw new ArgumentException("The list of terms cannot be empty when constructing a JobTreePath.", nameof(terms));
			}

			_rootItem = rootItem;
			Terms = terms.ToList();
		}

		// Used by the Clone Method
		private TreePath(U rootItem, List<U> terms, ObservableCollection<U> children)
		{
			_rootItem = rootItem;
			Terms = terms;
		}

		#endregion

		#region Public Properties

		//virtual public ObservableCollection<U> Children { get; private set; }
		virtual public ObservableCollection<U> Children => new(Node.Children.Select(x => x.Node));
		public List<U> Terms { get; init; }
		virtual public U Node => IsEmpty ? _rootItem : Terms[^1];
		public U NodeSafe => Terms[^1];

		public int Count => Terms.Count;
		public bool IsEmpty => !Terms.Any();
		public bool IsRoot => Node.IsRoot;
		public bool IsHome => Node.IsHome;

		public U? LastTerm => Terms.Count > 0 ? Terms[^1] : null;
		public U? ParentTerm => Terms.Count > 1 ? Terms[^2] : null;
		public U? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		public V? Item => LastTerm?.Item;
		public V ItemSafe => NodeSafe.Item;

		#endregion

		#region Public Methods

		public ITreeBranch<U, V> GetRoot()
		{
			return new TreeBranch<U, V>(_rootItem);
		}

		public ITreePath<U, V>? GetCurrentPath()
		{
			//var result = IsEmpty ? null : new TreePath<U,V>(_rootItem, Terms);
			//var result = IsEmpty ? null : Clone();
			var result = IsEmpty ? null : this;
			return result;
		}

		public ITreeBranch<U,V> GetParentBranch()
		{
			var result = Count > 1
				? new TreeBranch<U,V>(_rootItem, Terms.SkipLast(1))
				: new TreeBranch<U,V>(_rootItem);

			return result;
		}

		public ITreePath<U,V>? GetParentPath()
		{
			var result = Terms.Count > 1 ? new TreePath<U,V>(_rootItem, Terms.SkipLast(1)) : null;
			return result;
		}

		public bool TryGetParentPath([MaybeNullWhen(false)] out ITreePath<U,V> parentPath)
		{
			parentPath = Terms.Count > 1 ? new TreePath<U,V>(_rootItem, Terms.SkipLast(1)) : null;
			return parentPath != null;
		}

		public U? GetParentNode()
		{
			return GetParentPath()?.Node;
		}

		public bool TryGetParentNode([MaybeNullWhen(false)] out U parentNode)
		{
			parentNode = GetParentPath()?.Node;
			return parentNode != null;
		}

		public bool TryGetGrandparentPath([MaybeNullWhen(false)] out ITreePath<U,V> grandparentPath)
		{
			grandparentPath = Terms.Count > 2 ? new TreePath<U,V>(_rootItem, Terms.SkipLast(2)) : null;
			return grandparentPath != null;
		}

		// TOOD: Use this new version
		public U GetNodeOrRootNew()
		{
			var result = Node;
			return result;
		}

		public U GetNodeOrRoot()
		{
			var result = GetCurrentPath()?.Node ?? _rootItem;
			return result;
		}

		public U GetParentNodeOrRoot()
		{
			var result = GetParentPath()?.Node ?? _rootItem;
			return result;
		}

		public ITreePath<U,V> Combine(ITreePath<U,V> jobTreePath)
		{
			return Combine(jobTreePath.Terms);
		}

		public ITreePath<U,V> Combine(U jobTreeItem)
		{
			return Combine(new[] { jobTreeItem });
		}

		public ITreePath<U,V> Combine(IEnumerable<U> jobTreeItems)
		{
			if (IsEmpty)
			{
				var result = new TreePath<U,V>(_rootItem, jobTreeItems);
				return result;
			}
			else
			{
				var newTerms = new List<U>(Terms);
				newTerms.AddRange(jobTreeItems);
				var result = new TreePath<U,V>(_rootItem, newTerms);
				return result;
			}
		}

		public ITreePath<U, V> CreateSiblingPath(U child)
		{
			var parentPath = GetParentPath();

			var result = parentPath == null
				? new TreePath<U, V>(_rootItem, child)
				: parentPath.Combine(child);

			return result;
		}

		#endregion

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

		virtual public TreePath<U,V> Clone()
		{
			return new TreePath<U,V>((U)_rootItem.Clone(), new List<U>(Terms), new ObservableCollection<U>(Children));
		}

		#endregion

	}
}