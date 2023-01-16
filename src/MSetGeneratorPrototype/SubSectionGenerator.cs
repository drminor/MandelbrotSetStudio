using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class SubSectionGenerator
	{
		#region Public Methods

		public static void GenerateMapSection(SamplePointValues samplePointValues, IIterator iteratorSimd, BigVector blockPos, int rowNumber, int targetIterations) 
		{
			var inPlayList = samplePointValues.InPlayList;
			var doneFlags = samplePointValues.DoneFlags;
			var unusedCalcs = samplePointValues.UnusedCalcs;

			iteratorSimd.SetCoords(samplePointValues.Crs, samplePointValues.Cis, samplePointValues.Zrs, samplePointValues.Zis);

			while (inPlayList.Length > 0)
			{
				var escapedFlags = iteratorSimd.Iterate(inPlayList, out var sumOfSquares);

				inPlayList = UpdateCounts(escapedFlags, inPlayList, iteratorSimd.ApFixedPointFormat, blockPos, rowNumber, sumOfSquares, 
					samplePointValues, doneFlags, unusedCalcs, targetIterations);
			}

			iteratorSimd.MathOpCounts.NumberOfUnusedCalcs = unusedCalcs.Sum();
		}

		private static int[] UpdateCounts(Vector256<int>[] escapedFlagVectors, int[] inPlayList, ApFixedPointFormat apFixedPointFormat, BigVector blockPos, int rowNumber, FP31Deck sumOfSquares, 
			SamplePointValues samplePointValues, bool[] doneFlags, long[] unusedCalcs, int targetIterations)
		{
			var numberOfLanes = Vector256<uint>.Count;
			var toBeRemoved = new List<int>();

			var indexes = inPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var escapedFlagVector = escapedFlagVectors[idx];

				var allCompleted = true;

				var stPtr = idx * numberOfLanes;

				for (var cntrPtr = stPtr; cntrPtr < stPtr + numberOfLanes; cntrPtr++)
				{
					if (doneFlags[cntrPtr])
					{
						unusedCalcs[cntrPtr]++;
						continue;
					}

					var cnt = ++samplePointValues.Counts[cntrPtr];

					//var escaped = escapedFlags[cntrPtr] == -1;
					var escaped = escapedFlagVector.GetElement(cntrPtr - stPtr) == -1;

					// TODO: Need to save the ZValues to a safe place to prevent further updates.
					if (escaped)
					{
						samplePointValues.HasEscapedFlags[cntrPtr] = true;
						doneFlags[cntrPtr] = true;

						//var sacResult = escaped;
						//var mantissa = sumOfSquares.GetMantissa(idx);
						//var rValue = FP31ValHelper.CreateRValue(true, mantissa, apFixedPointFormat.TargetExponent, apFixedPointFormat.NumberOfFractionalBits);
						//var rValDiag = RValueHelper.ConvertToString(rValue);
						//Debug.WriteLine($"Bailed out after {cnt}: The value is {rValDiag}. Compare returned: {sacResult}. BlockPos: {blockPos}, Row: {rowNumber}, Col: {cntrPtr}.");
					}
					else if (cnt >= targetIterations)
					{
						doneFlags[cntrPtr] = true;
						samplePointValues.EscapeVelocities[cntrPtr] = 5; // TODO: calculate the EscapeVelocity

						//var sacResult = escaped;
						//var mantissa = sumOfSquares.GetMantissa(idx);
						//var rValue = FP31ValHelper.CreateRValue(true, mantissa, apFixedPointFormat.TargetExponent, apFixedPointFormat.NumberOfFractionalBits);
						//var rValDiag = RValueHelper.ConvertToString(rValue);
						//Debug.WriteLine($"Target reached: The value is {rValDiag}. Compare returned: {sacResult}. BlockPos: {blockPos}, Row: {rowNumber}, Col: {cntrPtr}.");
					}
					else
					{
						allCompleted = false;
					}
				}

				if (allCompleted)
				{
					toBeRemoved.Add(idx);

					if (doneFlags.Skip(stPtr).Take(numberOfLanes).Any(x => !x))
					{
						Debug.WriteLine("Huh?");
					}
				}
			}

			var newInPlayList = GetUpdatedInPlayList(inPlayList, toBeRemoved);

			return newInPlayList;
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

