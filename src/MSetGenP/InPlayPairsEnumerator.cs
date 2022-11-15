namespace MSetGenP
{
	public ref struct InPlayPairsEnumerator<T> 
	{
		private readonly ReadOnlySpan<T> _array1;
		private readonly ReadOnlySpan<T> _array2;
		//private readonly bool[] _includeFlags;

		private int _position;

		public InPlayPairsEnumerator(ReadOnlySpan<T> array1, ReadOnlySpan<T> array2)
		{
			_array1 = array1;
			_array2 = array2;
			//_includeFlags = Enumerable.Repeat(true, array1.Length).ToArray();

			_position = -1;
		}

		//public ArrayOfPairsEnumerator(T[] array1, T[] array2, bool[] includeFlags)
		//{
		//	_array1 = array1;
		//	_array2 = array2;
		//	_includeFlags = includeFlags;

		//	_position = -1;
		//}

		public (T, T) Current => (_array1[_position], _array2[_position]);

		public bool MoveNext()
		{
			if (_position < _array1.Length - 1)
			{
				_position++;
				return true;
			}
			else
			{
				return false;
			}
		}

		public void Reset()
		{
			_position = -1;
		}

	}
}
