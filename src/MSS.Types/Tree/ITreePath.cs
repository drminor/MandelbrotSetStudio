using System;

namespace MSS.Types
{
	public interface ITreePath<U, V> : ITreeBranch<U, V> where U : class, ITreeNode<U, V> where V : class, IEquatable<V>, IComparable<V>
	{
		U Node { get; }
		V Item { get; }

		ITreePath<U, V> CreateSiblingPath(U childNode);

	}
}