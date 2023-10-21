using System;
using System.Collections.Generic;

namespace MSS.Types
{
	public class MapSectionZVectorsPool
	{
		private object _stateLock;

		public int MaxSize { get; private set; }

		public int MaxPeak { get; private set; }

		public int TotalFree { get { return _pool.Count; } }

		protected Stack<MapSectionZVectors> _pool;

		public MapSectionZVectorsPool(SizeInt blockSize, int limbCount, int initialSize = 16, int maxSize = int.MaxValue)
		{
			BlockSize = blockSize;

			_stateLock = new object();
			_pool = new Stack<MapSectionZVectors>(initialSize);
			MaxSize = maxSize;
			MaxPeak = 0;


			Fill(initialSize, limbCount);
		}

		public SizeInt BlockSize { get; init; }

		protected void Fill(int amount, int limbCount)
		{
			lock (_stateLock)
			{
				for (var i = 0; i < amount; i++)
				{
					_pool.Push(NewObject(limbCount));
				}
				MaxPeak = Math.Max(MaxPeak, TotalFree);
			}
		}

		private MapSectionZVectors NewObject(int limbCount)
		{
			return new MapSectionZVectors(BlockSize, limbCount);
		}

		public MapSectionZVectors Obtain(int limbCount)
		{
			lock (_stateLock)
			{
				MapSectionZVectors result;

				if (TotalFree == 0)
				{
					result = NewObject(limbCount);
				}
				else
				{
					result = _pool.Pop();
					if (result.LimbCount != limbCount)
					{
						result.LimbCount = limbCount;
					}
				}

				result.IncreaseRefCount();
				return result;
			}
		}

		public bool Free(MapSectionZVectors obj)
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

		protected virtual void Reset(MapSectionZVectors obj)
		{
			if (obj != null)
			{
				obj.ResetObject();
			}
		}

		public void Clear(bool clearPeak = false)
		{
			lock (_stateLock)
			{
				while (_pool.TryPop(out var obj))
				{
					obj?.Dispose();
				}

				if (clearPeak)
				{
					MaxPeak = 0;
				}
			}
		}

		//public virtual MapSectionZVectors DuplicateFrom(MapSectionZVectors obj)
		//{
		//	if (obj == null)
		//	{
		//		throw new ArgumentException("DuplicateFrom must be supplied a non-null value.");
		//	}

		//	var source = Obtain(obj.LimbCount);
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
