using MSS.Common.APValues;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	internal static class SamplePointBuilder
	{
		public static FP31Val[] BuildSamplePoints(FP31Val startValue, FP31Val[] offsets, ScalarMath9 scalarMath9)
		{
			var result = new FP31Val[offsets.Length];

			for (var i = 0; i < offsets.Length; i++)
			{
				result[i] = scalarMath9.Add(startValue, offsets[i], "add spd offset to start value");
			}

			return result;
		}

		public static FP31Val[] BuildSamplePointOffsets(FP31Val delta, byte extent, ScalarMath9 scalarMath9)
		{
			var offsets = new FP31Val[extent];

			var acc = FP31ValHelper.CreateNewZeroFP31Val(scalarMath9.ApFixedPointFormat, delta.Precision);

			for (var i = 0; i < extent; i++)
			{
				offsets[i] = acc;

				acc = scalarMath9.Add(acc, delta, "BuildSamplePointOffsets"); 
			}

			return offsets;
		}


		#region NOT USED

		public static FP31Val[] BuildSamplePointOffsetsOld(FP31Val delta, byte extent, ScalarMath9 scalarMath9)
		{
			var offsets = new FP31Val[extent];

			for (var i = 0; i < extent; i++)
			{
				var samplePointOffset = scalarMath9.Multiply(delta, (byte)i);
				offsets[i] = samplePointOffset;
			}

			//var result = new FP31Deck(offsets);

			return offsets;
		}

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		public static FP31Deck BuildSamplePointOffsets(FP31Val startValue, FP31Val delta, byte extent, VecMath9 vecMath9)
		{
			var limbCount = delta.LimbCount;
			var result = new FP31Deck(limbCount, extent);
			var vectorCount = result.VectorCount;

			var integerMultiplesDeck = BuildIntegerMultiplesDeck(delta, vecMath9);

			result.UpdateFrom(integerMultiplesDeck, 0, 0, FP31Deck.Lanes);


			// Update the result, limb by limb, vector by vector
			// For each succesive vector, create a vector that is a duplicate of the right-most value from the last vector
			// get the limb values from that vector, and add this to the next set of limb values

			var carryVectors = Enumerable.Repeat(Vector256<uint>.Zero, vectorCount).ToArray();

			for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
			{
				var resultLimbVecs = result.GetLimbVectorsUW(limbPtr);

				//			0, 1, 2, 3, 4, 5, 6, 7
				//   	+   8, 8, 8, 8, 8, 8, 8, 8
				//   	=	8, 9, 10, 11, 12, 13, 14		

				var left = resultLimbVecs[0];
				var factor7 = resultLimbVecs[0].GetElement(7);
				var factor7Vec = Vector256.Create(factor7);

				var startValVector = Vector256.Create(startValue.Mantissa[limbPtr]);

				for (var vecPtr = 1; vecPtr < vectorCount; vecPtr++)
				{
					var sumVector = Avx2.Add(left, factor7Vec);
					left = sumVector;

					var sumVector1 = Avx2.Add(sumVector, carryVectors[vecPtr]);

					var sumVector2 = Avx2.And(sumVector1, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					var carryVector2 = Avx2.ShiftRightLogical(sumVector1, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

					var sumVector3 = Avx2.Add(sumVector2, startValVector);

					resultLimbVecs[vecPtr] = Avx2.And(sumVector3, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					var carryVector3 = Avx2.ShiftRightLogical(sumVector3, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

					carryVectors[vecPtr] = Avx2.Add(carryVector2, carryVector3);

				}
			}

			return result;
		}

		private static FP31Deck BuildIntegerMultiplesDeck(FP31Val x, VecMath9 vecMath9)
		{
			// Create a vector, vec2 that has 0, 1, 2, 3, 4, 5, 6 and 7 times the value of delta.

			var vec1 = new FP31Deck(x, FP31Deck.Lanes);
			var vec2 = vec1.Clone();

			vec1.SetMantissa(0, Enumerable.Repeat(0u, x.LimbCount).ToArray());
			vec1.SetMantissa(1, vec1.GetMantissa(0));
			vec2.SetMantissa(0, vec1.GetMantissa(0));

			var vResult = new FP31Deck(x.LimbCount, FP31Deck.Lanes);

			var inPlayList = vResult.GetNewInPlayList();

			vecMath9.Add(vec1, vec2, vResult, inPlayList);  //		0, 0, 1, 1, 1, 1, 1, 1
			vec2.SetMantissa(2, vResult.GetMantissa(0));    //	 +  0, 1, 1, 1, 1, 1, 1, 1
															//      0, 1, 2, 2, 2, 2, 2, 2 

			vecMath9.Add(vec1, vec2, vResult, inPlayList);  //		0, 0, 0, 1, 1, 1, 1, 1
			vec2.SetMantissa(2, vResult.GetMantissa(1));    //	 +  0, 1, 2, 2, 2, 2, 2, 2
															//      0, 1, 2, 3, 3, 3, 3, 3 

			vecMath9.Add(vec1, vec2, vResult, inPlayList);  //		0, 0, 0, 0, 1, 1, 1, 1
			vec2.SetMantissa(2, vResult.GetMantissa(1));    //	 +  0, 1, 2, 3, 3, 3, 3, 3
															//      0, 1, 2, 3, 4, 4, 4, 4 


			vecMath9.Add(vec1, vec2, vResult, inPlayList);
			vec2.SetMantissa(3, vResult.GetMantissa(2));

			return vec2;
		}

		#endregion


	}
}
