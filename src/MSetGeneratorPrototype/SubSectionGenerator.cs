using MSS.Types;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	internal class SubSectionGenerator
	{
		#region Public Methods

		public static void GenerateMapSection(IIterator iteratorSimd, SamplePointValues samplePointValues, uint threshold, BigVector blockPos, int rowNumber) 
		{
			var inPlayList = samplePointValues.InPlayList;
			var unusedCalcs = samplePointValues.UnusedCalcs;

			iteratorSimd.SetCoords(samplePointValues.Crs, samplePointValues.Cis, samplePointValues.Zrs, samplePointValues.Zis);
			iteratorSimd.Threshold = threshold;

			while (inPlayList.Length > 0)
			{
				var escapedFlags = iteratorSimd.Iterate(inPlayList, out var sumOfSquares);

				var vectorsNoLongerInPlay = UpdateCounts(escapedFlags, samplePointValues /*, iteratorSimd.ApFixedPointFormat, blockPos, rowNumber, sumOfSquares*/);
				inPlayList = samplePointValues.UpdateTheInPlayList(vectorsNoLongerInPlay);
			}

			long sumOfAllUnusedCalcs = 0;

			foreach(var i in unusedCalcs)
			{
				sumOfAllUnusedCalcs += i;
			}

			iteratorSimd.MathOpCounts.NumberOfUnusedCalcs = sumOfAllUnusedCalcs;

			//samplePointValues.UpdateTheCounts();
		}

		private static List<int> UpdateCounts(Vector256<int>[] escapedFlagVectors, SamplePointValues samplePointValues/*, ApFixedPointFormat apFixedPointFormat, BigVector blockPos, int rowNumber, FP31Deck sumOfSquares*/)
		{
			var toBeRemoved = new List<int>();

			var targetIterationsVector = samplePointValues.TargetIterationsVector;

			//ushort one = 1;
			var justOne = Vector256.Create(1);
			var justOneUnsigned = Vector256.Create(1u);

			//var doneFlags = samplePointValues.DoneFlags;
			//var unusedCalcs = samplePointValues.UnusedCalcs;

			var hasEscapedFlagsVectors = samplePointValues.HasEscapedFlagsV;
			var countsVectors = samplePointValues.CountsV;
			//var escapeVelocitiesVectors = samplePointValues.EscapeVelocitiesV;
			var doneFlagsVectors = samplePointValues.DoneFlagsV;
			var unusedCalcsVectors = samplePointValues.UnusedCalcsV;	
			
			var indexes = samplePointValues.InPlayList;

			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var doneFlagsV = doneFlagsVectors[idx];

				// Increment all counts
				var countsVt = Avx2.Add(countsVectors[idx], justOne);
				// Take the incremented count, only if the doneFlags is false for each vector position.
				var countsV = Avx2.BlendVariable(countsVt.AsByte(), countsVectors[idx].AsByte(), doneFlagsV.AsByte()).AsInt32(); // use First if Zero, second if 1
				countsVectors[idx] = countsV;

				// Increment all unused calculations
				var unusedCalcsVt = Avx2.Add(unusedCalcsVectors[idx], justOneUnsigned);
				// Take the incremented unusedCalc, only if the doneFlags is true for each vector position.
				var unusedCalcsV = Avx2.BlendVariable(unusedCalcsVectors[idx].AsByte(), unusedCalcsVt.AsByte(), doneFlagsV.AsByte()).AsUInt32();
				unusedCalcsVectors[idx] = unusedCalcsV;

				// Apply the new escapeFlags, only if the doneFlags is false for each vector position
				var updatedHaveEscapedFlagsV = Avx2.BlendVariable(escapedFlagVectors[idx].AsByte(), hasEscapedFlagsVectors[idx].AsByte(), doneFlagsV.AsByte()).AsInt32();
				hasEscapedFlagsVectors[idx] = updatedHaveEscapedFlagsV;

				// Compare the new Counts with the TargetIterations

				var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsVector);
				var escapedOrReachedVec = Avx2.Or(updatedHaveEscapedFlagsV, targetReachedCompVec);

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				var updatedDoneFlagsV = Avx2.BlendVariable(doneFlagsVectors[idx].AsByte(), Vector256<int>.AllBitsSet.AsByte(), escapedOrReachedVec.AsByte()).AsInt32();

				samplePointValues.DoneFlagsV[idx] = updatedDoneFlagsV;

				var compositeIsDone = Avx2.MoveMask(updatedDoneFlagsV.AsByte());

				if (compositeIsDone == -1)
				{
					toBeRemoved.Add(idx);
				}
			}

			return toBeRemoved;
		}

		/*

Instruction: vpblendvb ymm, ymm, ymm, ymm
CPUID Flags: AVX2		  
				FOR j := 0 to 31
					i := j*8
					IF mask[i+7]
						dst[i+7:i] := b[i+7:i]
					ELSE
						dst[i+7:i] := a[i+7:i]
					FI
				ENDFOR
				dst[MAX:256] := 0	

Instruction: vpcmpgtd ymm, ymm, ymm
CPUID Flags: AVX2			
		
				FOR j := 0 to 7
					i := j*32
					dst[i+31:i] := ( a[i+31:i] > b[i+31:i] ) ? 0xFFFFFFFF : 0
				ENDFOR
				dst[MAX:256] := 0



		*/

		//private static List<int> UpdateCountsOld(Vector256<int>[] escapedFlagVectors, SamplePointValues samplePointValues/*, ApFixedPointFormat apFixedPointFormat, BigVector blockPos, int rowNumber, FP31Deck sumOfSquares*/) 
		//{
		//	var numberOfLanes = Vector256<uint>.Count;
		//	var toBeRemoved = new List<int>();

		//	var targetIterations = samplePointValues.TargetIterations;
		//	var doneFlags = samplePointValues.DoneFlags;
		//	var unusedCalcs = samplePointValues.UnusedCalcs;
		//	var indexes = samplePointValues.InPlayList;

		//	for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
		//	{
		//		var idx = indexes[idxPtr];

		//		var escapedFlagVector = escapedFlagVectors[idx];

		//		var allCompleted = true;

		//		var stPtr = idx * numberOfLanes;

		//		for (var cntrPtr = stPtr; cntrPtr < stPtr + numberOfLanes; cntrPtr++)
		//		{
		//			if (doneFlags[cntrPtr])
		//			{
		//				unusedCalcs[cntrPtr]++;
		//				continue;
		//			}

		//			var cnt = ++samplePointValues.Counts[cntrPtr];

		//			//var escaped = escapedFlags[cntrPtr] == -1;
		//			var escaped = escapedFlagVector.GetElement(cntrPtr - stPtr) == -1;

		//			// TODO: Need to save the ZValues to a safe place to prevent further updates.
		//			if (escaped)
		//			{
		//				samplePointValues.HasEscapedFlags[cntrPtr] = true;
		//				doneFlags[cntrPtr] = true;

		//				//var sacResult = escaped;
		//				//var mantissa = sumOfSquares.GetMantissa(idx);
		//				//var rValue = FP31ValHelper.CreateRValue(true, mantissa, apFixedPointFormat.TargetExponent, apFixedPointFormat.NumberOfFractionalBits);
		//				//var rValDiag = RValueHelper.ConvertToString(rValue);
		//				//Debug.WriteLine($"Bailed out after {cnt}: The value is {rValDiag}. Compare returned: {sacResult}. BlockPos: {blockPos}, Row: {rowNumber}, Col: {cntrPtr}.");
		//			}
		//			else if (cnt >= targetIterations)
		//			{
		//				doneFlags[cntrPtr] = true;
		//				samplePointValues.EscapeVelocities[cntrPtr] = 5; // TODO: calculate the EscapeVelocity

		//				//var sacResult = escaped;
		//				//var mantissa = sumOfSquares.GetMantissa(idx);
		//				//var rValue = FP31ValHelper.CreateRValue(true, mantissa, apFixedPointFormat.TargetExponent, apFixedPointFormat.NumberOfFractionalBits);
		//				//var rValDiag = RValueHelper.ConvertToString(rValue);
		//				//Debug.WriteLine($"Target reached: The value is {rValDiag}. Compare returned: {sacResult}. BlockPos: {blockPos}, Row: {rowNumber}, Col: {cntrPtr}.");
		//			}
		//			else
		//			{
		//				allCompleted = false;
		//			}
		//		}

		//		if (allCompleted)
		//		{
		//			toBeRemoved.Add(idx);

		//			if (doneFlags.Skip(stPtr).Take(numberOfLanes).Any(x => !x))
		//			{
		//				Debug.WriteLine("Huh?");
		//			}
		//		}
		//	}

		//	return toBeRemoved;
		//}

		#endregion
	}
}

