using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public interface ITree<U,V> : IDisposable where U : class, ITreeNode<U,V> where V : class, IEquatable<V>, IComparable<V>
	{
		ObservableCollection<U> Nodes { get; }

		V CurrentItem { get; set; }
		bool IsDirty { get; set; }
		bool AnyItemIsDirty { get; }

		ITreeNode<U,V>? SelectedNode { get; set; }

		ITreePath<U,V>? GetCurrentPath();
		ITreePath<U,V>? GetPath(ObjectId itemId);

		ITreePath<U,V> Add(V item, bool selectTheAddedItem);

		//bool RestoreBranch(ObjectId itemId);
		//bool RestoreBranch(ITreePath<V> path);

		bool RemoveBranch(ObjectId itemId);
		bool RemoveBranch(ITreePath<U, V> path);

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool TryGetNextItem([MaybeNullWhen(false)] out V item, Func<ITreeNode<U, V>, bool>? predicate = null);
		bool TryGetPreviousItem([MaybeNullWhen(false)] out V item, Func<ITreeNode<U, V>, bool>? predicate = null);

		bool MoveBack(Func<ITreeNode<U, V>, bool>? predicate = null);
		bool MoveForward(Func<ITreeNode<U, V>, bool>? predicate = null);

		V? GetParentItem(U node);
		V? GetItem(ObjectId itemId);

		IEnumerable<V> GetItems();
		List<V>? GetItemAndDescendants(ObjectId itemId);
	}
}