using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Types
{
	/// <summary>
	/// Dictionary based Histogram
	/// Used by the ColorBandSetViewModel to hold data for 'TopValues', i.e., 
	/// Occurrances for the CountValue = TargetIterations.
	/// </summary>
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
			try
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
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e} while Incrementing HistogramD.");
			}
		}

		public void Decrement(int value)
		{
			try
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
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e} while Decrementing HistogramD.");	
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
