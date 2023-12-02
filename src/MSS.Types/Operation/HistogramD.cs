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

		public void Increment(int key)
		{
			try
			{
				if (Entries.TryGetValue(key, out var currentOccurrances))
				{
					Entries[key] = currentOccurrances + 1;
				}
				else
				{
					Entries.Add(key, 1);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e} while Incrementing HistogramD.");
			}
		}

		public void Decrement(int key)
		{
			try
			{
				if (Entries.TryGetValue(key, out var currentOccurrances))
				{
					Entries[key] = currentOccurrances - 1;
				}
				else
				{
					Debug.WriteLine($"WARNING: Decrementing the number of occurrances for a key that does not (yet) exist.");
					Entries.Add(key, -1);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e} while Decrementing HistogramD.");	
			}
		}

		public double GetAverage()
		{
			if (Entries.Count == 0)
			{
				return 0;
			}
			else
			{
				var cnt = 0;
				var total = 0.0;

				foreach(var entry in Entries)
				{
					cnt += entry.Value;
					total += entry.Value * entry.Key;
				}

				var result = total / cnt;
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
