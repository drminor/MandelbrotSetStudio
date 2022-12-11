using System;
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

		#endregion

		#region Public Methods

		public void GenerateMapSection(FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, ushort[] counts, bool[] doneFlags)
		{
			var resultLength = cRs.Length;

			var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);
			var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);

			var escapedFlagsMem = new Memory<long>(new long[resultLength]);
			var escapedFlagVectors = MemoryMarshal.Cast<long, Vector256<long>>(escapedFlagsMem.Span);

			var smxVecMathHelper = new SmxVecMathHelper(_apFixedPointFormat1, resultLength, _threshold);

			var inPlayList = BuildTheInplayList(doneFlags, resultLength);
			smxVecMathHelper.InPlayList = inPlayList;

			var iterator = new IteratorVector(smxVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			while (inPlayList.Length > 0)
			{
				iterator.Iterate();
				smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
				UpdateTheDoneFlags(smxVecMathHelper, sumOfSqrs, escapedFlagVectors, counts, doneFlags, inPlayList);
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

			var escapedFlagsMem = new Memory<long>(new long[resultLength]);
			var escapedFlagVectors = MemoryMarshal.Cast<long, Vector256<long>>(escapedFlagsMem.Span);

			var smxVecMathHelper = new SmxVecMathHelper(_apFixedPointFormat1, resultLength, _threshold);

			var inPlayList = smxVecMathHelper.InPlayList;

			// Perform the first iteration. 
			var zRs = cRs.Clone();
			var zIs = cIs.Clone();

			smxVecMathHelper.Square(zRs, zRSqrs);
			smxVecMathHelper.Square(zIs, zISqrs);
			smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
			inPlayList = UpdateTheDoneFlags(smxVecMathHelper, sumOfSqrs, escapedFlagVectors, counts, doneFlags, inPlayList);
			smxVecMathHelper.InPlayList = inPlayList;

			var iterator = new IteratorVector(smxVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			while (inPlayList.Length > 0)
			{
				iterator.Iterate();
				smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
				inPlayList = UpdateTheDoneFlags(smxVecMathHelper, sumOfSqrs, escapedFlagVectors, counts, doneFlags, inPlayList);
				smxVecMathHelper.InPlayList = inPlayList;
			}

			NumberOfACarries += smxVecMathHelper.NumberOfACarries;
			NumberOfMCarries += smxVecMathHelper.NumberOfMCarries;

			return counts;
		}

		private int[] UpdateTheDoneFlags(SmxVecMathHelper smxVecMathHelper, FPValues sumOfSqrs, Span<Vector256<long>> escapedFlagVectors, ushort[] counts, bool[] doneFlags, int[] inPlayList)
		{
			smxVecMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs, escapedFlagVectors);

			var vectorsNoLongerInPlay = UpdateCounts(inPlayList, escapedFlagVectors, counts, doneFlags);
			var updatedInPlayList = GetUpdatedInPlayList(inPlayList, vectorsNoLongerInPlay);

			return updatedInPlayList;
		}

		private List<int> UpdateCounts(int[] inPlayList, Span<Vector256<long>> escapedFlagVectors, ushort[] counts, bool[] doneFlags)
		{
			var lanes = Vector256<ulong>.Count;
			var toBeRemoved = new List<int>();

			foreach (var idx in inPlayList)
			{
				var escapedFlagVector = escapedFlagVectors[idx];
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

					var escaped = escapedFlagVector.GetElement(lanePtr) != 0;

					if (escaped)
					{
						Debug.Assert(escapedFlagVector.GetElement(lanePtr) == -1, "Unexpected value for the escapedFlagVector.");
						doneFlags[cntrPtr + lanePtr] = true;
					}

					if (cnt < _targetIterations && !escaped)
					{
						allCompleted = false;
					}
				}

				if (allCompleted)
				{
					toBeRemoved.Add(idx);
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

