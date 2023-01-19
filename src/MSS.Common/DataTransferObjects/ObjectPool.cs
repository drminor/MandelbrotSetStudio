using System;
using System.Collections.Generic;

//TODO: Implement the IDisposable interface to allow pooled objects to be freed by the using keyword.
public interface IPoolable
{
	void ResetObject();
}

public abstract class ObjectPool<T> where T : IPoolable
{

	/// <summary>The maximum number of free objects pooled.</summary>
	public int MaxSize { get; private set; }

	/// <summary>The maximum number of free objects this pool has had.</summary>
	public int MaxPeak { get; private set; }

	/// <summary>The total free objects within this pool.</summary>
	public int TotalFree { get { return pool.Count; } }

	///<summary>The pool of free objects, waiting to be obtained.</summary> 
	private Stack<T> pool;

	/// <summary>
	/// Create a new <see cref="ObjectPool{T}"/>.
	/// </summary>
	/// <param name="initialSize">The initial size of this pool.</param>
	/// <param name="maxSize">The maximum number of free objects that can be pooled.</param>
	public ObjectPool(int initialSize = 16, int maxSize = int.MaxValue)
	{
		this.pool = new Stack<T>(initialSize);
		this.MaxSize = maxSize;
	}

	/// <summary>
	/// Returns a new poolable object.
	/// </summary>
	/// <returns>Returns a new poolable object.</returns>
	protected abstract T NewObject();

	/// <summary>
	/// Returns a pooled object if one is available, or a new object.
	/// </summary>
	/// <returns>Returns a pooled object if one is available, or a new object.</returns>
	public T Obtain()
	{
		return (TotalFree == 0) ? NewObject() : pool.Pop();
	}

	/// <summary>
	/// Reset the provided object, if the total free objects is under the max size of this pool it will be added back into the pool.<br/>
	/// Note: The object will be reset even if it can't be added to the pool.
	/// </summary>
	/// <param name="obj">The object to reset and add back into the pool.</param>
	/// <returns>True if the object was added back into the pool.</returns>
	public bool Free(T obj)
	{
		if (obj == null)
			return false;

		Reset(obj);
		if (TotalFree < MaxSize)
		{
			pool.Push(obj);
			MaxPeak = Math.Max(MaxPeak, TotalFree);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Resets of the provided object. Override to implement custom object resetting logic. <br/>
	/// The default implementation uses the <see cref="IPoolable"/> interface to reset the object.
	/// </summary>
	/// <param name="obj"></param>
	protected virtual void Reset(T obj)
	{
		IPoolable poolObj = obj as IPoolable;
		if (poolObj != null)
			poolObj.ResetObject();
	}

	/// <summary>
	/// Clears the pool.
	/// </summary>
	/// <param name="clearPeak">If true <see cref="MaxPeak"/> will also be reset.</param>
	public void Clear(bool clearPeak = false)
	{
		pool.Clear();
		if (clearPeak)
			MaxPeak = 0;
	}

	public override string ToString()
	{
		return string.Format("{0} [F: {1} / MP: {2}]", this.GetType().Name, TotalFree, MaxPeak);
	}
}
