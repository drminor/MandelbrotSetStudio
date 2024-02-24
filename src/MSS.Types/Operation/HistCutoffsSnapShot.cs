using System.Collections.Generic;

namespace MSS.Types
{
	public class HistCutoffsSnapShot
	{
		public HistCutoffsSnapShot(KeyValuePair<int, int>[] histKeyValuePairs, int histogramLength, long upperCatchAllValue, int[] cutoffs)
		{
			HistKeyValuePairs = histKeyValuePairs;
			HistogramLength = histogramLength;
			UpperCatchAllValue = upperCatchAllValue;
			Cutoffs = cutoffs;
		}

		public KeyValuePair<int, int>[] HistKeyValuePairs { get; init; }

		public int HistogramLength { get; init; }
		public long UpperCatchAllValue { get; init; }

		public int[] Cutoffs { get; init; }

	}
}
