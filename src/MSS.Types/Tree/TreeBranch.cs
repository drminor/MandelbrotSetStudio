using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public class TreeBranch<U,V> : ITreeBranch<U, V> where U : class, ITreeNode<U,V> where V : class, IEquatable<V>, IComparable<V>
	{
		protected U RootItem { get; }

		#region Constructor

		public TreeBranch(U rootItem)
		{
			RootItem = rootItem;
			Terms = new List<U>();
		}

		public TreeBranch(U rootItem, U term) : this(rootItem, new[] { term })
		{ }

		public TreeBranch(U rootItem, IEnumerable<U> terms)
		{
			RootItem = rootItem;
			Terms = terms.ToList();
		}

		#endregion

		#region Public Properties

		public virtual ObservableCollection<U> Children => Node.Children;
		//public virtual ObservableCollection<U> Children => new(Node.Children);

		public IList<U> Terms { get; init; }
		private U Node => IsEmpty ? RootItem : Terms[^1];

		public int Count => Terms.Count;
		public bool IsEmpty => !Terms.Any();
		public bool IsRoot => Node.IsRoot;
		public bool IsHome => Node.IsHome;

		public U? LastTerm => Terms.Count > 0 ? Terms[^1] : null;
		public U? ParentTerm => Terms.Count > 1 ? Terms[^2] : null;
		public U? GrandparentTerm => Terms.Count > 2 ? Terms[^3] : null;

		#endregion

		#region Public Methods

		public bool ContainsItem(Func<U, bool> predicate, [MaybeNullWhen(false)] out ITreePath<U, V> path)
		{
			var foundNode = GetNodeOrRoot().Children.FirstOrDefault(predicate);
			path = foundNode == null ? null : Combine(foundNode);
			return path != null;
		}

		public ITreePath<U, V> CreatePath(U term)
		{
			if (term.IsRoot)
			{
				throw new ArgumentException("Cannot create a path using the root node.");
			}

			var terms = new List<U> { term };
			var parentNode = term.ParentNode;

			while (parentNode != null && !parentNode.IsRoot)
			{
				terms.Add(parentNode);
				parentNode = parentNode.ParentNode;
			}

			var result = CreatePath(terms.Reverse<U>());
			return result;
		}

		public ITreePath<U,V> CreatePath(IEnumerable<U> terms)
		{
			return new TreePath<U,V>(RootItem, terms);
		}

		public ITreeBranch<U, V> GetRoot()
		{
			return new TreeBranch<U, V>(RootItem);
		}

		public virtual ITreePath<U, V>? GetCurrentPath()
		{
			ITreePath<U, V>? result = IsEmpty ? null : new TreePath<U,V> (RootItem, Terms);
			return result;
		}

		public ITreeBranch<U, V> GetParentBranch()
		{
			var result = Count > 1
				? new TreeBranch<U, V>(RootItem, Terms.SkipLast(1))
				: new TreeBranch<U, V>(RootItem);

			return result;
		}

		public ITreePath<U, V>? GetParentPath()
		{
			var result = Terms.Count > 1 ? new TreePath<U, V>(RootItem, Terms.SkipLast(1)) : null;
			return result;
		}

		public bool TryGetParentPath([MaybeNullWhen(false)] out ITreePath<U, V> parentPath)
		{
			parentPath = Terms.Count > 1 ? new TreePath<U, V>(RootItem, Terms.SkipLast(1)) : null;
			return parentPath != null;
		}

		public U? GetParentNode()
		{
			return GetParentPath()?.Node;
		}

		public bool TryGetParentNode([MaybeNullWhen(false)] out U parentItem)
		{
			parentItem = GetParentPath()?.Node;
			return parentItem != null;
		}

		public bool TryGetGrandparentPath([MaybeNullWhen(false)] out ITreePath<U, V> grandparentPath)
		{
			grandparentPath = Terms.Count > 2 ? new TreePath<U, V>(RootItem, Terms.SkipLast(2)) : null;
			return grandparentPath != null;
		}

		public U GetNodeOrRoot()
		{
			var result = GetCurrentPath()?.Node ?? RootItem;
			return result;
		}

		public U GetParentNodeOrRoot()
		{
			var result = GetParentPath()?.Node ?? RootItem;
			return result;
		}

		public ITreePath<U, V> Combine(ITreePath<U, V> term)
		{
			return Combine(term.Terms);
		}

		public ITreePath<U, V> Combine(U term)
		{
			return Combine(new[] { term });
		}

		public ITreePath<U, V> Combine(IEnumerable<U> terms)
		{
			if (IsEmpty)
			{
				Debug.WriteLine($"Combine on Empty Branch. Terms: {terms}");
				var result = new TreePath<U, V>(RootItem, terms);
				return result;
			}
			else
			{
				var newTerms = new List<U>(Terms);
				newTerms.AddRange(terms);
				var result = new TreePath<U, V>(RootItem, newTerms);
				return result;
			}
		}

		#endregion

		#region Overrides, Conversion Operators and ICloneable Support

		//public static implicit operator JobTreeBranch(JobTreePath path)
		//{
		//	return new JobTreeBranch(path);
		//}

		public override string ToString()
		{
			return string.Join("; ", Terms.Select(x => $"{x.Id}, {x.ParentId ?? ObjectId.Empty}"));
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		virtual public ITreeBranch<U,V> Clone()
		{
			return new TreeBranch<U, V>((U)RootItem.Clone(), new List<U>(Terms));
		}

		#endregion
	}
}