using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSS.Types
{
	public abstract class ObjectPool<T> where T : IPoolable
	{
		private object _stateLock;

		/// <summary>The maximum number of free objects pooled.</summary>
		public int MaxSize { get; private set; }

		/// <summary>The maximum number of free objects this pool has had.</summary>
		public int MaxPeak { get; private set; }

		/// <summary>The total free objects within this pool.</summary>
		public int TotalFree { get { return _pool.Count; } }

		///<summary>The pool of free objects, waiting to be obtained.</summary> 
		protected Stack<T> _pool;

		/// <summary>
		/// Create a new <see cref="ObjectPool{T}"/>.
		/// </summary>
		/// <param name="initialSize">The initial size of this pool.</param>
		/// <param name="maxSize">The maximum number of free objects that can be pooled.</param>
		public ObjectPool(int initialSize = 16, int maxSize = int.MaxValue)
		{
			_stateLock = new object();
			_pool = new Stack<T>(initialSize);
			MaxSize = maxSize;
			MaxPeak = 0;
		}

		protected void Fill(int amount)
		{
			lock (_stateLock)
			{
				for (var i = 0; i < amount; i++)
				{
					_pool.Push(NewObject());
				}
				MaxPeak = Math.Max(MaxPeak, TotalFree);
			}
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
			lock(_stateLock)
			{
				T result;

				if (TotalFree == 0)
				{
					result = NewObject();
				}
				else
				{
					result = _pool.Pop();
				}

				result.IncreaseRefCount();
				return result;
			}
		}

		/// <summary>
		/// Reset the provided object, if the total free objects is under the max size of this pool it will be added back into the pool.<br/>
		/// Note: The object will be reset even if it can't be added to the pool.
		/// </summary>
		/// <param name="obj">The object to reset and add back into the pool.</param>
		/// <returns>True if the object was added back into the pool.</returns>
		public bool Free(T obj)
		{
			bool result;

			if (obj == null)
			{
				result = false;
			}
			else
			{
				lock (_stateLock)
				{
					obj.DecreaseRefCount();

					Debug.Assert(obj.ReferenceCount >= 0, "The ReferenceCount should never be less than zero.");


					if (obj.ReferenceCount > 0)
					{
						result = false;
					}
					else
					{
						if (TotalFree < MaxSize)
						{
							//Reset(obj);
							_pool.Push(obj);
							MaxPeak = Math.Max(MaxPeak, TotalFree);
						}
						else
						{
							obj.Dispose();
						}

						result = true;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Resets of the provided object. Override to implement custom object resetting logic. <br/>
		/// The default implementation uses the <see cref="IPoolable"/> interface to reset the object.
		/// </summary>
		/// <param name="obj"></param>
		protected virtual void Reset(T obj)
		{
			if (obj != null)
			{
				obj.ResetObject();
			}
		}

		/// <summary>
		/// Clears the pool.
		/// </summary>
		/// <param name="clearPeak">If true <see cref="MaxPeak"/> will also be reset.</param>
		public void Clear(bool clearPeak = false)
		{
			lock (_stateLock)
			{
				while(_pool.TryPop(out var obj))
				{
					obj?.Dispose();
				}

				if (clearPeak)
				{
					MaxPeak = 0;
				}
			}
		}

		//public virtual T DuplicateFrom(T obj)
		//{
		//	if (obj == null)
		//	{
		//		throw new ArgumentException("DuplicateFrom must be supplied a non-null value.");
		//	}

		//	var source = Obtain();
		//	obj.CopyTo(source);
		//	var result = source;

		//	return result;
		//}

		public override string ToString()
		{
			return string.Format("{0} [F: {1} / MP: {2}]", this.GetType().Name, TotalFree, MaxPeak);
		}
	}
}
