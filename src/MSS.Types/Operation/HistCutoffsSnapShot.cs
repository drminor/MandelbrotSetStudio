using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;

namespace MSS.Types
{
	public class HistCutoffsSnapShot
	{
		public HistCutoffsSnapShot(ObjectId colorBandSetId, KeyValuePair<int, int>[] histKeyValuePairs, int histogramLength, long upperCatchAllValue, PercentageBand[] percentageBands, bool usingPercentages)
		{
			ColorBandSetId = colorBandSetId;

			HistKeyValuePairs = histKeyValuePairs;
			HistogramLength = histogramLength;
			UpperCatchAllValue = upperCatchAllValue;
			PercentageBands = percentageBands;

			NoPercentageIsNaN = percentageBands.All(x => !double.IsNaN(x.Percentage));
			AtLeastOnePercentageIsNonZero = percentageBands.Any(x => x.Percentage != 0);
		}

		public ObjectId ColorBandSetId { get; init; }

		public KeyValuePair<int, int>[] HistKeyValuePairs { get; init; }

		public int HistogramLength { get; init; }
		public long UpperCatchAllValue { get; init; }

		public PercentageBand[] PercentageBands { get; init; }
		public bool UsingPercentages { get; init; }

		public bool NoPercentageIsNaN { get; init; }
		public bool AtLeastOnePercentageIsNonZero { get; init; }
		public bool HavePercentages => NoPercentageIsNaN && AtLeastOnePercentageIsNonZero;


		public int CutoffsLength => PercentageBands.Length;

		public int[] GetCutoffs()
		{
			var result = PercentageBands.Select(x => x.Cutoff).ToArray();
			return result;
		}

	}
}
