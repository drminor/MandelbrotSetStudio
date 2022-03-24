using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
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
	}
}
