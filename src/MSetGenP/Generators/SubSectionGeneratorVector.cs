using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGenP
{
	internal class SubSectionGeneratorVector
	{
		#region Public Methods

		//public void GenerateMapSection(FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, ushort[] counts, bool[] doneFlags)
		//{
		//	var resultLength = cRs.Length;

		//	var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
		//	var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);
		//	var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);

		//	var escapedFlags = new bool[resultLength];

		//	var fPVecMathHelper = new VecMath2C(_apFixedPointFormat1, resultLength, _threshold);

		//	var inPlayList = BuildTheInplayList(doneFlags, resultLength);
		//	fPVecMathHelper.InPlayList = inPlayList;

		//	var iterator = new IteratorVector(fPVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

		//	while (inPlayList.Length > 0)
		//	{
		//		iterator.Iterate();
		//		fPVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
		//		UpdateTheDoneFlags(fPVecMathHelper, sumOfSqrs, escapedFlags, counts, doneFlags, inPlayList);
		//	}

		//	NumberOfACarries += fPVecMathHelper.NumberOfACarries;
		//	NumberOfMCarries += fPVecMathHelper.NumberOfMCarries;
		//}

		public ushort[] GenerateMapSection(IVecMath vecMath, int targetIterations, FPValues cRs, FPValues cIs, out bool[] doneFlags)
		{
			var resultLength = cRs.Length;

			//var counts = Enumerable.Repeat((ushort)1, resultLength).ToArray();

			var counts = new ushort[resultLength];
			//doneFlags = new bool[resultLength];

			var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);
			var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);

			var escapedFlags = new bool[resultLength];
			//var vecMath = new VecMath(_apFixedPointFormat1, resultLength, _threshold);

			doneFlags = vecMath.DoneFlags;
			var inPlayList = vecMath.InPlayList;

			// Perform the first iteration. 
			var zRs = cRs.Clone();
			var zIs = cIs.Clone();

			vecMath.Square(zRs, zRSqrs);
			vecMath.Square(zIs, zISqrs);
			vecMath.Add(zRSqrs, zISqrs, sumOfSqrs);

			inPlayList = UpdateTheDoneFlags(vecMath, sumOfSqrs, inPlayList, escapedFlags, counts, doneFlags, targetIterations);
			vecMath.InPlayList = inPlayList;

			var iterator = new IteratorVector(cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			while (inPlayList.Length > 0)
			{
				var aCarriesSnap = vecMath.NumberOfACarries;
				var doneFlagsCnt = doneFlags.Count(x => x);

				iterator.Iterate(vecMath);
				vecMath.Add(zRSqrs, zISqrs, sumOfSqrs);

				//var aCarriesDif = smxVecMathHelper.NumberOfACarries - aCarriesSnap;
				//if (aCarriesDif > 0)
				//{
				//	var newDoneFlagsCnt = doneFlags.Count(x => x);
				//	var doneFlagsDiff = newDoneFlagsCnt - doneFlagsCnt;
				//	Debug.Assert(doneFlagsDiff == aCarriesDif, "Not All Done Flags were updated.");
				//}

				inPlayList = UpdateTheDoneFlags(vecMath, sumOfSqrs, inPlayList, escapedFlags, counts, doneFlags, targetIterations);
				vecMath.InPlayList = inPlayList;
			}

			//NumberOfACarries += _vecMath.NumberOfACarries;
			//NumberOfMCarries += _vecMath.NumberOfMCarries;

			// TODO: Need to keep track if a sample point has escaped or not, currently the DoneFlag is set if 'Escaped' or 'Reached Target Iteration.'

			doneFlags = vecMath.DoneFlags;
			return counts;
		}

		private int[] UpdateTheDoneFlags(IVecMath vecMath, FPValues sumOfSqrs, int[] inPlayList, bool[] escapedFlags, ushort[] counts, bool[] doneFlags, int targetIterations)
		{
			vecMath.IsGreaterOrEqThanThreshold(sumOfSqrs, escapedFlags);

			var vectorsNoLongerInPlay = UpdateCounts(vecMath, sumOfSqrs, inPlayList, escapedFlags, counts, doneFlags, targetIterations);
			var updatedInPlayList = GetUpdatedInPlayList(inPlayList, vectorsNoLongerInPlay);

			return updatedInPlayList;
		}

		private List<int> UpdateCounts(IVecMath vecMath, FPValues sumOfSqrs, int[] inPlayList, bool[] escapedFlags, ushort[] counts, bool[] doneFlags, int targetIterations)
		{
			var numberOfLanes = Vector256<ulong>.Count;
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
						continue;
					}

					var cnt = counts[cntrPtr] + 1;
					counts[cntrPtr] = (ushort)cnt;

					var escaped = escapedFlags[cntrPtr];

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

		private void ConfirmDoneFlags(int stPtr, int numberOfLanes, bool[] doneFlags)
		{
			if (doneFlags.Skip(stPtr).Take(numberOfLanes).Any(x => !x))
			{
				Debug.WriteLine("Huh?");
			}
		}

		private int[] GetUpdatedInPlayList(int[] inPlayList, List<int> vectorsNoLongerInPlay)
		{
			var lst = inPlayList.ToList();

			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				lst.Remove(vectorIndex);
			}

			var updatedLst = lst.ToArray();

			return updatedLst;
		}

		private int[] BuildTheInplayList(bool[] doneFlags, int vecCount)
		{
			var lanes = Vector256<ulong>.Count;

			Debug.Assert(doneFlags.Length * lanes == vecCount, $"The doneFlags length: {doneFlags.Length} does not match {lanes} times the vector count: {vecCount}.");

			var result = Enumerable.Range(0, vecCount).ToList();

			for (int j = 0; j < vecCount; j++)
			{
				var arrayPtr = j * lanes;

				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					if (doneFlags[arrayPtr + lanePtr])
					{
						result.Remove(j);
						break;
					}
				}
			}

			return result.ToArray();
		}

		//private List<int> UpdateCounts(int[] inPlayList, Span<Vector256<long>> escapedFlagVectors, ushort[] cntrs, FPValues sumOfSqrs)
		//{
		//	var lanes = Vector256<ulong>.Count;
		//	var toBeRemoved = new List<int>();

		//	foreach (var idx in inPlayList)
		//	{
		//		var anyReachedTargetIterations = false;
		//		var anyEscaped = false;

		//		var allCompleted = true;

		//		var escapedFlagVector = escapedFlagVectors[idx];

		//		var cntrsBuf = Enumerable.Repeat(-1, lanes).ToArray();

		//		var cntrPtr = idx * lanes;
		//		for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
		//		{
		//			if (escapedFlagVector.GetElement(lanePtr) == 0)
		//			{
		//				var cnt = (cntrs[cntrPtr + lanePtr] + 1);

		//				if (cnt >= ushort.MaxValue)
		//				{
		//					Debug.WriteLine($"WARNING: The Count is > ushort.Max.");
		//					cnt = ushort.MaxValue;
		//				}

		//				//cntrs[cntrPtr + lanePtr] = (ushort) cnt;
		//				cntrsBuf[lanePtr] = (ushort)cnt;

		//				if (cnt >= _targetIterations)
		//				{
		//					// Target reached
		//					anyReachedTargetIterations = true;

		//					//var sacResult = escapedFlagVector.GetElement(lanePtr);
		//					//var rValDiag = smxVecMathHelper.GetSmxAtIndex(sumOfSqrs, idx + lanePtr).GetStringValue();
		//					//Debug.WriteLine($"Target reached: The value is {rValDiag}. Compare returned: {sacResult}.");
		//				}
		//				else
		//				{
		//					// Didn't escape and didn't reach target
		//					allCompleted = false;
		//				}
		//			}
		//			else
		//			{
		//				//cntrsBuf[lanePtr] = cntrs[cntrPtr + lanePtr]; // record current counter.
		//				// Escaped
		//				anyEscaped = true;
		//				//var sacResult = escapedFlagVector.GetElement(lanePtr);
		//				//var rValDiag = smxVecMathHelper.GetSmxAtIndex(sumOfSqrs, idx + lanePtr).GetStringValue();
		//				//Debug.WriteLine($"Bailed out: The value is {rValDiag}. Compare returned: {sacResult}.");
		//			}
		//		}

		//		//if (allCompleted)
		//		//{
		//		//	toBeRemoved.Add(idx);
		//		//}

		//		if (anyReachedTargetIterations || anyEscaped)
		//		{
		//			if (!allCompleted)
		//			{
		//				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
		//				{
		//					if (cntrsBuf[lanePtr] != -1)
		//					{
		//						cntrs[cntrPtr + lanePtr] = (ushort)cntrsBuf[lanePtr];
		//					}
		//					else
		//					{
		//						//iteratorScalar.Iterate()
		//						//cntrs[cntrPtr + lanePtr] = 51;
		//					}
		//				}
		//			}
		//			else
		//			{
		//				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
		//				{
		//					if (cntrsBuf[lanePtr] != -1)
		//					{
		//						cntrs[cntrPtr + lanePtr] = (ushort)cntrsBuf[lanePtr];
		//					}
		//				}
		//			}

		//			toBeRemoved.Add(idx);
		//		}
		//		else
		//		{
		//			for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
		//			{
		//				if (cntrsBuf[lanePtr] != -1)
		//				{
		//					cntrs[cntrPtr + lanePtr] = (ushort)cntrsBuf[lanePtr];
		//				}
		//			}

		//		}
		//	}

		//	return toBeRemoved;
		//}


		#endregion
	}
}

