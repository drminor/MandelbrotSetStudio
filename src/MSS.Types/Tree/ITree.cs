﻿using MongoDB.Bson;
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

		U? SelectedNode { get; set; }

		ITreePath<U,V>? GetCurrentPath();
		ITreePath<U,V>? GetPath(ObjectId itemId);
		V? GetItem(ObjectId itemId);

		ITreePath<U,V> Add(V item, bool selectTheAddedItem);

		bool RemoveNode(ITreePath<U, V> path);

		bool RemoveBranch(ObjectId itemId);
		bool RemoveBranch(ITreePath<U, V> path);

		bool TryGetNextItemPath([MaybeNullWhen(false)] out ITreePath<U, V> forwardPath, Func<U, bool>? predicate);
		bool TryGetPreviousItemPath([MaybeNullWhen(false)] out ITreePath<U, V> backPath, Func<U, bool>? predicate);

		bool MoveBack(Func<U, bool>? predicate);
		bool MoveForward(Func<U, bool>? predicate);

		IEnumerable<V> GetItems();
		IList<V>? GetItemAndDescendants(ObjectId itemId);
	}
}