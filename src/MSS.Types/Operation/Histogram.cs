using System.Collections.Generic;
using System.Linq;

namespace MSS.Types
{
	public class Histogram
	{
		IDictionary<int, int> Entries;

		public Histogram()
		{
			Entries = new Dictionary<int, int>();
		}

		public Histogram(IDictionary<int, int> entries) : this(entries.Keys.ToArray(), entries.Values.ToArray())
		{
		}

		public Histogram(int[] values, int[] occurances)
		{
			Entries = new Dictionary<int, int>();
			for (int i = 0; i < values.Length; i++)
			{
				Entries.Add(values[i], occurances[i]);
			}
		}
	}

}
