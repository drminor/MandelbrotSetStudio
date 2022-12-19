﻿using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGenP
{
	internal class SubSectionGeneratorVector
	{
		private readonly ApFixedPointFormat _apFixedPointFormat1;

		private readonly int _targetIterations;
		private readonly uint _threshold;

		//private readonly SmxMathHelper _smxMathHelper;
		//private IteratorScalar _iteratorScaler;

		#region Constructor

		public SubSectionGeneratorVector(ApFixedPointFormat apFixedPointFormat, int targetIterations, uint threshold)
		{
			_apFixedPointFormat1 = apFixedPointFormat;

			_targetIterations = targetIterations;
			_threshold = threshold;

			//_smxMathHelper = new SmxMathHelper(apFixedPointFormat, threshold);
			//_iteratorScaler = new IteratorScalar(_smxMathHelper, _targetIterations);
		}

		#endregion

		#region Public Properties

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public long NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		#endregion

		#region Public Methods

		public void GenerateMapSection(FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, ushort[] counts, bool[] doneFlags)
		{
			var resultLength = cRs.Length;

			var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);
			var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);

			var escapedFlags = new bool[resultLength];

			var smxVecMathHelper = new VecMath(_apFixedPointFormat1, resultLength, _threshold);

			var inPlayList = BuildTheInplayList(doneFlags, resultLength);
			smxVecMathHelper.InPlayList = inPlayList;

			var iterator = new IteratorVector(smxVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			while (inPlayList.Length > 0)
			{
				iterator.Iterate();
				smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
				UpdateTheDoneFlags(smxVecMathHelper, sumOfSqrs, escapedFlags, counts, doneFlags, inPlayList);
			}

			NumberOfACarries += smxVecMathHelper.NumberOfACarries;
			NumberOfMCarries += smxVecMathHelper.NumberOfMCarries;
		}

		public ushort[] GenerateMapSection(FPValues cRs, FPValues cIs, out bool[] doneFlags)
		{
			var resultLength = cRs.Length;

			//var counts = Enumerable.Repeat((ushort)1, resultLength).ToArray();

			var counts = new ushort[resultLength];
			doneFlags = new bool[resultLength];

			var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);
			var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);

			var escapedFlags = new bool[resultLength];

			var smxVecMathHelper = new VecMath(_apFixedPointFormat1, resultLength, _threshold);
			smxVecMathHelper.DoneFlags = doneFlags;

			var inPlayList = smxVecMathHelper.InPlayList;

			// Perform the first iteration. 
			var zRs = cRs.Clone();
			var zIs = cIs.Clone();

			smxVecMathHelper.Square(zRs, zRSqrs);
			smxVecMathHelper.Square(zIs, zISqrs);
			smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
			inPlayList = UpdateTheDoneFlags(smxVecMathHelper, sumOfSqrs, escapedFlags, counts, doneFlags, inPlayList);
			smxVecMathHelper.InPlayList = inPlayList;

			var iterator = new IteratorVector(smxVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			while (inPlayList.Length > 0)
			{
				var aCarriesSnap = smxVecMathHelper.NumberOfACarries;
				var doneFlagsCnt = doneFlags.Count(x => x);

				iterator.Iterate();
				smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);

				//var aCarriesDif = smxVecMathHelper.NumberOfACarries - aCarriesSnap;
				//if (aCarriesDif > 0)
				//{
				//	var newDoneFlagsCnt = doneFlags.Count(x => x);
				//	var doneFlagsDiff = newDoneFlagsCnt - doneFlagsCnt;
				//	Debug.Assert(doneFlagsDiff == aCarriesDif, "Not All Done Flags were updated.");
				//}

				inPlayList = UpdateTheDoneFlags(smxVecMathHelper, sumOfSqrs, escapedFlags, counts, doneFlags, inPlayList);
				smxVecMathHelper.InPlayList = inPlayList;



			}

			NumberOfACarries += smxVecMathHelper.NumberOfACarries;
			NumberOfMCarries += smxVecMathHelper.NumberOfMCarries;

			// TODO: Need to keep track if a sample point has escaped or not, currently the DoneFlag is set if 'Escaped' or 'Reached Target Iteration.'

			doneFlags = smxVecMathHelper.DoneFlags;
			return counts;
		}

		private int[] UpdateTheDoneFlags(VecMath smxVecMathHelper, FPValues sumOfSqrs, bool[] escapedFlags, ushort[] counts, bool[] doneFlags, int[] inPlayList)
		{
			smxVecMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs, escapedFlags);

			var vectorsNoLongerInPlay = UpdateCounts(inPlayList, escapedFlags, counts, doneFlags);
			var updatedInPlayList = GetUpdatedInPlayList(inPlayList, vectorsNoLongerInPlay);

			return updatedInPlayList;
		}

		private List<int> UpdateCounts(int[] inPlayList, bool[] escapedFlags, ushort[] counts, bool[] doneFlags)
		{
			var lanes = Vector256<ulong>.Count;
			var toBeRemoved = new List<int>();

			var indexes = inPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var allCompleted = true;

				var cntrPtr = idx * lanes;
				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					var doneFlag = doneFlags[cntrPtr + lanePtr];

					if (doneFlag)
					{
						continue;
					}

					var cnt = counts[cntrPtr + lanePtr] + 1;
					counts[cntrPtr + lanePtr] = (ushort)cnt;

					var escaped = escapedFlags[cntrPtr + lanePtr];

					// TODO: Need to save the ZValues to a safe place to prevent further updates.
					if (escaped)
					{
						doneFlags[cntrPtr + lanePtr] = true;
					}
					else if (cnt >= _targetIterations)
					{
						doneFlags[cntrPtr + lanePtr] = true;
					}
					else
					{
						allCompleted = false;
					}
				}

				if (allCompleted)
				{
					toBeRemoved.Add(idx);

					var fg = new bool[lanes];
					for (var i = 0; i < lanes; i++)
					{
						fg[i] = doneFlags[cntrPtr + i];
					}

					if (fg.Any(x => !x))
					{
						Debug.WriteLine("Huh?");
					}
				}
			}

			return toBeRemoved;
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

		#endregion
	}
}
