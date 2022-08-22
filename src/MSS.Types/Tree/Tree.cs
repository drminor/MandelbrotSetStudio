using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSS.Types
{
	public abstract class Tree<U,V> : ITree<U,V> where U: class, ITreeNode<U,V> where V: class, IEquatable<V>, IComparable<V>
	{
		protected ReaderWriterLockSlim TreeLock { get; }
		protected virtual ITreeBranch<U, V> Root { get; set; }
		protected ITreePath<U, V>? CurrentPath { get; set; }

		#region Constructor

		public Tree(TreeBranch<U,V> root)
		{
			TreeLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

			Root = root;
			CurrentPath = Root.GetCurrentPath();

			Debug.Assert(!IsDirty, "IsDirty should be false as the constructor is exited.");
		}

		#endregion

		#region Public Properties

		public ObservableCollection<U> Nodes => Root.Children;

		public virtual ITreeNode<U, V>? SelectedNode { get; set; }

		public V CurrentItem
		{
			get => DoWithReadLock(() =>
			{
				var currentItem = CurrentPath?.Item;
				if (currentItem == null)
				{
					Debug.WriteLine("WARNING: In CurrentItem:Getter, the CurrentPath is null. Returning the Home Item.");
					currentItem = Nodes[0].Item;
				}
				return currentItem;
			});

			set => DoWithWriteLock(() =>
			{
				if (value != CurrentPath?.Item)
				{
					if (value != null)
					{
						if (MoveCurrentTo(value, Root, out var path))
						{
							CurrentPath = path;
						}
						else
						{
							Debug.WriteLine($"WARNING: Could not MoveCurrent to item: {value}.");
						}
					}
				}
			});
		}

		public bool CanGoBack
		{
			get
			{
				TreeLock.EnterReadLock();

				try
				{
					return CanMoveBack(CurrentPath);
				}
				finally
				{
					TreeLock.ExitReadLock();
				}
			}
		}

		public bool CanGoForward
		{
			get
			{
				TreeLock.EnterReadLock();

				try
				{
					return CanMoveForward(CurrentPath);
				}
				finally
				{
					TreeLock.ExitReadLock();
				}
			}
		}

		public bool IsDirty { get; set; }

		public bool AnyItemIsDirty
		{
			get
			{
				TreeLock.EnterReadLock();

				try
				{
					return GetNodes(Root).Any(x => x.IsDirty);
				}
				finally
				{
					TreeLock.ExitReadLock();
				}

			}
		}

		#endregion

		#region Public Methods

		public ITreePath<U, V> Add(V item, bool selectTheAddedItem)
		{
			ITreePath<U, V> newPath;
			TreeLock.EnterWriteLock();

			try
			{
				newPath = AddInternal(item, currentBranch: Root);
				IsDirty = true;
			}
			finally
			{
				TreeLock.ExitWriteLock();
			}

			if (selectTheAddedItem)
			{
				ExpandAndSetCurrent(newPath);
				CurrentPath = newPath;
			}

			return newPath;
		}

		public bool RemoveBranch(ObjectId itemId)
		{
			if (!TryFindPathById(itemId, Root, out var path))
			{
				return false;
			}

			var result = RemoveBranch(path);
			return result;
		}

		public virtual bool RemoveBranch(ITreePath<U, V> path)
		{
			var node = path.Node;
			var parentNode = path.GetParentNodeOrRoot();
			var idx = parentNode.Children.IndexOf(node);

			if (idx == 0)
			{
				if (parentNode.IsHome)
				{
					throw new InvalidOperationException("Removing the Home node is not yet supported.");
				}

				CurrentPath = path.GetParentPath();
			}
			else
			{
				CurrentPath = path.CreateSiblingPath(parentNode.Children[idx - 1].Node);
			}

			var result = parentNode.Children.Remove(node);
			ExpandAndSetCurrent(CurrentPath);

			return result;
		}

		public virtual bool TryGetPreviousItem([MaybeNullWhen(false)] out V item, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			if (CurrentPath == null)
			{
				item = null;
				return false;
			}

			var backPath = GetPreviousItemPath(CurrentPath, predicate);
			item = backPath?.Item;

			return item != null;
		}

		public virtual bool MoveBack(Func<ITreeNode<U,V>, bool>? predicate = null)
		{
			if (CurrentPath == null)
			{
				return false;
			}

			var backPath = GetPreviousItemPath(CurrentPath, predicate);

			if (backPath != null)
			{
				CurrentPath = backPath;
				ExpandAndSetCurrent(backPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public virtual bool TryGetNextItem([MaybeNullWhen(false)] out V item, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			if (CurrentPath == null)
			{
				item = null;
				return false;
			}

			var forwardPath = GetNextItemPath(CurrentPath, predicate);
			item = forwardPath?.Item;

			return item != null;
		}

		public virtual bool MoveForward(Func<ITreeNode<U,V>, bool>? predicate = null)
		{
			if (CurrentPath == null)
			{
				return false;
			}

			var forwardPath = GetNextItemPath(CurrentPath, predicate);

			if (forwardPath != null)
			{
				CurrentPath = forwardPath;
				ExpandAndSetCurrent(forwardPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Public Methods - Collection

		public ITreePath<U,V>? GetCurrentPath()
		{
			TreeLock.EnterReadLock();

			try
			{
				return CurrentPath == null ? null : CurrentPath;
			}
			finally
			{
				TreeLock.ExitReadLock();
			}
		}

		public ITreePath<U,V>? GetPath(ObjectId itemId)
		{
			TreeLock.EnterReadLock();

			try
			{
				return GetPathById(itemId, Root);
			}
			finally
			{
				TreeLock.ExitReadLock();
			}
		}

		public IEnumerable<V> GetItems()
		{
			TreeLock.EnterReadLock();

			try
			{
				var result = GetItems(Root);
				return result;
			}
			finally
			{
				TreeLock.ExitReadLock();
			}
		}

		public V? GetItem(ObjectId itemId)
		{
			_ = TryFindItem(itemId, Root, out var result);
			return result;
		}

		public V? GetParentItem(U node)
		{
			if (node.ParentId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindItem(node.ParentId.Value, Root, out var result);
				return result;
			}
		}

		public List<V>? GetItemAndDescendants(ObjectId itemId)
		{
			TreeLock.EnterReadLock();

			try
			{
				List<V>? result;

				if (TryFindPathById(itemId, Root, out var path))
				{
					result = new List<V> { path.Node.Item };
					result.AddRange(GetItems(path));
				}
				else
				{
					result = null;
				}

				return result;
			}
			finally
			{
				TreeLock.ExitReadLock();
			}
		}

		#endregion

		#region Protected Add Methods

		protected abstract ITreePath<U, V> AddInternal(V item, ITreeBranch<U,V> currentBranch);

		protected virtual ITreePath<U, V> AddItem(V item, ITreeBranch<U, V> parentBranch)
		{
			var parentNode = parentBranch.GetNodeOrRoot();
			var newNode = parentNode.AddItem(item);

			var result = parentBranch.Combine(newNode);

			return result;
		}

		#endregion

		#region Protected Export Item Methods

		protected IList<V> GetItems(ITreeBranch<U,V> currentBranch)
		{
			// TODO: Consider implementing an IEnumerator<ITreeItem<U,V> for the Tree class.
			var result = new List<V>();

			foreach (var child in currentBranch.GetNodeOrRoot().Children)
			{
				result.Add(child.Item);

				var nodeList = GetItems(currentBranch.Combine(child.Node));
				result.AddRange(nodeList);
			}

			return result;
		}

		protected IList<ITreeNode<U,V>> GetNodes(ITreeBranch<U,V> currentBranch)
		{
			// TODO: Consider implementing an IEnumerator<ITreeItem<U,V> for the Tree class.
			var result = new List<ITreeNode<U,V>>();

			foreach (var child in currentBranch.GetNodeOrRoot().Children)
			{
				result.Add(child);

				var nodeList = GetNodes(currentBranch.Combine(child.Node));
				result.AddRange(nodeList);
			}

			return result;
		}

		protected List< Tuple<ITreeNode<U, V>, ITreeNode<U, V>? > > GetNodesWithParentage(ITreeBranch<U,V> currentBranch)
		{
			var result = new List<Tuple<ITreeNode<U, V>, ITreeNode<U, V>?>>();

			foreach (var child in currentBranch.GetNodeOrRoot().Children)
			{
				result.Add(new Tuple<ITreeNode<U, V>, ITreeNode<U, V>?>(child, (U?)child.ParentNode));

				var nodeList = GetNodesWithParentage(currentBranch.Combine(child.Node));
				result.AddRange(nodeList);
			}

			return result;
		}

		#endregion

		#region Private Collection Methods, With Support for CanvasSizeUpdates

		protected bool TryFindItem(ObjectId id, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out V item)
		{
			item = GetPathById(id, currentBranch)?.Item;
			return item != null;
		}

		protected ITreePath<U,V>? GetPathById(ObjectId id, ITreeBranch<U,V> currentBranch)
		{
			return TryFindPathById(id, currentBranch, out var path) ? path : null;
		}

		protected ITreePath<U,V> GetPath(V item, ITreeBranch<U,V> currentBranch)
		{
			return TryFindPath(item, currentBranch, out var path)
				? path
				: throw new InvalidOperationException($"Cannot find Item: {item} in the Tree.");
		}

		protected bool TryFindPath(V item, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			path = currentBranch.GetCurrentPath();
			return item.Equals(path?.LastTerm) || TryFindPathInternal(item, currentBranch, out path);
		}

		protected bool TryFindPathById(ObjectId id, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			path = currentBranch.GetCurrentPath();
			return (id == path?.LastTerm?.Id) || TryFindPathByIdInternal(id, currentBranch, out path);
		}

		protected bool TryFindPathByIdInternal(ObjectId id, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			//var tc = currentBranch.Children;
			if (NodeContainsItem(currentBranch, x => x.Id == id, out path))
			{
				return true;
			}

			var node = currentBranch.GetNodeOrRoot();

			foreach (U child in node.Children)
			{
				var cPath = currentBranch.Combine(child);

				//ITreeBranch<U,V> cb = cPath;
				//var testBranchChildren = cb.Children;
				//var testPathChildren = cPath.Children;
				//var testBranchItem = cb.GetItemOrRoot();
				//var testPathItem = cPath.Item;

				if (TryFindPathByIdInternal(id, currentBranch.Combine(child), out var localPath))
				{
					path = currentBranch.Combine(localPath);
					return true;
				}
			}

			path = null;
			return false;
		}

		protected bool TryFindPathInternal(V item, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			//var tc = currentBranch.Children;
			if (NodeContainsItem(currentBranch, x => x.Item.Equals(item), out path))
			{
				return true;
			}

			var node = currentBranch.GetNodeOrRoot();


			foreach (U child in node.Children)
			{
				var cPath = currentBranch.Combine(child);

				//ITreeBranch<U,V> cb = cPath;
				//var testBranchChildren = cb.Children;
				//var testPathChildren = cPath.Children;
				//var testBranchItem = cb.GetItemOrRoot();
				//var testPathItem = cPath.Item;

				if (TryFindPathInternal(item, currentBranch.Combine(child), out var localPath))
				{
					path = currentBranch.Combine(localPath);
					return true;
				}
			}

			path = null;
			return false;
		}

		protected bool NodeContainsItem(ITreeBranch<U,V> branch, Func<ITreeNode<U,V>, bool> predicate, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			var foundNode = branch.GetNodeOrRoot().Children.FirstOrDefault(predicate);
			//var foundNode = branch.Children.FirstOrDefault(predicate);
			path = foundNode == null ? null : branch.Combine(foundNode.Node);
			return path != null;
		}

		#endregion

		#region Protected Navigate Methods

		protected virtual ITreePath<U,V>? GetNextItemPath(ITreePath<U,V> path, Func<ITreeNode<U,V>, bool>? predicate = null)
		{
			var currentItem = path.Node;

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			var nextNode = GetNextNode(siblings, currentPosition, predicate);

			if (nextNode != null)
			{
				//The new item will be a sibling of the current item
				var result = path.Combine(nextNode.Node);
				return result;
			}
			else
			{
				return null;
			}
		}

		protected virtual ITreeNode<U, V>? GetNextNode(IList<ITreeNode<U, V>> nodes, int currentPosition, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			var result = predicate != null
				? nodes.Skip(currentPosition + 1).FirstOrDefault(predicate)
				: nodes.Skip(currentPosition + 1).FirstOrDefault();
			return result;
		}

		protected virtual bool CanMoveForward(ITreePath<U,V>? path)
		{
			var currentItem = path?.LastTerm;

			if (path == null || currentItem == null)
			{
				return false;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			return !(currentPosition == siblings.Count - 1);
		}

		protected virtual ITreePath<U,V>? GetPreviousItemPath(ITreePath<U,V> path, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			var currentItem = path.Node;

			if (currentItem == null)
			{
				return null;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousNode = GetPreviousNode(siblings, currentPosition, predicate);

			// TODO: Make climbing the tree, more elegant
			while (previousNode == null && path.Count > 1)
			{
				path = path.GetParentPath()!;
				currentItem = path.Node;

				var grandparentNode = path.GetParentNodeOrRoot();
				var ancestors = grandparentNode.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousNode = GetPreviousNode(ancestors, currentPosition + 1, predicate);
			}

			if (previousNode != null)
			{
				var result = path.Combine(previousNode.Node);
				return result;
			}
			else
			{
				return null;
			}
		}

		protected virtual ITreeNode<U, V>? GetPreviousNode(IList<ITreeNode<U, V>> nodes, int currentPosition, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			var result = predicate != null
				? nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault(predicate)
				: nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault();
			return result;
		}

		protected virtual bool CanMoveBack(ITreePath<U,V>? path)
		{
			var currentItem = path?.LastTerm;

			if (path == null || currentItem == null)
			{
				return false;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (currentPosition > 0)
			{
				// We can move to the previous item at the current level.
				return true;
			}
			else
			{
				// If we can go up, return true.
				return path.Count > 1;
			}
		}

		protected virtual bool MoveCurrentTo(V item, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			if (TryFindPath(item, currentBranch, out path))
			{
				ExpandAndSetCurrent(path);
				return true;
			}
			else
			{
				return false;
			}
		}

		protected virtual void ExpandAndSetCurrent(ITreePath<U,V>? path)
		{
			if (path == null || path.IsEmpty)
			{
				return;
			}

			foreach (var p in path.Terms.SkipLast(1))
			{
				p.IsExpanded = true;
			}

			var lastTerm = path .Node;
			lastTerm.IsCurrent = true;
		}

		#endregion

		#region Lock Helpers

		protected V DoWithReadLock(Func<V> function)
		{
			TreeLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				TreeLock.ExitReadLock();
			}
		}

		protected void DoWithWriteLock(Action action)
		{
			TreeLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				TreeLock.ExitWriteLock();
			}
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			((IDisposable)TreeLock).Dispose();
		}

		#endregion

	}
}
