using MSS.Types;
using MSS.Types.APValues;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public class SamplePointBuilder : IDisposable
	{
		private readonly SamplePointCache _samplePointCache;

		public SamplePointBuilder(SamplePointCache samplePointCache)
		{
			_samplePointCache = samplePointCache;
		}

		public SizeInt BlockSize => _samplePointCache.BlockSize;
		public FP31VecMath GetVecMath(int limbCount) => _samplePointCache.GetVectorMath(limbCount);

		public (FP31ValArray samplePointXVArray, FP31ValArray samplePointYVArray) BuildSamplePoints(IteratorCoords iteratorCoords)
		{
			var samplePointOffsets = _samplePointCache.GetSamplePointOffsets(iteratorCoords.Delta);
			var fP31ScalarMath = _samplePointCache.GetScalarMath(iteratorCoords.Delta.LimbCount);
			var samplePointsX = BuildSamplePoints(iteratorCoords.StartingCx, samplePointOffsets, fP31ScalarMath);
			var samplePointsY = BuildSamplePoints(iteratorCoords.StartingCy, samplePointOffsets, fP31ScalarMath);

			var valArrayX = new FP31ValArray(samplePointsX);
			var valArrayY = new FP31ValArray(samplePointsY);	

			return (valArrayX, valArrayY);	
		}

		public (Vector256<uint>[] samplePointXVecs, Vector256<uint>[] samplePointYVecs) BuildSamplePointsNew(IteratorCoords iteratorCoords)
		{
			var samplePointOffsets = _samplePointCache.GetSamplePointOffsets(iteratorCoords.Delta);
			var fP31ScalarMath = _samplePointCache.GetScalarMath(iteratorCoords.Delta.LimbCount);
			var samplePointsX = BuildSamplePoints(iteratorCoords.StartingCx, samplePointOffsets, fP31ScalarMath);
			var samplePointsY = BuildSamplePoints(iteratorCoords.StartingCy, samplePointOffsets, fP31ScalarMath);

			var valArrayX = new FP31ValArray(samplePointsX);
			var valArrayY = new FP31ValArray(samplePointsY);

			return (valArrayX.Mantissas, valArrayY.Mantissas);
		}

		public (FP31Val[] samplePointX, FP31Val[] samplePointY) BuildSamplePointsOld(IteratorCoords iteratorCoords)
		{
			var samplePointOffsets = _samplePointCache.GetSamplePointOffsets(iteratorCoords.Delta);
			var fP31ScalarMath = _samplePointCache.GetScalarMath(iteratorCoords.Delta.LimbCount);
			var samplePointsX = BuildSamplePoints(iteratorCoords.StartingCx, samplePointOffsets, fP31ScalarMath);
			var samplePointsY = BuildSamplePoints(iteratorCoords.StartingCy, samplePointOffsets, fP31ScalarMath);

			return (samplePointsX, samplePointsY);
		}

		public static FP31Val[] BuildSamplePoints(FP31Val startValue, FP31Val[] offsets, FP31ScalarMath scalarMath)
		{
			var result = new FP31Val[offsets.Length];

			for (var i = 0; i < offsets.Length; i++)
			{
				result[i] = scalarMath.Add(startValue, offsets[i], "add spd offset to start value");
			}

			return result;
		}

		public static FP31Val[] BuildSamplePointOffsets(FP31Val delta, byte extent, FP31ScalarMath scalarMath)
		{
			var offsets = new FP31Val[extent];

			for (var i = 0; i < extent; i++)
			{
				offsets[i] = scalarMath.Multiply(delta, (byte)i);
			}

			return offsets;
		}

		#region Doubles

		public static double[] BuildSamplePoints(RValue startValue, double[] offsets)
		{
			var sValue = BigIntegerHelper.ConvertToDouble(startValue);
			var result = new double[offsets.Length];

			for (var i = 0; i < offsets.Length; i++)
			{
				result[i] = sValue + offsets[i];
			}

			return result;
		}

		public static double[] BuildSamplePointOffsets(RSize delta, byte extent)
		{
			var dWidth = BigIntegerHelper.ConvertToDouble(delta.Width);
			var offsets = new double[extent];

			for (var i = 0; i < extent; i++)
			{
				offsets[i] = dWidth * i;
			}

			return offsets;
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					_samplePointCache.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region NOT USED

		public static Vector256<uint>[] BuildSamplePoints(FP31Val startValue, Vector256<uint>[] offsets, FP31VecMath fP31VecMath)
		{
			var result = new Vector256<uint>[offsets.Length];

			var limbCount = fP31VecMath.LimbCount;
			var lanes = Vector256<uint>.Count;
			var startVec = new Vector256<uint>[limbCount];

			for (var limbPtr = 0; limbPtr < limbCount; limbPtr++)
			{
				startVec[limbPtr] = Vector256.Create(startValue.Mantissa[limbPtr]);
			}

			var offsetVec = new Vector256<uint>[limbCount];
			var resultVec = new Vector256<uint>[limbCount];

			var valueCount = offsets.Length / limbCount;

			for (var j = 0; j < valueCount; j++)
			{
				var valueOffset = j * limbCount;

				for (var i = 0; i < limbCount; i++)
				{
					offsetVec[i] = offsets[valueOffset + i];
				}

				fP31VecMath.Add(startVec, offsetVec, resultVec);

				for (var i = 0; i < limbCount; i++)
				{
					result[valueOffset + i] = resultVec[i];
				}
			}

			return result;
		}

		public static FP31Val[] BuildSamplePointOffsetsOld(FP31Val delta, byte extent, FP31ScalarMath scalarMath)
		{
			var offsets = new FP31Val[extent];

			var acc = FP31ValHelper.CreateNewZeroFP31Val(scalarMath.ApFixedPointFormat, delta.Precision);

			for (var i = 0; i < extent; i++)
			{
				offsets[i] = acc;

				acc = scalarMath.Add(acc, delta, "BuildSamplePointOffsets");
			}

			return offsets;
		}

		//public static FP31Val[] BuildSamplePointOffsetsOld(FP31Val delta, byte extent, scalarMath scalarMath)
		//{
		//	var offsets = new FP31Val[extent];

		//	for (var i = 0; i < extent; i++)
		//	{
		//		var samplePointOffset = scalarMath.Multiply(delta, (byte)i);
		//		offsets[i] = samplePointOffset;
		//	}

		//	//var result = new FP31Deck(offsets);

		//	return offsets;
		//}

		//private const int EFFECTIVE_BITS_PER_LIMB = 31;

		//private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		//private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		//public static FP31Deck BuildSamplePointOffsets(FP31Val startValue, FP31Val delta, byte extent, VecMath9 vecMath9)
		//{
		//	var limbCount = delta.LimbCount;
		//	var result = new FP31Deck(limbCount, extent);
		//	var vectorCount = result.VectorCount;

		//	var integerMultiplesDeck = BuildIntegerMultiplesDeck(delta, vecMath9);

		//	result.UpdateFrom(integerMultiplesDeck, 0, 0, FP31Deck.Lanes);


		//	// Update the result, limb by limb, vector by vector
		//	// For each succesive vector, create a vector that is a duplicate of the right-most value from the last vector
		//	// get the limb values from that vector, and add this to the next set of limb values

		//	var carryVectors = Enumerable.Repeat(Vector256<uint>.Zero, vectorCount).ToArray();

		//	for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
		//	{
		//		var resultLimbVecs = result.GetLimbVectorsUW(limbPtr);

		//		//			0, 1, 2, 3, 4, 5, 6, 7
		//		//   	+   8, 8, 8, 8, 8, 8, 8, 8
		//		//   	=	8, 9, 10, 11, 12, 13, 14		

		//		var left = resultLimbVecs[0];
		//		var factor7 = resultLimbVecs[0].GetElement(7);
		//		var factor7Vec = Vector256.Create(factor7);

		//		var startValVector = Vector256.Create(startValue.Mantissa[limbPtr]);

		//		for (var vecPtr = 1; vecPtr < vectorCount; vecPtr++)
		//		{
		//			var sumVector = Avx2.Add(left, factor7Vec);
		//			left = sumVector;

		//			var sumVector1 = Avx2.Add(sumVector, carryVectors[vecPtr]);

		//			var sumVector2 = Avx2.And(sumVector1, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
		//			var carryVector2 = Avx2.ShiftRightLogical(sumVector1, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

		//			var sumVector3 = Avx2.Add(sumVector2, startValVector);

		//			resultLimbVecs[vecPtr] = Avx2.And(sumVector3, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
		//			var carryVector3 = Avx2.ShiftRightLogical(sumVector3, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

		//			carryVectors[vecPtr] = Avx2.Add(carryVector2, carryVector3);

		//		}
		//	}

		//	return result;
		//}

		//private static FP31Deck BuildIntegerMultiplesDeck(FP31Val x, VecMath9 vecMath9)
		//{
		//	// Create a vector, vec2 that has 0, 1, 2, 3, 4, 5, 6 and 7 times the value of delta.

		//	var vec1 = new FP31Deck(x, FP31Deck.Lanes);
		//	var vec2 = vec1.Clone();

		//	vec1.SetMantissa(0, Enumerable.Repeat(0u, x.LimbCount).ToArray());
		//	vec1.SetMantissa(1, vec1.GetMantissa(0));
		//	vec2.SetMantissa(0, vec1.GetMantissa(0));

		//	var vResult = new FP31Deck(x.LimbCount, FP31Deck.Lanes);

		//	var inPlayList = vResult.GetNewInPlayList();

		//	vecMath9.Add(vec1, vec2, vResult, inPlayList);  //		0, 0, 1, 1, 1, 1, 1, 1
		//	vec2.SetMantissa(2, vResult.GetMantissa(0));    //	 +  0, 1, 1, 1, 1, 1, 1, 1
		//													//      0, 1, 2, 2, 2, 2, 2, 2 

		//	vecMath9.Add(vec1, vec2, vResult, inPlayList);  //		0, 0, 0, 1, 1, 1, 1, 1
		//	vec2.SetMantissa(2, vResult.GetMantissa(1));    //	 +  0, 1, 2, 2, 2, 2, 2, 2
		//													//      0, 1, 2, 3, 3, 3, 3, 3 

		//	vecMath9.Add(vec1, vec2, vResult, inPlayList);  //		0, 0, 0, 0, 1, 1, 1, 1
		//	vec2.SetMantissa(2, vResult.GetMantissa(1));    //	 +  0, 1, 2, 3, 3, 3, 3, 3
		//													//      0, 1, 2, 3, 4, 4, 4, 4 


		//	vecMath9.Add(vec1, vec2, vResult, inPlayList);
		//	vec2.SetMantissa(3, vResult.GetMantissa(2));

		//	return vec2;
		//}

		#endregion
	}
}
