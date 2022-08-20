using System;

namespace MSS.Types
{
	public interface ITreePath<U, V> : ITreeBranch<U, V> where U : class, ITreeNode<U, V> where V : class, IEquatable<V>, IComparable<V>
	{
		//bool IsHome { get; }
		//bool IsRoot { get; }
		U NodeSafe { get; }
		V ItemSafe { get; }

		ITreePath<U, V> CreateSiblingPath(U childNode);

	}
}