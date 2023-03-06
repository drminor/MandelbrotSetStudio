using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	internal class FP31VecMathUPointers : IDisposable
	{
		#region Private Properties

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC_L = Vector256.Create(LOW31_BITS_SET_L);

		private const uint SIGN_BIT_MASK = 0x3FFFFFFF;
		private static readonly Vector256<uint> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private const uint RESERVED_BIT_MASK = 0x80000000;
		private static readonly Vector256<uint> RESERVED_BIT_MASK_VEC = Vector256.Create(RESERVED_BIT_MASK);

		private const int TEST_BIT_30 = 0x40000000; // bit 30 is set.
		private static readonly Vector256<int> TEST_BIT_30_VEC = Vector256.Create(TEST_BIT_30);

		private static readonly Vector256<int> ZERO_VEC = Vector256<int>.Zero;
		private static readonly Vector256<uint> ALL_BITS_SET_VEC = Vector256<uint>.AllBitsSet;

		private static readonly Vector256<uint> SHUFFLE_EXP_LOW_VEC = Vector256.Create(0u, 0u, 1u, 1u, 2u, 2u, 3u, 3u);
		private static readonly Vector256<uint> SHUFFLE_EXP_HIGH_VEC = Vector256.Create(4u, 4u, 5u, 5u, 6u, 6u, 7u, 7u);

		private static readonly Vector256<uint> SHUFFLE_PACK_LOW_VEC = Vector256.Create(0u, 2u, 4u, 6u, 0u, 0u, 0u, 0u);
		private static readonly Vector256<uint> SHUFFLE_PACK_HIGH_VEC = Vector256.Create(0u, 0u, 0u, 0u, 0u, 2u, 4u, 6u);

		private PairOfVecBuffer _squareResult0;
		private PairOfVecBuffer _squareResult1;
		private PairOfVecBuffer _squareResult2;

		private Vector256<uint> _ones;

		byte _shiftAmount;
		byte _inverseShiftAmount;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VecMathUPointers(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			_squareResult0 = new PairOfVecBuffer(LimbCount);
			_squareResult1 = new PairOfVecBuffer(LimbCount, isDeep: true);
			_squareResult2 = new PairOfVecBuffer(LimbCount, isDeep: true);

			_ones = Vector256.Create(1u);


			_shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;
			_inverseShiftAmount = (byte)(31 - _shiftAmount);

			MathOpCounts = new MathOpCounts();
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; private set; }

		public MathOpCounts MathOpCounts { get; init; }

		#endregion

		#region Multiply and Square

		public void Square(VecBuffer a, VecBuffer result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			//CheckReservedBitIsClear(a, "Squaring");

			ClearLimbSet(result);

			ConvertFrom2C(a, _squareResult0);
			//MathOpCounts.NumberOfConversions++;

			SquareInternal(_squareResult0, _squareResult1);
			SumThePartials(_squareResult1, _squareResult2);
			ShiftAndTrim(_squareResult2, result);
		}

		private unsafe void SquareInternal(PairOfVecBuffer sourceBuffer, PairOfVecBuffer resultBuffer)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.ClearManatissMems();

			for (int leftPtr = 0; leftPtr < LimbCount; leftPtr++)
			{
				for (int rightPtr = leftPtr; rightPtr < LimbCount; rightPtr++)
				{
					var resultPtr = leftPtr + rightPtr;  // 0+0, 0+1; 1+1, 0, 1, 2

					// Load, Load, Multiply
					var left1 = Avx2.LoadDquVector256((uint*)sourceBuffer.Vec1.GetBytePointer(leftPtr * 32));
					var right1 = Avx2.LoadDquVector256((uint*)sourceBuffer.Vec1.GetBytePointer(rightPtr * 32));
					var productVector1 = Avx2.Multiply(left1, right1);

					// Load, Load, Multiply
					var left2 = Avx2.LoadDquVector256((uint*)sourceBuffer.Vec2.GetBytePointer(leftPtr * 32));
					var right2 = Avx2.LoadDquVector256((uint*)sourceBuffer.Vec2.GetBytePointer(rightPtr * 32));
					var productVector2 = Avx2.Multiply(left2, right2);

					IncrementNoMultiplications(8);

					if (rightPtr > leftPtr)
					{
						//product *= 2;
						productVector1 = Avx2.ShiftLeftLogical(productVector1, 1);
						productVector2 = Avx2.ShiftLeftLogical(productVector2, 1);
					}

					// 0/1; 1/2; 2/3


					// And, SRL
					var resultLimb1Low = Avx2.And(productVector1, HIGH33_MASK_VEC_L);
					var resultLimb1Hi = Avx2.ShiftRightLogical(productVector1, EFFECTIVE_BITS_PER_LIMB);

					var result1LowPtr = (ulong*)resultBuffer.Vec1.GetBytePointer(resultPtr * 32);
					var result1HiPtr = (ulong*)resultBuffer.Vec1.GetBytePointer((resultPtr + 1) * 32);

					//// Load, Load
					//var result1Low = Avx2.LoadDquVector256(result1LowPtr);
					//var result1Hi = Avx2.LoadDquVector256(result1HiPtr);

					//// Add, Add
					//result1Low = Avx2.Add(result1Low, resultLimb1Low);
					//result1Hi = Avx2.Add(result1Hi, resultLimb1Hi);

					//// Store, Store
					//Avx2.Store(result1LowPtr, result1Low);
					//Avx2.Store(result1HiPtr, result1Hi);

					Avx2.Store(result1LowPtr, resultLimb1Low);
					Avx2.Store(result1HiPtr, resultLimb1Hi);


					// And, SRL
					var resultLimb2Low = Avx2.And(productVector2, HIGH33_MASK_VEC_L);
					var resultLimb2Hi = Avx2.ShiftRightLogical(productVector2, EFFECTIVE_BITS_PER_LIMB);

					var result2LowPtr = (ulong*)resultBuffer.Vec2.GetBytePointer(resultPtr * 32);
					var result2HiPtr = (ulong*)resultBuffer.Vec2.GetBytePointer((resultPtr + 1) * 32);

					//// Load, Load
					//var result2Low = Avx2.LoadDquVector256(result2LowPtr);
					//var result2Hi = Avx2.LoadDquVector256(result2HiPtr);

					//// Add, Add
					//result2Low = Avx2.Add(result2Low, resultLimb2Low);
					//result2Hi = Avx2.Add(result2Hi, resultLimb2Hi);

					//// Store, Store
					//Avx2.Store(result2LowPtr, result2Low);
					//Avx2.Store(result2HiPtr, result2Hi);


					Avx2.Store(result2LowPtr, resultLimb2Low);
					Avx2.Store(result2HiPtr, resultLimb2Hi);

					//MathOpCounts.NumberOfSplits += 4;
					//MathOpCounts.NumberOfAdditions += 16;
				}
			}
		}

		[Conditional("PERF")]
		private void IncrementNoMultiplications(int amount)
		{
			MathOpCounts.NumberOfMultiplications += amount;
		}

		#endregion

		#region Multiplication Post Processing

		private unsafe void SumThePartials(PairOfVecBuffer sourceBuffer, PairOfVecBuffer resultBuffer)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var carry1 = Vector256<ulong>.Zero; //Avx2.Xor(_carryVectorsLong1, _carryVectorsLong1);
			var carry2 = Vector256<ulong>.Zero; //Avx2.Xor(_carryVectorsLong2, _carryVectorsLong2);

			var resultLength = LimbCount * 2;

			for (int limbPtr = 0; limbPtr < resultLength; limbPtr++)
			{

				var source1Ptr = (ulong*)sourceBuffer.Vec1.GetBytePointer(limbPtr * 32);
				var source1Limb = Avx2.LoadDquVector256(source1Ptr);
				var partialSum1 = Avx2.Add(source1Limb, carry1);
				source1Limb = Avx2.Xor(source1Limb, source1Limb);
				Avx2.Store(source1Ptr, source1Limb);

				var source2Ptr = (ulong*)sourceBuffer.Vec2.GetBytePointer(limbPtr * 32);
				var source2Limb = Avx2.LoadDquVector256(source2Ptr);
				var partialSum2 = Avx2.Add(source2Limb, carry2);
				source2Limb = Avx2.Xor(source2Limb, source2Limb);
				Avx2.Store(source1Ptr, source2Limb);

				var result1Limb = Avx2.And(partialSum1, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
				var result2Limb = Avx2.And(partialSum2, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

				var result1Ptr = (ulong*)resultBuffer.Vec1.GetBytePointer(limbPtr * 32);
				var result2Ptr = (ulong*)resultBuffer.Vec2.GetBytePointer(limbPtr * 32);

				Avx2.Store(result1Ptr, result1Limb);
				Avx2.Store(result2Ptr, result2Limb);

				carry1 = Avx2.ShiftRightLogical(partialSum1, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
				carry2 = Avx2.ShiftRightLogical(partialSum2, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			}
		}

		private unsafe void ShiftAndTrim(PairOfVecBuffer sourceBuffer, VecBuffer resultBuffer)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var sourceIndex = LimbCount; //Math.Max(sourceBuffer.Vec1.Length - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				// --------------------
				// Calculate the lo end

				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				// Load
				var sourcePtr = (ulong*)sourceBuffer.Vec1.GetBytePointer((limbPtr + sourceIndex) * 32);
				var sourceLimb = Avx2.LoadDquVector256(sourcePtr);

				// Or
				var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb, _shiftAmount), HIGH33_MASK_VEC_L);

				// Take the top shiftAmount of bits from the previous limb
				//Load
				var prevSourcePtr = (ulong*)sourceBuffer.Vec1.GetBytePointer((limbPtr + sourceIndex - 1) * 32);
				var prevSourceLimb = Avx2.LoadDquVector256(prevSourcePtr);
				
				// Or
				wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb, HIGH33_MASK_VEC_L), _inverseShiftAmount));

				// --------------------
				// Calculate the hi end

				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				// Load
				sourcePtr = (ulong*)sourceBuffer.Vec2.GetBytePointer((limbPtr + sourceIndex) * 32);
				var sourceLimb2 = Avx2.LoadDquVector256(sourcePtr);

				// Or
				var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb2, _shiftAmount), HIGH33_MASK_VEC_L);

				// Take the top shiftAmount of bits from the previous limb
				// Load
				prevSourcePtr = (ulong*)sourceBuffer.Vec2.GetBytePointer((limbPtr + sourceIndex - 1) * 32);
				var prevSourceLimb2 = Avx2.LoadDquVector256(prevSourcePtr);
				
				// Or
				wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb2, HIGH33_MASK_VEC_L), _inverseShiftAmount));

				// ---------------
				// Pack Hi and Low 

				// Permute, Permute, Or
				var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
				var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
				var resultLimb = Avx2.Or(low128, high128);

				// Store
				var resultPtr = (uint*)resultBuffer.GetBytePointer(limbPtr * 32);
				Avx2.Store(resultPtr, resultLimb);

				//MathOpCounts.NumberOfSplits += 4;
			}
		}

		#endregion

		#region Add and Subtract

		public void Sub(VecBuffer left, VecBuffer right, VecBuffer result)
		{
			//CheckReservedBitIsClear(b, "Negating B");

			using var tempResult = CreateNewLimbSet();

			Negate(right, tempResult);
			//MathOpCounts.NumberOfConversions++;

			Add(left, tempResult, result);
		}

		public unsafe void Add(VecBuffer leftBuffer, VecBuffer rightBuffer, VecBuffer resultBuffer)
		{
			var carryVectors = Vector256<uint>.Zero;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var left = Avx2.LoadDquVector256((uint*)leftBuffer.GetBytePointer(limbPtr * 32));
				var right = Avx2.LoadDquVector256((uint*)rightBuffer.GetBytePointer(limbPtr * 32));

				var sum = Avx2.Add(left, right);
				var newValue = Avx2.Add(sum, carryVectors);
				//MathOpCounts.NumberOfAdditions += 2;

				var resultlimb = Avx2.And(newValue, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
				var resultPtr = (uint*)resultBuffer.GetBytePointer(limbPtr * 32);
				Avx2.Store(resultPtr, resultlimb);

				carryVectors = Avx2.ShiftRightLogical(newValue, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
			}
		}

		public unsafe Vector256<uint> GetMslOfSum(VecBuffer leftBuffer, VecBuffer rightBuffer)
		{
			var carryVectors = Vector256<uint>.Zero;

			var result = Vector256<uint>.Zero;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var left = Avx2.LoadDquVector256((uint*)leftBuffer.GetBytePointer(limbPtr * 32));
				var right = Avx2.LoadDquVector256((uint*)rightBuffer.GetBytePointer(limbPtr * 32));

				var sum = Avx2.Add(left, right);
				var newValue = Avx2.Add(sum, carryVectors);
				//MathOpCounts.NumberOfAdditions += 2;

				result = Avx2.And(newValue, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.

				carryVectors = Avx2.ShiftRightLogical(newValue, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
			}

			return result;
		}


		#endregion

		#region Two Compliment Support

		private unsafe void Negate(VecBuffer sourceBuffer, VecBuffer resultBuffer)
		{
			var carryVectors = _ones;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var source = Avx2.LoadDquVector256((uint*)sourceBuffer.GetBytePointer(limbPtr * 32));

				var notVector = Avx2.Xor(source, ALL_BITS_SET_VEC);
				var newValuesVector = Avx2.Add(notVector, carryVectors);
				//MathOpCounts.NumberOfAdditions += 2;

				var resultLimb = Avx2.And(newValuesVector, HIGH33_MASK_VEC); ;
				Avx2.Store((uint*)resultBuffer.GetBytePointer(limbPtr * 32), resultLimb);

				carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
				//MathOpCounts.NumberOfSplits++;
			}
		}

		private unsafe void ConvertFrom2C(VecBuffer sourceBuffer, PairOfVecBuffer resultBufferPair)
		{
			//CheckReservedBitIsClear(source, "ConvertFrom2C");

			var signBitFlags = GetSignBits(sourceBuffer, out var signBitVecs);

			if (signBitFlags == -1)
			{

				// All positive values
				for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					var source = Avx2.LoadDquVector256((uint*)sourceBuffer.GetBytePointer(limbPtr * 32));

					// TODO: Is Masking the high bits really required.
					// Take the lower 4 values and set the low halves of each result
					var resultLimbLower = Avx2.And(Avx2.PermuteVar8x32(source, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);
					var resultPtr = (uint*)resultBufferPair.Vec1.GetBytePointer(limbPtr * 32);
					Avx2.Store(resultPtr, resultLimbLower);

					// Take the higher 4 values and set the high halves of each result
					var resultLimbUpper = Avx2.And(Avx2.PermuteVar8x32(source, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
					resultPtr = (uint*)resultBufferPair.Vec2.GetBytePointer(limbPtr * 32);
					Avx2.Store(resultPtr, resultLimbUpper);
				}
			}
			else
			{
				// Mixed Positive and Negative values
				var carryVectors = _ones;

				for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					var source = Avx2.LoadDquVector256((uint*)sourceBuffer.GetBytePointer(limbPtr * 32));

					var notVector = Avx2.Xor(source, ALL_BITS_SET_VEC);
					var newValuesVector = Avx2.Add(notVector, carryVectors);
					//MathOpCounts.NumberOfAdditions += 2;

					var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

					//MathOpCounts.NumberOfSplits++;

					var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source.AsByte(), signBitVecs.AsByte())).AsUInt32();

					// Take the lower 4 values and set the low halves of each result
					//result.Lower[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);
					var resultLimbLower = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);
					var resultPtr = (uint*)resultBufferPair.Vec1.GetBytePointer(limbPtr * 32);
					Avx2.Store(resultPtr, resultLimbLower);

					// Take the higher 4 values and set the high halves of each result
					//result.Upper[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
					var resultLimbUpper = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
					resultPtr = (uint*)resultBufferPair.Vec2.GetBytePointer(limbPtr * 32);
					Avx2.Store(resultPtr, resultLimbUpper);
				}
			}
		}

		private unsafe int GetSignBits(VecBuffer sourceBuffer, out Vector256<int> signBitVecs)
		{
			var sourceMsl = Avx2.LoadDquVector256((int*)sourceBuffer.GetBytePointer((LimbCount - 1) * 32));

			signBitVecs = Avx2.CompareEqual(Avx2.And(sourceMsl, TEST_BIT_30_VEC), ZERO_VEC);
			return Avx2.MoveMask(signBitVecs.AsByte());
		}

		#endregion

		#region Comparison

		public Vector256<int> CreateVectorForComparison(uint value)
		{
			var fp31Val = FP31ValHelper.CreateFP31Val(new RValue(value, 0), ApFixedPointFormat);
			var msl = (int)fp31Val.Mantissa[^1] - 1;
			var result = Vector256.Create(msl);

			return result;
		}

		public unsafe void IsGreaterOrEqThan(ref Vector256<uint> leftMsl, ref Vector256<int> right, ref Vector256<int> escapedFlagsVec)
		{
			// TODO: Is masking the Sign Bit really necessary.
			var sansSign = Avx2.And(leftMsl, SIGN_BIT_MASK_VEC);
			escapedFlagsVec = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);

			//MathOpCounts.NumberOfGrtrThanOps++;
		}

		#endregion

		#region Value Support

		public unsafe VecBuffer CreateNewLimbSet()
		{
			var bytes = new byte[LimbCount];
			Array.Clear(bytes);
			var result = new VecBuffer(bytes);
			return result;
		}

		public unsafe void CopyLimbSet(VecBuffer source, VecBuffer dest)
		{
			source.AsSpan().CopyTo(dest.AsSpan());
		}

		private void ClearLimbSet(Vector256<uint>[] limbs)
		{
			for (var i = 0; i < limbs.Length; i++)
			{
				limbs[i] = Vector256<uint>.Zero; // Avx2.Xor(limbs[i], limbs[i]);
			}
		}

		private unsafe void ClearLimbSet(VecBuffer sourceBuffer)
		{
			for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var resultPtr = (uint*)sourceBuffer.GetBytePointer(limbPtr * 32);
				Avx2.Store(resultPtr, Vector256<uint>.Zero);
			}
		}

		#endregion

		#region IDisposable (Main Class)

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					_squareResult0.Dispose();
					_squareResult1.Dispose();
					_squareResult2.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~FP31VecMathUPointers()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion


		private class PairOfVecBuffer : IDisposable
		{
			public PairOfVecBuffer(int limbCount, bool isDeep = false)
			{
				if (isDeep)
				{
					_vec1Storage = new byte[limbCount * 2 * 32];
					Array.Clear(_vec1Storage);
					Vec1 = new VecBuffer(_vec1Storage);

					_vec2Storage = new byte[limbCount * 2 * 32];
					Array.Clear(_vec2Storage);
					Vec2 = new VecBuffer(_vec2Storage);
				}
				else
				{
					_vec1Storage = new byte[limbCount * 32];
					Array.Clear(_vec1Storage);
					Vec1 = new VecBuffer(_vec1Storage);

					_vec2Storage = new byte[limbCount * 32];
					Array.Clear(_vec2Storage);
					Vec2 = new VecBuffer(_vec2Storage);
				}

				//ClearLimbSet();
			}

			private byte[] _vec1Storage;
			private byte[] _vec2Storage;

			public VecBuffer Vec1 { get; init; }
			public VecBuffer Vec2 { get; init; }


			//public void ClearLimbSet()
			//{
			//	for (var i = 0; i < Lower.Length; i++)
			//	{
			//		Lower[i] = Vector256<uint>.Zero;
			//		Upper[i] = Vector256<uint>.Zero;
			//	}
			//}

			#region IDisposable

			private bool disposedValue;

			protected virtual void Dispose(bool disposing)
			{
				if (!disposedValue)
				{
					if (disposing)
					{
						// Dispose managed state (managed objects)
						Vec1.Dispose();
						Vec2.Dispose();
					}

					// TODO: free unmanaged resources (unmanaged objects) and override finalizer
					// TODO: set large fields to null
					disposedValue = true;
				}
			}

			// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
			// ~PairOfVecBuffer()
			// {
			//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			//     Dispose(disposing: false);
			// }

			public void Dispose()
			{
				// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}

			#endregion
		}
	}
}
