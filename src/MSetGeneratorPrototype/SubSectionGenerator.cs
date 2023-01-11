using MSS.Common.APValues;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class SubSectionGenerator
	{
		#region Public Methods

		public static ushort[] GenerateMapSection(VecMath9 vecMath, IteratorSimd iteratorSimd, int targetIterations, FP31Deck cRs, FP31Deck cIs, out bool[] doneFlags)
		{
			var resultLength = cRs.Length;

			var counts = new ushort[resultLength];

			doneFlags = vecMath.DoneFlags;
			var inPlayList = vecMath.InPlayList;
			var unusedCalcs = vecMath.UnusedCalcs;

			iteratorSimd.SetCoords(cRs, cIs);

			while (inPlayList.Length > 0)
			{
				var escapedFlags = iteratorSimd.Iterate(vecMath);

				var vectorsNoLongerInPlay = UpdateCounts(inPlayList, escapedFlags, counts, doneFlags, unusedCalcs, targetIterations);
				inPlayList = GetUpdatedInPlayList(inPlayList, vectorsNoLongerInPlay);
				vecMath.InPlayList = inPlayList;
			}

			// TODO: Need to keep track if a sample point has escaped or not, currently the DoneFlag is set if 'Escaped' or 'Reached Target Iteration.'

			return counts;
		}

		private static List<int> UpdateCounts(int[] inPlayList, int[] escapedFlags, ushort[] counts, bool[] doneFlags, long[] unusedCalcs, int targetIterations)
		{
			var numberOfLanes = Vector256<uint>.Count;
			var toBeRemoved = new List<int>();

			var indexes = inPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var allCompleted = true;

				var stPtr = idx * numberOfLanes;

				for (var cntrPtr = stPtr; cntrPtr < stPtr + numberOfLanes; cntrPtr++)
				{
					var doneFlag = doneFlags[cntrPtr];

					if (doneFlag)
					{
						unusedCalcs[idx]++;
						continue;
					}

					var cnt = counts[cntrPtr] + 1;
					counts[cntrPtr] = (ushort)cnt;

					var escaped = escapedFlags[cntrPtr] == -1;

					// TODO: Need to save the ZValues to a safe place to prevent further updates.
					if (escaped)
					{
						doneFlags[cntrPtr] = true;
						//var sacResult = escaped;
						//var rValDiag = vecMath.GetSmxAtIndex(sumOfSqrs, stPtr).GetStringValue();
						//Debug.WriteLine($"Bailed out after {cnt}: The value is {rValDiag}. Compare returned: {sacResult}. BlockPos: {vecMath.BlockPosition}, Row: {vecMath.RowNumber}, Col: {cntrPtr}.");
					}
					else if (cnt >= targetIterations)
					{
						doneFlags[cntrPtr] = true;
						//var sacResult = escaped;
						//var rValDiag = vecMath.GetSmxAtIndex(sumOfSqrs, stPtr).GetStringValue();
						//Debug.WriteLine($"Target reached: The value is {rValDiag}. Compare returned: {sacResult}. BlockPos: {vecMath.BlockPosition}, Row: {vecMath.RowNumber}, Col: {cntrPtr}.");
					}
					else
					{
						allCompleted = false;
					}
				}

				if (allCompleted)
				{
					toBeRemoved.Add(idx);

					ConfirmDoneFlags(stPtr, numberOfLanes, doneFlags);	
				}
			}

			return toBeRemoved;
		}

		private static void ConfirmDoneFlags(int stPtr, int numberOfLanes, bool[] doneFlags)
		{
			if (doneFlags.Skip(stPtr).Take(numberOfLanes).Any(x => !x))
			{
				Debug.WriteLine("Huh?");
			}
		}

		private static int[] GetUpdatedInPlayList(int[] inPlayList, List<int> vectorsNoLongerInPlay)
		{
			var lst = inPlayList.ToList();

			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				lst.Remove(vectorIndex);
			}

			var updatedLst = lst.ToArray();

			return updatedLst;
		}

		//private int[] BuildTheInplayList(bool[] doneFlags, int vecCount)
		//{
		//	var lanes = Vector256<uint>.Count;

		//	Debug.Assert(doneFlags.Length * lanes == vecCount, $"The doneFlags length: {doneFlags.Length} does not match {lanes} times the vector count: {vecCount}.");

		//	var result = Enumerable.Range(0, vecCount).ToList();

		//	for (int j = 0; j < vecCount; j++)
		//	{
		//		var arrayPtr = j * lanes;

		//		for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
		//		{
		//			if (doneFlags[arrayPtr + lanePtr])
		//			{
		//				result.Remove(j);
		//				break;
		//			}
		//		}
		//	}

		//	return result.ToArray();
		//}

		#endregion
	}
}

