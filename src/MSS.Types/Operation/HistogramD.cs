using System.Collections.Generic;
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
	}
}
