using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public static class ColorBandSetHelper
	{
		public static ColorBandSet AdjustTargetIterations(ColorBandSet colorBandSet, int targetIterations, IEnumerable<ColorBandSet> colorBandSetCollection)
		{
			ColorBandSet? result = null;

			if (targetIterations < colorBandSet.HighCutoff)
			{
				if (TryGetCbsSmallestCutoffGtrThan(targetIterations, colorBandSetCollection, out var matched))
				{
					result = matched;
				}
				else
				{
					throw new InvalidOperationException("No Matching ColorBandSet found.");
				}
			}

			if (result != null && result.HighCutoff != targetIterations)
			{
				result = AdjustTargetIterations(result, targetIterations);
			}

			if (result != null)
			{
				return result.CreateNewCopy();
			}
			else
			{
				throw new InvalidOperationException("Result is null.");
			}
		}

		private static bool TryGetCbsSmallestCutoffGtrThan(int cutOff, IEnumerable<ColorBandSet> colorBandSets, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			colorBandSet = colorBandSets.OrderByDescending(f => f.HighCutoff).FirstOrDefault(x => x.HighCutoff <= cutOff);

			return colorBandSet != null;
		}

		public static ColorBandSet AdjustTargetIterations(ColorBandSet colorBandSet, int targetIterations)
		{
			if (colorBandSet.HighCutoff == targetIterations)
			{
				return colorBandSet;
			}

			colorBandSet = colorBandSet.CreateNewCopy(targetIterations);

			if (colorBandSet.HighCutoff > targetIterations)
			{
				var x = colorBandSet.Take(colorBandSet.Count - 1).FirstOrDefault(x => x.Cutoff > targetIterations - 2);

				while (x != null)
				{
					_ = colorBandSet.Remove(x);
					x = colorBandSet.Take(colorBandSet.Count - 1).FirstOrDefault(x => x.Cutoff > targetIterations - 2);
				}
			}

			return colorBandSet;
		}
	}
}
