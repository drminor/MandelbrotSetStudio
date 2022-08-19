using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public interface ITreeBranch<U, V> : ICloneable where U : class, ITreeNode<U, V> where V : class, IEquatable<V>, IComparable<V>
	{
		V? Item { get; }
		ObservableCollection<U> Children { get; }

		int Count { get; }
		bool IsEmpty { get; }

		U Node { get; }

		List<U> Terms { get; init; }

		U? LastTerm { get; }
		U? ParentTerm { get; }
		U? GrandparentTerm { get; }

		ITreePath<U,V>? GetCurrentPath();

		U? GetParentNode();
		ITreePath<U,V>? GetParentPath();
		ITreeBranch<U,V> GetParentBranch();

		ITreeBranch<U,V> GetRoot();
		U GetNodeOrRoot();
		U GetParentNodeOrRoot();

		bool TryGetParentNode([MaybeNullWhen(false)] out U parentItem);
		bool TryGetParentPath([MaybeNullWhen(false)] out ITreePath<U,V> parentPath);
		bool TryGetGrandparentPath([MaybeNullWhen(false)] out ITreePath<U,V> grandparentPath);

		ITreePath<U,V> Combine(U node);
		 ITreePath<U,V> Combine(ITreePath<U,V> treePath);
		ITreePath<U,V> Combine(IEnumerable<U> nodes);
	}
}