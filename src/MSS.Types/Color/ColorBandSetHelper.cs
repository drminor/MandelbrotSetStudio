using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public static class ColorBandSetHelper
	{
		public static ColorBandSet AdjustTargetIterations(ColorBandSet colorBandSet, int targetIterations, IEnumerable<ColorBandSet> colorBandSetCollection)
		{
			ColorBandSet result;

			if (colorBandSet.HighCutoff != targetIterations)
			{
				result = GetBestMatchingColorBandSet(targetIterations, colorBandSetCollection);
				var adjustedColorBandSet = AdjustTargetIterations(result, targetIterations);
				result = adjustedColorBandSet;
			}
			else
			{
				result = colorBandSet;
			}

			return result;
		}

		public static ColorBandSet GetBestMatchingColorBandSet(int cutoff, IEnumerable<ColorBandSet> colorBandSets)
		{
			// Try to find the ColorBandSet with a HighCutoff just less than the target cutoff.
			if (TryGetCbsLargestCutoffLessThan(cutoff, colorBandSets, out var colorBandSet))
			{
				return colorBandSet;
			}
			else
			{
				// Try to find the ColorBandSet with a HighCutoff just greater than the target cutoff.
				if (TryGetCbsSmallestCutoffGtrThan(cutoff, colorBandSets, out colorBandSet))
				{
					return colorBandSet;
				}
				else
				{
					Debug.WriteLine("This should never happen unless the colorBandSet collection is empty.");
					return colorBandSets.First();
				}
			}
		}

		private static bool TryGetCbsSmallestCutoffGtrThan(int cutoff, IEnumerable<ColorBandSet> colorBandSets, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			colorBandSet = colorBandSets.OrderByDescending(f => f.HighCutoff).FirstOrDefault(x => x.HighCutoff <= cutoff);

			return colorBandSet != null;
		}

		private static bool TryGetCbsLargestCutoffLessThan(int cutoff, IEnumerable<ColorBandSet> colorBandSets, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			colorBandSet = colorBandSets.OrderByDescending(x => x.HighCutoff).FirstOrDefault(x => x.HighCutoff <= cutoff);

			return colorBandSet != null;
		}

		public static ColorBandSet AdjustTargetIterations(ColorBandSet colorBandSet, int targetIterations)
		{
			if (colorBandSet.HighCutoff == targetIterations)
			{
				return colorBandSet;
			}
			else if (colorBandSet.HighCutoff > targetIterations)
			{
				var newColorBandSet = colorBandSet.CreateNewCopy(targetIterations);
				newColorBandSet.MoveItemsToReserveWithCutoffGtrThan(targetIterations - 2);
				return newColorBandSet;
			}
			else
			{
				return colorBandSet.CreateNewCopy(targetIterations);
			}
		}
	}
}
