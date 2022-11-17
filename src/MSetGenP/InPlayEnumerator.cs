using System.Numerics;

namespace MSetGenP
{
	public ref struct InPlayEnumerator<T>
	{
		private readonly Span<T> _array1;
		//private readonly bool[] _includeFlags;

		private int _position;

		public InPlayEnumerator(Span<T> array1)
		{
			_array1 = array1;
			//_includeFlags = Enumerable.Repeat(true, array1.Length).ToArray();

			_position = -1;
		}

		//public ArrayOfPairsEnumerator(T[] array1, bool[] includeFlags)
		//{
		//	_array1 = array1;
		//	_includeFlags = includeFlags;

		//	_position = -1;
		//}

		public T Current => _array1[_position];

		public T this[int index]
		{
			get => _array1[index];
			set => _array1[index] = value;
		}

		public void WriteValues(T[] values)
		{
			for(var i = 0; i < values.Length; i++)
			{
				_array1[i] = values[i];
			}
		}

		public bool MoveNext()
		{
			if (_position < _array1.Length -1)
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
