using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	// TODO: How is the ColorBandSetComparer class being used?
	public class ColorBandSetComparer : IEqualityComparer<ColorBandSet>
	{
		public bool Equals(ColorBandSet? x, ColorBandSet? y)
		{
			if (x == null)
			{
				return y == null;
			}
			else if (y == null)
			{
				return false;
			}
			else if (x.Count != y.Count)
			{
				return false;
			}
			else
			{
				for(var i = 0; i < x.Count; i++)
				{
					if (x[i] != y[i])
					{
						return false;
					}
				}

				return true;
			}
		}

		public bool EqualsExt(ColorBandSet? x, ColorBandSet? y, out IList<int> mismatchedLines)
		{
			var result = true;
			mismatchedLines = new List<int>();

			if (x == null)
			{
				return y == null;
			}
			else if (y == null)
			{
				return false;
			}
			else if (x.Count != y.Count)
			{
				return false;
			}
			else
			{
				for (var i = 0; i < x.Count; i++)
				{
					if (x[i] != y[i])
					{
						mismatchedLines.Add(i);
						result = false;
					}
				}

				return result;
			}
		}

		public int GetHashCode([DisallowNull] ColorBandSet obj)
		{
			return obj.GetHashCode();
		}

		public static void CheckThatColorBandsWereUpdatedProperly(ColorBandSet colorBandSet, ColorBandSet goodCopy, bool throwOnMismatch)
		{
			var theyMatch = new ColorBandSetComparer().EqualsExt(colorBandSet, goodCopy, out var mismatchedLines);

			if (theyMatch)
			{
				Debug.WriteLine("The new ColorBandSet is sound.");
			}
			else
			{
				Debug.WriteLine("Creating a new copy of the ColorBands produces a result different that the current collection of ColorBands.");
				Debug.WriteLine($"Updated: {colorBandSet}, new: {goodCopy}");
				Debug.WriteLine($"The mismatched lines are: {string.Join(", ", mismatchedLines.Select(x => x.ToString()).ToArray())}");

				if (throwOnMismatch)
				{
					throw new InvalidOperationException("ColorBandSet update mismatch.");
				}
			}
		}

	}
}
