using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	// TODO: How is the ColorBandSetComparer class being used?
	public class ColorBandSetComparerNameAndTi : IEqualityComparer<ColorBandSet>
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
			else if (x.Name != y.Name)
			{
				return false;
			}
			else if (x.TargetIterations != y.TargetIterations)
			{
				return false;
			}
			else
			{ 
				return true;
			}
		}

		public int GetHashCode([DisallowNull] ColorBandSet obj)
		{
			return obj.GetHashCode();
		}



	}
}
