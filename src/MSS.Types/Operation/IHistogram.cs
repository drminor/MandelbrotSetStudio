﻿using System.Collections.Generic;

namespace MSS.Types
{
	public interface IHistogram
	{
		int[] Values { get; }
		int LowerBound { get; }
		int UpperBound { get; }
		int Length { get; }

		bool IsEmpty { get; }

		int this[int index] { get; set; }

		long UpperCatchAllValue { get; set; }

		double GetAverageMaxIndex();
		KeyValuePair<int, int>[] GetKeyValuePairs();

		IEnumerable<KeyValuePair<int, int>> GetKeyValuePairs2();

		void Reset();
		void Reset(int newSize);

		//int Add(int index, int amount);
		//void Add(int[] indexes, int[] amounts);
		//void Add(ICollection<int> indexes, ICollection<int> amounts);
		void Add(IHistogram histogram);

		//int Remove(int index, int amount);
		//void Remove(int[] indexes, int[] amounts);
		//void Remove(ICollection<int> indexes, ICollection<int> amounts);
		void Remove(IHistogram histogram);

		//int Increment(int index);

		//void Increment(int[] indexes);
		//void Increment(ICollection<int> indexes);

		//int Decrement(int index);
		//void Decrement(int[] indexes);
		//void Decrement(ICollection<int> indexes);

		//void Set(ICollection<int> indexes, ICollection<int> amounts);
		//void Set(int[] indexes, int[] amounts);

	}
}