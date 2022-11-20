namespace MSetGenP
{
	internal class ShiftedArray<T> where T : struct
	{
		public ShiftedArray()
		{
			Array = new T[0];
			Offset = 0;
			Extension = 0;
		}

		public ShiftedArray(T[] array, int offset)
		{
			Array = array ?? throw new ArgumentNullException(nameof(array));
			Offset = offset;
			Extension = 0;
		}

		public T[] Array { get; set; }
		public int Offset { get; set; }
		public int Extension { get; set; }

		public int Length => Array.Length + Offset + Extension;

		public T this[int index]
		{
			get
			{
				if (index > Length - 1)
				{
					throw new IndexOutOfRangeException();
				}

				if (index < Offset || index > Length - 1 - Extension)
				{
					return default;
				}

				return Array[index - Offset];
			}
		}


	}
}
