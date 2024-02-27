using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Types
{
	/// <summary>
	/// Array based Histogram used to hold data for a single MapSection
	/// </summary>
	public class HistogramALow : IHistogram
	{
		private int[] _values;
		private readonly int _lowBound;

		#region Constructor

		//public HistogramALow(int low, int high)
		//{
		//	_values = new int[1 + high - low];
		//	_lowBound = low;
		//}

		//public HistogramALow(int[] values)
		//{
		//	var low = values.Min();
		//	var high = values.Max();

		//	_values = new int[1 + high - low];
		//	_lowBound = low;

		//	for (var ptr = 0; ptr < values.Length; ptr++)
		//	{
		//		_ = Increment(values[ptr]);
		//	}
		//}

		public HistogramALow(IEnumerable<ushort> values)
		{
			if (values.Count() == 0)
			{
				IsEmpty = true;
				_values = Array.Empty<int>();
				_lowBound = 0;
			}
			else
			{
				IsEmpty = false;

				var low = values.Min();
				var high = values.Max();

				// TODO: Consider using ArrayPool<int>.Shared

				_values = new int[1 + high - low];
				_lowBound = low;

				foreach (var val in values)
				{
					_ = Increment(val);
				}
			}
		}

		public HistogramALow(IDictionary<int, int> keyValuePairs)
		{
			if (keyValuePairs.Count() == 0)
			{
				IsEmpty = true;
				_values = Array.Empty<int>();
				_lowBound = 0;
			}
			else
			{
				IsEmpty = false;

				var low = keyValuePairs.Min(x => x.Key);
				var high = keyValuePairs.Max(x => x.Key);

				_values = new int[1 + high - low];
				_lowBound = low;

				foreach (var kvp in keyValuePairs)
				{
					var aI = kvp.Key - _lowBound;
					_values[aI] = kvp.Value;
				}
			}
		}

		//public HistogramALow(IDictionary<int, int> entries)
		//{
		//	var low = entries.Min(x => x.Key);
		//	var high = entries.Max(x => x.Key);

		//	_values = new int[1 + high - low];
		//	_lowBound = low;

		//	Set(entries.Keys, entries.Values);
		//}

		//public HistogramALow(int[] values, int[] occurances)
		//{
		//	var low = values.Min();
		//	var high = values.Max();

		//	_values = new int[1 + high - low];
		//	_lowBound = low;

		//	Set(values, occurances);
		//}

		#endregion

		#region Public Properties

		public int[] Values => _values;
		public int LowerBound => _lowBound;
		public int UpperBound => _values.Length - 1 + _lowBound;
		public int Length => _values.Length;

		public bool IsEmpty { get; set; }

		public long UpperCatchAllValue { get; set; }

		public int this[int index]
		{
			get => _values[index - _lowBound];
			set => _values[index - _lowBound] = value;
		}

		#endregion

		#region Public Methods

		public double GetAverageMaxIndex() => throw new NotImplementedException();

		public KeyValuePair<int, int>[] GetKeyValuePairs()
		{
			var cnt = _values.Count(x => x != 0);
			var result = new KeyValuePair<int, int>[cnt];
			var rPtr = 0;

			for (var i = 0; i < _values.Length; i++)
			{
				if (_values[i] != 0)
				{
					result[rPtr++] = new KeyValuePair<int, int>(i + _lowBound, _values[i]);
				}
			}

			return result;
		}

		public IEnumerable<KeyValuePair<int, int>> GetKeyValuePairs2()
		{
			for (var i = 0; i < _values.Length; i++)
			{
				if (_values[i] != 0)
				{
					yield return new KeyValuePair<int, int>(i + _lowBound, _values[i]);
				}
			}
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
		//		_values[i - _lowBound] = amounts[ptr];
		//	}
		//}

		//public void Set(ICollection<int> indexes, ICollection<int> amounts)
		//{
		//	var aEnumerator = amounts.GetEnumerator();

		//	foreach (var i in indexes)
		//	{
		//		_ = aEnumerator.MoveNext();
		//		_values[i - _lowBound] = aEnumerator.Current;
		//	}
		//}

		private int Increment(int index)
		{
			try
			{
				var aI = index - _lowBound;
				_values[aI] = _values[aI] + 1;

				return _values[aI];
			}
			catch (Exception e)
			{
				Debug.WriteLine($"HistogramALow got exception {e} while Incrementing.");
				return 0;
			}
		}

		//public int Decrement(int index)
		//{
		//	var aI = index - _lowBound;
		//	_values[aI] = _values[aI] - 1;

		//	return _values[aI];
		//}

		//public void Increment(int[] indexes)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr] - _lowBound;
		//		_values[i] = _values[i] + 1;
		//	}
		//}

		//public void Decrement(int[] indexes)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var i = indexes[ptr] - _lowBound;
		//		_values[i] = _values[i] - 1;
		//	}
		//}

		//public void Increment(ICollection<int> indexes)
		//{
		//	foreach (var i in indexes)
		//	{
		//		_values[i - _lowBound] = _values[i - _lowBound] + 1;
		//	}
		//}

		//public void Decrement(ICollection<int> indexes)
		//{
		//	foreach (var i in indexes)
		//	{
		//		_values[i - _lowBound] = _values[i - _lowBound] - 1;
		//	}
		//}

		//public int Add(int index, int amount)
		//{
		//	var aI = index - _lowBound;
		//	_values[aI] = _values[aI] + amount;

		//	return _values[aI];
		//}

		//public int Remove(int index, int amount)
		//{
		//	var aI = index - _lowBound;
		//	_values[aI] = _values[aI] - amount;

		//	return _values[aI];
		//}

		//public void Add(int[] indexes, int[] amounts)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var aI = indexes[ptr] - _lowBound;
		//		_values[aI] = _values[aI] += amounts[ptr];
		//	}
		//}

		//public void Remove(int[] indexes, int[] amounts)
		//{
		//	for (var ptr = 0; ptr < indexes.Length; ptr++)
		//	{
		//		var aI = indexes[ptr] - _lowBound;
		//		_values[aI] = _values[aI] -= amounts[ptr];
		//	}
		//}

		public void Add(IHistogram histogram)
		{
			if (histogram == null)
			{
				return;
			}

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
				_values[i - _lowBound] = _values[i - _lowBound] + aEnumerator.Current;
			}
		}

		public void Remove(IHistogram histogram)
		{
			if (histogram == null)
			{
				return;
			}

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
				_values[i - _lowBound] = _values[i - _lowBound] - aEnumerator.Current;
			}
		}

		#endregion
	}
}
