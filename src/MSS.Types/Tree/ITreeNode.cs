using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSS.Types
{
	public interface ITreeNode<U, V> : ITreeItem<V>, ICloneable where U: class, ITreeNode<U,V> where V : IEquatable<V>, IComparable<V>
	{
		U? ParentNode { get; set; }
		ObservableCollection<U> Children { get; init; }

		bool IsHome { get; }
		bool IsOrphan { get; }
		bool IsRoot { get; init; }

		bool IsCurrent { get; set; }
		bool IsExpanded { get; set; }

		U AddItem(V item);
		void AddNode(U node);

		IList<U> GetAncestors();
		int GetSortPosition(U node);
		bool Move(U destination);
		bool Remove(U node);
	}

	public interface ITreeItem<V> where V: IEquatable<V>, IComparable<V>
	{
		V Item { get; init; }
		ObjectId Id { get; }
		ObjectId? ParentId { get; }
		bool IsDirty { get; set; }
	}
}