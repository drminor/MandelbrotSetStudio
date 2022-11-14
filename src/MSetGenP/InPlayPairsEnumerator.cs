using System.Collections;

namespace MSetGenP
{
	public class InPlayPairsEnumerator<T> : IEnumerator<ValueTuple<T, T>> where T : struct
	{
		private readonly T[] _array1;
		private readonly T[] _array2;
		//private readonly bool[] _includeFlags;

		private int _position;

		public InPlayPairsEnumerator(T[] array1, T[] array2)
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

		object IEnumerator.Current => Current;

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

		#region IDisposable Support

		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ArrayOfPairsEnumerator()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
