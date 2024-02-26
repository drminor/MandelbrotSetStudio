using System.Collections.Generic;
using System.Linq;

namespace MSS.Types
{
	public class HistCutoffsSnapShot
	{
		public HistCutoffsSnapShot(KeyValuePair<int, int>[] histKeyValuePairs, int histogramLength, long upperCatchAllValue, bool havePercentages, PercentageBand[] percentageBands)
		{
			HistKeyValuePairs = histKeyValuePairs;
			HistogramLength = histogramLength;
			UpperCatchAllValue = upperCatchAllValue;
			//Cutoffs = cutoffs;
			HavePercentages = havePercentages;
			PercentageBands = percentageBands;
		}

		public KeyValuePair<int, int>[] HistKeyValuePairs { get; init; }

		public int HistogramLength { get; init; }
		public long UpperCatchAllValue { get; init; }


		public bool HavePercentages { get; init; }
		public PercentageBand[] PercentageBands { get; init; }

		public int CutoffsLength => PercentageBands.Length;

		public int[] GetCutoffs()
		{
			var result = PercentageBands.Select(x => x.Cutoff).ToArray();
			return result;
		}

	}
}
