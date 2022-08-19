using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSS.Types
{
	public interface ITreeNode<U, V> : ITreeItem<V>, ICloneable where U: ITreeItem<V> where V : IEquatable<V>, IComparable<V>
	{
		ITreeNode<U, V>? ParentNode { get; set; }
		U Node { get; }
		ObservableCollection<ITreeNode<U, V>> Children { get; init; }

		bool IsHome { get; }
		bool IsOrphan { get; }
		bool IsRoot { get; init; }

		bool IsCurrent { get; set; }
		bool IsExpanded { get; set; }

		U AddItem(V item);
		void AddNode(ITreeNode<U,V> node);

		List<ITreeNode<U, V>> GetAncestors();
		int GetSortPosition(V item);
		bool Move(ITreeNode<U, V> destination);
		bool Remove(ITreeNode<U,V> node);
	}

	public interface ITreeItem<V> where V: IEquatable<V>, IComparable<V>
	{
		V Item { get; init; }
		ObjectId Id { get; }
		ObjectId? ParentId { get; }
		bool IsDirty { get; set; }
	}
}