using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Types
{
	public class HistogramD
	{
		#region Constructor

		public HistogramD()
		{
			Entries = new Dictionary<int, int>();
		}

		public HistogramD(IDictionary<int, int> entries) : this(entries.Keys.ToArray(), entries.Values.ToArray())
		{
		}

		public HistogramD(int[] values, int[] occurances)
		{
			Entries = new Dictionary<int, int>();
			for (var i = 0; i < values.Length; i++)
			{
				Entries.Add(values[i], occurances[i]);
			}
		}

		#endregion

		#region Public Properties

		public IDictionary<int, int> Entries { get; }

		#endregion

		#region Public Methods

		public void Increment(int value)
		{
			if (Entries.TryGetValue(value, out var currentOccurrances))
			{
				Entries[value] = currentOccurrances + 1;
			}
			else
			{
				Entries.Add(value, 1);
			}
		}

		public void Decrement(int value)
		{
			if (Entries.TryGetValue(value, out var currentOccurrances))
			{
				Entries[value] = currentOccurrances - 1;
			}
			else
			{
				Debug.WriteLine($"WARNING: Decrementing a value that does not (yet) exist.");
				Entries.Add(value, -1);
			}
		}

		public double GetAverage()
		{
			var cnt = Entries.Count;

			if (cnt == 0)
			{
				return 0;
			}
			else
			{
				var total = Entries.Keys.Sum();
				var result = ((double)total) / cnt;
				return result;
			}
		}

		public void Clear()
		{
			Entries.Clear();
		}

		#endregion
	}
}
