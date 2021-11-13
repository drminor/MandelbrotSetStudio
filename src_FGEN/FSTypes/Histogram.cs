using System.Collections.Generic;
using System.Linq;

namespace FSTypes
{
	public class Histogram
	{
		public readonly int JobId;

		public readonly int[] Values;

		public readonly int[] Occurances;

		public Histogram(int jobId, IDictionary<int, int> hDictionary)
		{
			JobId = jobId;
			if (hDictionary != null)
			{
				Values = hDictionary.Keys.ToArray();
				Occurances = hDictionary.Values.ToArray();
			}
			else
			{
				Values = new int[0];
				Occurances = new int[0];
			}
		}
	}

}
