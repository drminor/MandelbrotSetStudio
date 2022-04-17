using System;
using System.Collections.Generic;
using System.Linq;

namespace MSS.Types
{
	public class HistogramA : IHistogram
	{
		private int[] _values;

		#region Constructor

		public HistogramA(int size)
		{
			_values = new int[size];
		}

		public HistogramA(int[] values)
		{
			var m = values.Max();
			_values = new int[m];

			for(var ptr = 0; ptr < values.Length; ptr++)
			{
				_ = Increment(values[ptr]);
			}
		}

		//public HistogramA(IDictionary<int, int> entries)
		//{
		//	var m = entries.Max(x => x.Key);

		//	_values = new int[m];

		//	Set(entries.Keys, entries.Values);
		//}

		//public HistogramA(int[] values, int[] occurances)
		//{
		//	var m = values.Max();

		//	_values = new int[m];

		//	Set(values, occurances);
		//}

		#endregion

		#region Public Properties

		public int[] Values => _values;
		public int LowerBound => 0;
		public int UpperBound => _values.Length - 1;
		public int Length => _values.Length;

		public long UpperCatchAllValue { get; set; }

		public int this[int index]
		{
			get => _values[index];
			set => _values[index] = value;
		}

		#endregion

		#region Public Methods

		public KeyValuePair<int, int>[] GetKeyValuePairs()
		{
			var cnt = _values.Count(x => x != 0);
			var result = new KeyValuePair<int, int>[cnt];
			var rPtr = 0;

			for (var i = 0; i < _values.Length; i++)
			{
				if (_values[i] != 0)
				{
					result[rPtr++] = new KeyValuePair<int, int>(i, _values[i]);
				}
			}

			return result;
		}

		public void Reset()
		{
			for (var ptr = 0; ptr < _values.Length; ptr++)
			{
				_values[ptr] = 0;
			}
		}

		public void Reset(int newSize)
		{
			_values = new int[newSize + 1];
		}

		//public void Set(int[] indexes, int[] amounts)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr];
		//		_values[i] = amounts[ptr];
		//	}
		//}

		//public void Set(ICollection<int> indexes, ICollection<int> amounts)
		//{
		//	var aEnumerator = amounts.GetEnumerator();

		//	foreach (var i in indexes)
		//	{
		//		_ = aEnumerator.MoveNext();
		//		_values[i] = aEnumerator.Current;
		//	}
		//}

		public int Increment(int index)
		{
			_values[index] = _values[index] + 1;

			return _values[index];
		}

		//public int Decrement(int index)
		//{
		//	_values[index] = _values[index] - 1;

		//	return _values[index];
		//}

		//public void Increment(int[] indexes)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr];
		//		_values[i] = _values[i] + 1;
		//	}
		//}

		//public void Decrement(int[] indexes)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr];
		//		_values[i] = _values[i] - 1;
		//	}
		//}

		//public void Increment(ICollection<int> indexes)
		//{
		//	foreach (var i in indexes)
		//	{
		//		_values[i] = _values[i] + 1;
		//	}
		//}

		//public void Decrement(ICollection<int> indexes)
		//{
		//	foreach (var i in indexes)
		//	{
		//		_values[i] = _values[i] - 1;
		//	}
		//}

		//public int Add(int index, int amount)
		//{
		//	_values[index] = _values[index] + amount;

		//	return _values[index];
		//}

		//public int Remove(int index, int amount)
		//{
		//	_values[index] = _values[index] - amount;

		//	return _values[index];
		//}

		//public void Add(int[] indexes, int[] amounts)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr];
		//		_values[i] = _values[i] += amounts[ptr];
		//	}
		//}

		//public void Remove(int[] indexes, int[] amounts)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr];
		//		_values[i] = _values[i] -= amounts[ptr];
		//	}
		//}

		public void Add(IHistogram histogram)
		{
			var kvps = histogram.GetKeyValuePairs();

			var keys = kvps.Select(x => x.Key).ToList();
			var values = kvps.Select(x => x.Value).ToList();

			Add(keys, values);
		}

		private void Add(ICollection<int> indexes, ICollection<int> amounts)
		{
			var aEnumerator = amounts.GetEnumerator();

			foreach (var i in indexes)
			{
				_ = aEnumerator.MoveNext();
				if (i > Length - 1)
				{
					UpperCatchAllValue += aEnumerator.Current;
				}
				else
				{
					_values[i] = _values[i] + aEnumerator.Current;
				}

			}
		}

		public void Remove(IHistogram histogram)
		{
			var kvps = histogram.GetKeyValuePairs();

			var keys = kvps.Select(x => x.Key).ToList();
			var values = kvps.Select(x => x.Value).ToList();

			Remove(keys, values);
		}

		private void Remove(ICollection<int> indexes, ICollection<int> amounts)
		{
			var aEnumerator = amounts.GetEnumerator();

			foreach (var i in indexes)
			{
				_ = aEnumerator.MoveNext();
				if (i > Length - 1)
				{
					UpperCatchAllValue -= aEnumerator.Current;
				}
				else
				{
					_values[i] = _values[i] - aEnumerator.Current;
				}
			}
		}

		#endregion
	}
}
