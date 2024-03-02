using MongoDB.Bson;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace MSS.Types
{
	public static class ColorBandSetHelper
	{
		#region Adjust Target Iterations

		public static ColorBandSet AdjustTargetIterations(ColorBandSet colorBandSet, int targetIterations, IEnumerable<ColorBandSet> colorBandSetCollection)
		{
			ColorBandSet result;

			if (colorBandSet.HighCutoff == targetIterations)
			{
				result = colorBandSet;
			}
			else
			{
				result = GetBestMatchingColorBandSet(targetIterations, colorBandSetCollection);
				var adjustedColorBandSet = AdjustTargetIterations(result, targetIterations);
				result = adjustedColorBandSet;
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
			// TODO: When creating a new ColorBandSet because we had to Adjust the TargetIterations, how do we handle updating the name and/or Serial#.

			ColorBandSet result;

			if (colorBandSet.HighCutoff == targetIterations)
			{
				result = colorBandSet;
			}
			else if (colorBandSet.HighCutoff > targetIterations)
			{
				result = TrimColorBandsWithCutoffGreaterThan(colorBandSet, targetIterations);
			}
			else
			{
				result = colorBandSet.CreateNewCopy(targetIterations);
			}

			return result;
		}

		public static ColorBandSet TrimColorBandsWithCutoffGreaterThan(ColorBandSet colorBandSet, int targetIterations)
		{
			var cBands = colorBandSet as IList<ColorBand>;
			var itemsToKeep = cBands.Where(x => x.Cutoff <= targetIterations).ToList();

			var reservedColorBands = cBands.Where(x => x.Cutoff > targetIterations).Reverse().Select(y => new ReservedColorBand(y.StartColor, y.BlendStyle, y.EndColor));

			var result = new ColorBandSet(ObjectId.GenerateNewId(), colorBandSet.ParentId, colorBandSet.ProjectId, colorBandSet.Name, colorBandSet.Description, itemsToKeep, targetIterations, reservedColorBands, colorBandSet.ColorBandsSerialNumber);


			//foreach (var reservedColorBand in itemsToTrim)
			//{
			//	result.PushReservedColorBand(reservedColorBand);
			//}

			return result;
		}

		#endregion

		#region Percentages and Cutoffs

		public static bool TryGetPercentagesFromCutoffs(HistCutoffsSnapShot histCutoffsSnapShot, [NotNullWhen(true)] out PercentageBand[]? percentageBands)
		{
			if (histCutoffsSnapShot.CutoffsLength > 0)
			{
				percentageBands = BuildNewPercentages(histCutoffsSnapShot);
				return true;
			}
			else
			{
				percentageBands = null;
				return false;
			}
		}

		public static PercentageBand[] BuildNewPercentages(HistCutoffsSnapShot histCutoffsSnapShot)
		{
			var kvps = histCutoffsSnapShot.HistKeyValuePairs;
			var upperCatchAllValue = histCutoffsSnapShot.UpperCatchAllValue;
			var cutoffs = histCutoffsSnapShot.GetCutoffs();

			if (cutoffs.Length == 0)
			{
				return new PercentageBand[0];
			}

			// The result starts off with the existing PercentageBands.	
			var result = new PercentageBand[histCutoffsSnapShot.PercentageBands.Length];
			Array.Copy(histCutoffsSnapShot.PercentageBands, result, result.Length);

			var curBucketPtr = 0;
			var targetCutoff = cutoffs[curBucketPtr];
			long runningSum = 0;

			var i = 0;
			for (; i < kvps.Length && curBucketPtr < result.Length - 1; i++)
			{
				var idx = kvps[i].Key;
				var amount = kvps[i].Value;

				while (curBucketPtr < result.Length - 1 && idx > targetCutoff)
				{
					curBucketPtr++;
					targetCutoff = result[curBucketPtr].Cutoff;
				}

				runningSum += amount;

				if (idx == targetCutoff)
				{
					result[curBucketPtr].ExactCount = amount;
				}

				result[curBucketPtr].Count += amount;
				result[curBucketPtr].RunningSum = runningSum;
			}

			//Debug.Assert(curBucketPtr == result.Length - 2, $"CbsHistogramViewModel. BuildNewPercentages. Not all PercentageBands were updated. The TargetCutoff < the last Histogram Key.");

			if (curBucketPtr != result.Length - 2)
			{
				Debug.WriteLine($"WARNING: CbsHistogramViewModel. BuildNewPercentages. Not all PercentageBands were updated. The TargetCutoff < the last Histogram Key.");
			}

			for (; i < kvps.Length; i++)
			{
				var amount = kvps[i].Value;
				runningSum += amount;

				result[^2].Count += amount;
				result[^2].RunningSum = runningSum;
			}

			// The last percentage band receives the Upper CatchAll Value
			var finalAmount = upperCatchAllValue;
			//runningSum += finalAmount;		// Don't Include the number of samples that reached the target iteration.

			result[^1].Count = finalAmount;
			result[^1].RunningSum = runningSum + finalAmount;

			//// For now, include all of the cnts above the target in the last bucket.
			//result[^2].Count += result[^1].Count;

			var total = (double)runningSum;

			foreach (var pb in result)
			{
				pb.Percentage = Math.Round(100 * (pb.Count / total), digits: 2);
			}

			var sumOfAllPercentages = result.Take(result.Length - 1).Sum(x => x.Percentage);
			Debug.WriteLine($"The Sum of all percentages is {sumOfAllPercentages} on call to BuildNewPercentages.");

			return result;
		}

		public static bool TryGetCutoffsFromPercentages(HistCutoffsSnapShot histCutoffsSnapShot, [NotNullWhen(true)] out CutoffBand[]? cutoffBands)
		{
			if (histCutoffsSnapShot.CutoffsLength > 0 && histCutoffsSnapShot.HavePercentages)
			{
				cutoffBands = BuildNewCutoffs(histCutoffsSnapShot);
				return true;
			}
			else
			{
				cutoffBands = null;
				return false;
			}
		}

		public static CutoffBand[] BuildNewCutoffs(HistCutoffsSnapShot histCutoffsSnapShot)
		{
			if (histCutoffsSnapShot.PercentageBands.Length == 0)
			{
				return new CutoffBand[0];
			}

			var kvps = histCutoffsSnapShot.HistKeyValuePairs;
			var topIndex = histCutoffsSnapShot.HistogramLength;
			var upperCatchAllValue = histCutoffsSnapShot.UpperCatchAllValue;

			// Make a copy
			var result = histCutoffsSnapShot.PercentageBands.Select(x => new CutoffBand(x.Cutoff, x.Percentage)).ToArray();

			var sumOfAllCounts = SetTargetCounts(histCutoffsSnapShot, result);

			// Set the high cutoff
			result[^2].Cutoff = topIndex;

			var i = 0;
			var idx = 0;
			var prevCutoff = -1;

			var previousRunningCount = 0d;
			var runningCount = 0d;
			var curBucketPtr = 0;

			// Move past those bands that have a TargetCount = 0
			while (curBucketPtr < result.Length - 2 && result[curBucketPtr].TargetCount == 0)
			{
				var cutoffBand = result[curBucketPtr];
				cutoffBand.ActualCount = 0;
				cutoffBand.ActualPercentage = 0;
				cutoffBand.PreviousCount = 0;
				cutoffBand.NextCount = 0;

				curBucketPtr++;
			}

			while (curBucketPtr < result.Length - 2 && i < kvps.Length)
			{
				var cutoffBand = result[curBucketPtr];
				var targetCount = cutoffBand.TargetCount;

				while (runningCount < targetCount && i < kvps.Length)
				{
					// Update the running count and advance to the next histogram entry.
					idx = kvps[i].Key;
					var amount = kvps[i].Value;

					previousRunningCount = runningCount;
					runningCount += amount;
					i++;
				}

				if (runningCount >= targetCount)
				{
					if (idx > prevCutoff + 1)
					{
						cutoffBand.Cutoff = idx;

						cutoffBand.ActualCount = runningCount;
						cutoffBand.ActualPercentage = runningCount / sumOfAllCounts;
						cutoffBand.PreviousCount = previousRunningCount;

						if (i < kvps.Length)
						{
							cutoffBand.NextCount = runningCount + kvps[i].Value;
						}

						prevCutoff = idx;
						curBucketPtr++;
					}
					else
					{
						if (i < kvps.Length)
						{
							// Update the running count and advance to the next histogram entry.
							idx = kvps[i].Key;
							var amount = kvps[i].Value;

							previousRunningCount = runningCount;
							runningCount += amount;
							i++;
						}
					}
				}
			}

			if (curBucketPtr < result.Length - 2)
			{
				var diff = (result.Length - 2) - curBucketPtr;
				Debug.WriteLine($"WARNING: CbsHistogramViewModel. BuildNewCutoffs. The last {diff} cutoffs were not updated. The running count of histogram values did not reach the target count.");
			}

			var followingCutoff = topIndex;

			var j = result.Length - 3;

			for (; j >= 0 && followingCutoff > 1; j--)
			{
				var cutoffBand = result[j];
				if (cutoffBand.Cutoff >= followingCutoff)
				{
					Debug.WriteLine($"WARNING: CbsHistogramViewModel. BuildNewCutoffs. Forced the Offset for ColorBand at index: {j} from: {cutoffBand.Cutoff} to {followingCutoff - 1} to keep the BucketWidth >= 1.");
					cutoffBand.Cutoff = followingCutoff - 1;
				}

				followingCutoff = cutoffBand.Cutoff;
			}

			//if (j > 0)
			//{
			//	Debug.WriteLine($"WARNING: CbsHistogramViewModel. BuildNewCutoffs. Removed the first {j} Colorbands. There were more Colorbands than Histogram Values.");
			//	result = result.Skip(j).ToArray();
			//}

			return result;
		}

		public static double SetTargetCounts(HistCutoffsSnapShot histCutoffsSnapShot, CutoffBand[] cutoffBands)
		{
			var kvps = histCutoffsSnapShot.HistKeyValuePairs;

			// Get total counts
			double sumOfAllCounts = kvps.Sum(x => x.Value);

			// Set the Target Counts
			var runningPercentage = 0d;

			for (var cbPtr = 0; cbPtr < cutoffBands.Length; cbPtr++)
			{
				var cutoffBand = cutoffBands[cbPtr];
				runningPercentage += cutoffBand.Percentage;
				cutoffBand.RunningPercentage = runningPercentage;
				cutoffBand.TargetCount = (runningPercentage / 100) * sumOfAllCounts;
			}

			if (cutoffBands.Any(x => double.IsNaN(x.TargetCount)))
			{
				throw new InvalidOperationException("WARNING: CbsHistogramViewModel. BuildNewCutoffs. One or more of the TargetCounts is NaN.");
			}

			return sumOfAllCounts;
		}

		public static PercentageBand[] GetPercentageBands(ColorBandSet colorBandSet)
		{
			var pbList = colorBandSet.Select(x => new PercentageBand(x.Cutoff, x.Percentage)).ToList();
			pbList.Add(new PercentageBand(int.MaxValue));
			var result = pbList.ToArray();

			return result;
		}

		public static PercentageBand[] GetPercentageBands(int[] cutoffs)
		{
			var pbList = cutoffs.Select(x => new PercentageBand(x)).ToList();
			pbList.Add(new PercentageBand(int.MaxValue));
			var result = pbList.ToArray();

			return result;
		}

		#endregion

		#region Diagnostics

		public static void CheckNewCutoffs(PercentageBand[] percentageBands, CutoffBand[] cutoffBands)
		{
			if (percentageBands.Length != cutoffBands.Length)
			{
				throw new ArgumentException("The length of the PercentageBands is not the same as the length of the CutoffBands.");
			}

			if (percentageBands.Length == 0) return;

			var hiCutoff = percentageBands[^1].Cutoff;

			if (cutoffBands[^1].Cutoff != hiCutoff)
			{
				Debug.WriteLine($"WARNING: CbsHistogramViewModel. CheckNewCutoffs. The new Cutoffs have a different value for the High ColorBand's Cutoff. New: {cutoffBands[^1].Cutoff}, Old: {hiCutoff}");
			}

			var lastCutoff = cutoffBands[0].Cutoff;

			for (var i = 1; i < cutoffBands.Length; i++)
			{
				var thisCutoff = cutoffBands[i].Cutoff;
				if (thisCutoff < lastCutoff)
				{
					Debug.WriteLine($"WARNING: CbsHistogramViewModel. CheckNewCutoffs. The BucketWidth is zero at index {i}.");
				}

				lastCutoff = thisCutoff;
			}
		}

		public static void ReportNewCutoffs(HistCutoffsSnapShot histCutoffsSnapShot, PercentageBand[] percentageBands, CutoffBand[] cutoffBands)
		{
			if (percentageBands.Length != cutoffBands.Length)
			{
				throw new ArgumentException("The length of the PercentageBands is not the same as the length of the CutoffBands.");
			}

			var sb = new StringBuilder();

			var kvps = histCutoffsSnapShot.HistKeyValuePairs;

			if (kvps.Length > 2)
			{
				var last3 = kvps.Skip(kvps.Length - 3).ToArray();
				sb.AppendLine($"The top 3 values are {last3[0].Key}/{last3[0].Value}, {last3[1].Key}/{last3[1].Value}, {last3[2].Key}/{last3[2].Value}. " +
					$"The UpperCatchAllValue is {histCutoffsSnapShot.UpperCatchAllValue}.");
			}

			sb.AppendLine("Original	New		Percentage		RunningPer		Target		Actual		PrevCount		NextCount");

			for (var i = 0; i < cutoffBands.Length; i++)
			{
				var originalCutoff = percentageBands[i].Cutoff;
				var cb = cutoffBands[i];

				sb.AppendLine($"{originalCutoff}\t\t\t{cb.Cutoff}\t\t{cb.Percentage,8:F2}\t\t{cb.RunningPercentage,8:F2}\t\t{cb.TargetCount,8:F2}\t\t{cb.ActualCount}\t\t\t{cb.PreviousCount}\t\t\t\t{cb.NextCount}");
			}

			Debug.Write(sb.ToString());
		}

		public static void ReportNewPercentages(PercentageBand[] percentageBands)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Cutoff	Percentage		Count		ExactCount		RunningSum");

			for (var i = 0; i < percentageBands.Length; i++)
			{
				var pb = percentageBands[i];

				sb.AppendLine($"{pb.Cutoff}\t\t{pb.Percentage,8:F2}\t\t{pb.Count,8:F2}\t\t{pb.ExactCount,8:F2}\t\t{pb.RunningSum}");
			}

			Debug.Write(sb.ToString());
		}

		#endregion
	}
}
