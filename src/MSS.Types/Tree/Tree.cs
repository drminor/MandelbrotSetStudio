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
	public class Tree<U,V> : ITree<U,V> where U: class, ITreeNode<U,V> where V: class, IEquatable<V>, IComparable<V>
	{
		protected readonly ReaderWriterLockSlim _treeLock;
		protected virtual ITreeBranch<U, V> _root { get; set; }

		protected ITreePath<U,V>? _currentPath;

		#region Constructor

		public Tree(TreeBranch<U,V> root)
		{
			_treeLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

			_root = root;
			_currentPath = _root.GetCurrentPath();

			Debug.Assert(!IsDirty, "IsDirty should be false as the constructor is exited.");
		}

		#endregion

		#region Public Properties

		public virtual ObservableCollection<U> Nodes => _root.Children;

		public V CurrentItem
		{
			get => DoWithReadLock(() =>
			{
				var currentItem = _currentPath?.Item;
				if (currentItem == null)
				{
					Debug.WriteLine("WARNING: In CurrentItem:Getter, the CurrentPath is null. Returning the Home Item.");
					currentItem = Nodes[0].Item;
				}
				return currentItem;
			});

			set => DoWithWriteLock(() =>
			{
				if (value != _currentPath?.Item)
				{
					if (value != null)
					{
						if (!MoveCurrentTo(value, _root, out _currentPath))
						{
							Debug.WriteLine($"WARNING: Could not MoveCurrent to item: {value}.");
						}
					}
				}
			});
		}

		// TODO: Consider having the ITreeItem<U,V> Tree keep track of "CanGoBack" / "CanGoForward" as to make these real properties.
		public bool CanGoBack
		{
			get
			{
				_treeLock.EnterReadLock();

				try
				{
					return CanMoveBack(_currentPath);
				}
				finally
				{
					_treeLock.ExitReadLock();
				}
			}
		}

		public bool CanGoForward
		{
			get
			{
				_treeLock.EnterReadLock();

				try
				{
					return CanMoveForward(_currentPath);
				}
				finally
				{
					_treeLock.ExitReadLock();
				}
			}
		}

		public bool IsDirty { get; set; }

		public bool AnyItemIsDirty
		{
			get
			{
				_treeLock.EnterReadLock();

				try
				{
					return GetNodes(_root).Any(x => x.IsDirty);
				}
				finally
				{
					_treeLock.ExitReadLock();
				}

			}
		}

		#endregion

		#region Public Methods

		public ITreePath<U,V> Add(V item, bool selectTheAddedItem)
		{
			_treeLock.EnterWriteLock();

			ITreePath<U,V> newPath;

			try
			{
				newPath = AddInternal(item, currentBranch: _root);
				IsDirty = true;
			}
			finally
			{
				_treeLock.ExitWriteLock();
			}

			if (selectTheAddedItem)
			{
				ExpandAndSetCurrent(newPath);
				_currentPath = newPath;
			}

			return newPath;
		}

		public bool RemoveBranch(ObjectId id)
		{
			// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.
			if (!TryFindPathById(id, _root, out var path))
			{
				return false;
			}

			var result = RemoveBranch(path);
			return result;
		}

		public bool RemoveBranch(ITreePath<U,V> path)
		{
			return true;
		}

		public bool TryGetPreviousItem([MaybeNullWhen(false)] out V item, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			if (_currentPath == null)
			{
				item = null;
				return false;
			}

			var backPath = GetPreviousItemPath(_currentPath, predicate);
			item = backPath?.Item;

			return item != null;
		}

		public bool MoveBack(Func<ITreeNode<U,V>, bool>? predicate = null)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var backPath = GetPreviousItemPath(_currentPath, predicate);

			if (backPath != null)
			{
				_currentPath = backPath;
				ExpandAndSetCurrent(backPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetNextItem([MaybeNullWhen(false)] out V item, Func<ITreeNode<U, V>, bool>? predicate = null)
		{
			if (_currentPath == null)
			{
				item = null;
				return false;
			}

			var forwardPath = GetNextItemPath(_currentPath, predicate);
			item = forwardPath?.Item;

			return item != null;
		}

		public bool MoveForward(Func<ITreeNode<U,V>, bool>? predicate = null)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var forwardPath = GetNextItemPath(_currentPath, predicate);

			if (forwardPath != null)
			{
				_currentPath = forwardPath;
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
			_treeLock.EnterReadLock();

			try
			{
				return _currentPath == null ? null : _currentPath;
			}
			finally
			{
				_treeLock.ExitReadLock();
			}
		}

		public ITreePath<U,V>? GetPath(ObjectId id)
		{
			_treeLock.EnterReadLock();

			try
			{
				return GetPathById(id, _root);
			}
			finally
			{
				_treeLock.ExitReadLock();
			}
		}

		public IEnumerable<V> GetItems()
		{
			_treeLock.EnterReadLock();

			try
			{
				var result = GetItems(_root);
				return result;
			}
			finally
			{
				_treeLock.ExitReadLock();
			}
		}

		public V? GetItem(ObjectId id)
		{
			_ = TryFindItem(id, _root, out var result);
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
				_ = TryFindItem(node.ParentId.Value, _root, out var result);
				return result;
			}
		}

		public List<V>? GetItemAndDescendants(ObjectId id)
		{
			_treeLock.EnterReadLock();

			try
			{
				List<V>? result;

				if (TryFindPathById(id, _root, out var path))
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
				_treeLock.ExitReadLock();
			}
		}

		#endregion

		#region Private Add Methods

		// TODO: Implement AddInternal on the Tree class.
		private ITreePath<U,V> AddInternal(V item, ITreeBranch<U,V> currentBranch)
		{
			throw new NotImplementedException("AddInternal not yet implemented.");
		}

		private ITreePath<U,V> AddInLine(V item, ITreePath<U,V> parentPath)
		{
			Debug.WriteLine($"Adding item: {item}, in-line after: {parentPath.Node.Id}.");

			ITreeBranch<U,V> parentBranchToUse = parentPath.GetParentBranch();

			var result = AddItem(item, parentBranchToUse);

			return result;
		}

		private ITreePath<U,V> AddItem(V item, ITreeBranch<U,V> parentBranch)
		{
			var parentNode = parentBranch.Node;
			var newNode = (U) parentNode.AddItem(item);

			var result = parentBranch.Combine(newNode);

			return result;
		}

		#endregion

		#region Private Load and Export Item Methods

		//protected ITreeBranch<U,V> CreateTree(List<V> items, out ITreePath<U,V>? currentPath)
		//{
		//	V homeItem = items.Take(1).First();
		//	var root = new TreeBranch<U, V>(homeItem);
		//	currentPath = root.GetCurrentPath();
		//	return root;
		//}

		private IList<V> GetItems(ITreeBranch<U,V> currentBranch)
		{
			// TODO: Consider implementing an IEnumerator<ITreeItem<U,V> for the Tree class.
			var result = new List<V>();

			//foreach (var child in currentBranch.GetNodeOrRoot().Children)
			//{
			//	result.Add(child.Item);

			//	var nodeList = GetItems(currentBranch.Combine(child));
			//	result.AddRange(nodeList);
			//}

			return result;
		}

		private IList<ITreeItem<V>> GetNodes(ITreeBranch<U,V> currentBranch)
		{
			// TODO: Consider implementing an IEnumerator<ITreeItem<U,V> for the Tree class.
			var result = new List<ITreeItem<V>>();

			//foreach (var child in currentBranch.GetNodeOrRoot().Children)
			//{
			//	result.Add(child);

			//	var nodeList = GetNodes(currentBranch.Combine(child));
			//	result.AddRange(nodeList);
			//}

			return result;
		}

		private List< Tuple<ITreeItem<V>, ITreeItem<V>? > > GetNodesWithParentage(ITreeBranch<U,V> currentBranch)
		{
			var result = new List<Tuple<ITreeItem<V>, ITreeItem<V>?>>();

			//foreach (var child in currentBranch.GetNodeOrRoot().Children)
			//{
			//	result.Add(new Tuple<ITreeItem<V>, ITreeItem<V>?>(child, (U?) child.ParentNode));

			//	var nodeList = GetNodesWithParentage(currentBranch.Combine(child));
			//	result.AddRange(nodeList);
			//}

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
			return (item.Equals(path?.LastTerm)) || TryFindPathInternal(item, currentBranch, out path);
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

			var node = currentBranch.Node; // .GetNodeOrRoot();

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

			var node = currentBranch.Node; // .GetNodeOrRoot();


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

		protected bool NodeContainsItem(ITreeBranch<U,V> branch, Func<ITreeItem<V>, bool> predicate, [MaybeNullWhen(false)] out ITreePath<U,V> path)
		{
			//var foundNode = branch.GetNodeOrRoot().Children.FirstOrDefault(predicate);
			var foundNode = branch.Children.FirstOrDefault(predicate);
			path = foundNode == null ? null : branch.GetCurrentPath(); //  branch.Combine(foundNode);
			return path != null;
		}

		#endregion

		#region Private Navigate Methods

		protected ITreePath<U,V>? GetNextItemPath(ITreePath<U,V> path, Func<U, bool>? predicate = null)
		{
			var currentItem = path.Node;

			ITreePath<U,V>? result;

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (TryGetNextNode((IList<U>)siblings, currentPosition, out var nextNode, predicate))
			{
				//The new item will be a sibling of the current item
				result = path.Combine(nextNode);
			}
			else
			{
				result = null;
			}

			return result;
		}

		private bool TryGetNextNode(IList<U> nodes, int currentPosition, [MaybeNullWhen(false)] out U nextNode, Func<U, bool>? predicate = null)
		{
			if (predicate != null)
			{
				nextNode = nodes.Skip(currentPosition + 1).FirstOrDefault(predicate);
			}
			else
			{
				nextNode = nodes.Skip(currentPosition + 1).FirstOrDefault();
			}

			return nextNode != null;
		}

		private bool CanMoveForward(ITreePath<U,V>? path)
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

		protected ITreePath<U,V>? GetPreviousItemPath(ITreePath<U,V> path, Func<U, bool>? predicate = null)
		{
			var currentItem = path.LastTerm;

			if (currentItem == null)
			{
				return null;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousNode = GetPreviousNode((IList<U>)siblings, currentPosition, predicate);

			while (previousNode == null && path.Count > 1)
			{
				path = path.GetParentPath()!;
				currentItem = path.Node;

				var grandparentNode = path.GetParentNodeOrRoot();
				var ancestors = grandparentNode.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousNode = GetPreviousNode((IList<U>)ancestors, currentPosition + 1, predicate);
			}

			if (previousNode != null)
			{
				var result = path.Combine(previousNode);

				return result;
			}
			else
			{
				return null;
			}
		}

		private U? GetPreviousNode(IList<U> nodes, int currentPosition, Func<U, bool>? predicate = null)
		{
			U? result;

			if (predicate != null)
			{
				result = nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault(predicate);
			}
			else
			{
				result = nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault();
			}

			return result;
		}

		private bool CanMoveBack(ITreePath<U,V>? path)
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

		protected bool MoveCurrentTo(V item, ITreeBranch<U,V> currentBranch, [MaybeNullWhen(false)] out ITreePath<U,V> path)
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

		protected void ExpandAndSetCurrent(ITreePath<U,V> path)
		{
			if (path.IsEmpty)
			{
				return;
			}

			foreach (var p in path.Terms.SkipLast(1))
			{
				p.IsExpanded = true;
			}

			var lastTerm = path.Node;
			lastTerm.IsCurrent = true;
		}

		#endregion

		#region Lock Helpers

		private V DoWithReadLock(Func<V> function)
		{
			_treeLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_treeLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_treeLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_treeLock.ExitWriteLock();
			}
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			((IDisposable)_treeLock).Dispose();
		}

		#endregion

	}
}
