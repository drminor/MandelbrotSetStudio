using MSS.Common.MSetGenerator;
using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSS.Common
{
	public class FP31VecMathL2 : IFP31VecMath
	{
		#region Private Properties

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC_L = Vector256.Create(LOW31_BITS_SET_L);

		private const uint SIGN_BIT_MASK = 0x3FFFFFFF;
		private static readonly Vector256<uint> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private const int TEST_BIT_30 = 0x40000000; // bit 30 is set.
		private static readonly Vector256<int> TEST_BIT_30_VEC = Vector256.Create(TEST_BIT_30);

		private static readonly Vector256<int> ZERO_VEC = Vector256<int>.Zero;
		private static readonly Vector256<uint> XOR_BITS_VEC = Vector256.Create(LOW31_BITS_SET);

		private static readonly Vector256<uint> SHUFFLE_EXP_LOW_VEC = Vector256.Create(0u, 0u, 1u, 1u, 2u, 2u, 3u, 3u);
		private static readonly Vector256<uint> SHUFFLE_EXP_HIGH_VEC = Vector256.Create(4u, 4u, 5u, 5u, 6u, 6u, 7u, 7u);

		private static readonly Vector256<uint> SHUFFLE_PACK_LOW_VEC = Vector256.Create(0u, 2u, 4u, 6u, 0u, 0u, 0u, 0u);
		private static readonly Vector256<uint> SHUFFLE_PACK_HIGH_VEC = Vector256.Create(0u, 0u, 0u, 0u, 0u, 2u, 4u, 6u);

		private Vector256<uint> _ones;

		private Vector256<ulong> _carryLong1;
		private Vector256<ulong> _carryLong2;

		private byte _shiftAmount;
		private byte _inverseShiftAmount;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VecMathL2(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			Debug.Assert(LimbCount == 2, $"Attempting to construct a FP31VecMathL2 with a ApFixedPointFormat having a limb count of {LimbCount}.");

			_ones = Vector256.Create(1u);

			_carryLong1 = Vector256<ulong>.Zero;
			_carryLong2 = Vector256<ulong>.Zero;


			_shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;
			_inverseShiftAmount = (byte)(31 - _shiftAmount);

			MathOpCounts = new MathOpCounts();
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; private set; }

		public MathOpCounts MathOpCounts { get; init; }

		public string Implementation => "FP31VecMath-L2";

		#endregion

		#region Square

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Square(Vector256<uint>[] source, Vector256<uint>[] result)
		{
			// Our multiplication routines don't support 2's compliment,
			// So we must convert back from two's complement to standard.

			// The result of squaring is always positive, so we don't have to convert
			// back to two's compliment afterwards.

			//FP31VecMathHelper.CheckReservedBitIsClear(source, "Squaring");

			//ConvertFrom2C(source, _squareResult0);
			//var signBitFlags = GetSignBits(source, ref _signBitVecs);

			var signBitVecs = Avx2.CompareEqual(Avx2.And(source[LimbCount - 1].AsInt32(), TEST_BIT_30_VEC), ZERO_VEC);
			var signBitFlags = Avx2.MoveMask(signBitVecs.AsByte());

			IncrementComparisonsCount(8);

			Vector256<uint> result0_Low_L0;
			Vector256<uint> result0_High_L0;

			Vector256<uint> result0_Low_L1;
			Vector256<uint> result0_High_L1;

			if (signBitFlags == -1)
			{
				// All positive values

				// TODO: Is Masking the high bits really required.

				// i = 0
				// Take the lower 4 values and set the low halves of each result
				result0_Low_L0 = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L0 = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// i = 1
				// Take the lower 4 values and set the low halves of each result
				result0_Low_L1 = Avx2.And(Avx2.PermuteVar8x32(source[1], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L1 = Avx2.And(Avx2.PermuteVar8x32(source[1], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
			}
			else
			{
				// Mixed Positive and Negative values

				// i = 0
				var notVector1 = Avx2.Xor(source[0], XOR_BITS_VEC);
				var newValuesVector1 = Avx2.Add(notVector1, _ones);

				var limbValues = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);                // The low 31 bits of the sum is the result.
				var carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

				var cLimbValues = Avx2.BlendVariable(limbValues, source[0], signBitVecs.AsUInt32());

				// Take the lower 4 values and set the low halves of each result
				result0_Low_L0 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L0 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// i = 1
				var notVector2 = Avx2.Xor(source[1], XOR_BITS_VEC);
				var newValuesVector2 = Avx2.Add(notVector2, carry);

				var limbValues2 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                  // The low 31 bits of the sum is the result.
				//carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

				var cLimbValues2 = Avx2.BlendVariable(limbValues2, source[1], signBitVecs.AsUInt32());

				// Take the lower 4 values and set the low halves of each result
				result0_Low_L1 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L1 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				WarnIfAnyCarry(newValuesVector2, signBitVecs, "Negating for Squaring.");

				IncrementNegationsCount(16);
			}

			IncrementConversionsCount(32);

			//SquareInternal(_squareResult0, _squareResult1);

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.Clear();

			// j = 0, i = 0, r = 0/1
			var productVector11 = Avx2.Multiply(result0_Low_L0, result0_Low_L0);
			var productVector12 = Avx2.Multiply(result0_High_L0, result0_High_L0);

			//var result1_Low_L0 = Avx2.And(productVector11, HIGH33_MASK_VEC_L);
			var result1_Low_L1 = Avx2.ShiftRightLogical(productVector11, EFFECTIVE_BITS_PER_LIMB);

			//var result1_High_L0 = Avx2.And(productVector12, HIGH33_MASK_VEC_L);
			var result1_High_L1 = Avx2.ShiftRightLogical(productVector12, EFFECTIVE_BITS_PER_LIMB);

			// j = 0, i = 1, r = 1/2
			var productVector21 = Avx2.ShiftLeftLogical(Avx2.Multiply(result0_Low_L0, result0_Low_L1), 1);
			var productVector22 = Avx2.ShiftLeftLogical(Avx2.Multiply(result0_High_L0, result0_High_L1), 1);

			result1_Low_L1 = Avx2.Add(result1_Low_L1, Avx2.And(productVector21, HIGH33_MASK_VEC_L));
			var result1_Low_L2 = Avx2.ShiftRightLogical(productVector21, EFFECTIVE_BITS_PER_LIMB);

			result1_High_L1 = Avx2.Add(result1_High_L1, Avx2.And(productVector22, HIGH33_MASK_VEC_L));
			var result1_High_L2 = Avx2.ShiftRightLogical(productVector22, EFFECTIVE_BITS_PER_LIMB);

			// j = 1, i = 1, r = 2/3
			var productVector31 = Avx2.Multiply(result0_Low_L1, result0_Low_L1);
			var productVector32 = Avx2.Multiply(result0_High_L1, result0_High_L1);

			result1_Low_L2 = Avx2.Add(result1_Low_L2, Avx2.And(productVector31, HIGH33_MASK_VEC_L));
			var result1_Low_L3 = Avx2.ShiftRightLogical(productVector31, EFFECTIVE_BITS_PER_LIMB);

			result1_High_L2 = Avx2.Add(result1_High_L2, Avx2.And(productVector32, HIGH33_MASK_VEC_L));
			var result1_High_L3 = Avx2.ShiftRightLogical(productVector32, EFFECTIVE_BITS_PER_LIMB);

			IncrementMultiplicationsCount(48);
			IncrementAdditionsCount(32);
			IncrementSplitsCount(32);

			//SumThePartials(_squareResult1, _squareResult2);

			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			// i = 0

			// i = 1

			var result2_Low_L1 = Avx2.And(result1_Low_L1, HIGH33_MASK_VEC_L);				// The low 31 bits of the sum is the result.
			var result2_High_L1 = Avx2.And(result1_High_L1, HIGH33_MASK_VEC_L);             // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(result1_Low_L1, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(result1_High_L1, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.


			// i = 2
			var partialSum2Low = Avx2.Add(result1_Low_L2, _carryLong1);
			var partialSum2High = Avx2.Add(result1_High_L2, _carryLong2);

			var result2_Low_L2 = Avx2.And(partialSum2Low, HIGH33_MASK_VEC_L);				// The low 31 bits of the sum is the result.
			var result2_High_L2 = Avx2.And(partialSum2High, HIGH33_MASK_VEC_L);             // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(partialSum2Low, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(partialSum2High, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			// i = 3
			var partialSum3Low = Avx2.Add(result1_Low_L3, _carryLong1);
			var partialSum3High = Avx2.Add(result1_High_L3, _carryLong2);

			var result2_Low_L3 = Avx2.And(partialSum3Low, HIGH33_MASK_VEC_L);				// The low 31 bits of the sum is the result.
			var result2_High_L3 = Avx2.And(partialSum3High, HIGH33_MASK_VEC_L);             // The low 31 bits of the sum is the result.

			// TODO: Throw overflow if any bit:31 - 63 of withCarries is set.
			//_carryLong1 = Avx2.ShiftRightLogical(partialSum3Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			//_carryLong2 = Avx2.ShiftRightLogical(partialSum3High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			IncrementAdditionsCount(32);
			IncrementSplitsCount(28);

			//ShiftAndTrim(_squareResult2, result);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			//for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)

			// i = 0

			// Calculate the lo end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var wideResult1Low = Avx2.And(Avx2.ShiftLeftLogical(result2_Low_L2, _shiftAmount), HIGH33_MASK_VEC_L);
			// Take the top shiftAmount of bits from the previous limb
			wideResult1Low = Avx2.Or(wideResult1Low, Avx2.ShiftRightLogical(Avx2.And(result2_Low_L1, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var wideResult1High = Avx2.And(Avx2.ShiftLeftLogical(result2_High_L2, _shiftAmount), HIGH33_MASK_VEC_L);
			// Take the top shiftAmount of bits from the previous limb
			wideResult1High = Avx2.Or(wideResult1High, Avx2.ShiftRightLogical(Avx2.And(result2_High_L1, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var result1Low = Avx2.PermuteVar8x32(wideResult1Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var result1High = Avx2.PermuteVar8x32(wideResult1High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			result[0] = Avx2.Or(result1Low, result1High);

			// i = 1

			// Calculate the lo end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var wideResult2Low = Avx2.And(Avx2.ShiftLeftLogical(result2_Low_L3, _shiftAmount), HIGH33_MASK_VEC_L);
			// Take the top shiftAmount of bits from the previous limb
			wideResult2Low = Avx2.Or(wideResult2Low, Avx2.ShiftRightLogical(Avx2.And(result2_Low_L2, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var wideResult2High = Avx2.And(Avx2.ShiftLeftLogical(result2_High_L3, _shiftAmount), HIGH33_MASK_VEC_L);
			// Take the top shiftAmount of bits from the previous limb
			wideResult2High = Avx2.Or(wideResult2High, Avx2.ShiftRightLogical(Avx2.And(result2_High_L2, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var result2Low = Avx2.PermuteVar8x32(wideResult2Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var result2High = Avx2.PermuteVar8x32(wideResult2High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			result[1] = Avx2.Or(result2Low, result2High);

			IncrementSplitsCount(64);
			IncrementConversionsCount(32);
		}

		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WarnIfAnyCarry(Vector256<uint> source, Vector256<int> mask, string description)
		{
			FP31VecMathHelper.WarnIfAnyCarry(source, mask, description);
		}

		#endregion

		#region Add and Subtract

		public bool TrySub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlagsVec)
		{
			FP31VecMathHelper.CheckReservedBitIsClear(right, "Negating right for Sub");
			//Negate(right, _negationResult);

			// i = 0
			var notVector1 = Avx2.Xor(right[0], XOR_BITS_VEC);
			var newValuesVector1 = Avx2.Add(notVector1, _ones);

			var negatedRight_L0 = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);
			var carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);

			// i = 1
			var notVector2 = Avx2.Xor(right[1], XOR_BITS_VEC);
			var newValuesVector2 = Avx2.Add(notVector2, carry);

			var negated_Right_L1 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);
			//FP31VecMathHelper.WarnIfAnyCarry(newValuesVector2, doneFlagsVec, "Negating for Subtraction.");

			IncrementNegationsCount(16);

			//Add(left, _negationResult, result);

			// i = 0
			var newValuesVector = Avx2.Add(left[0], negatedRight_L0);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// i = 1
			var sumVector = Avx2.Add(left[1], negated_Right_L1);
			newValuesVector2 = Avx2.Add(sumVector, carry);

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
			//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB); // The high 31 bits of sum becomes the new carry.

			IncrementAdditionsCount(16);

			var anyCarryWhileNegating = FP31VecMathHelper.AnyCarryFound(newValuesVector2, doneFlagsVec);

			return !anyCarryWhileNegating;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			//CheckReservedBitIsClear(right, "Negating right for Sub");
			//Negate(right, _negationResult);

			// i = 0
			var notVector1 = Avx2.Xor(right[0], XOR_BITS_VEC);
			var newValuesVector1 = Avx2.Add(notVector1, _ones);

			var negatedRight_L0 = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);
			var carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);

			// i = 1
			var notVector2 = Avx2.Xor(right[1], XOR_BITS_VEC);
			var newValuesVector2 = Avx2.Add(notVector2, carry);

			var negated_Right_L1 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);
			//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);

			IncrementNegationsCount(16);

			//Add(left, _negationResult, result);

			// i = 0
			var newValuesVector = Avx2.Add(left[0], negatedRight_L0);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// i = 1
			var sumVector = Avx2.Add(left[1], negated_Right_L1);
			newValuesVector2 = Avx2.Add(sumVector, carry);

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
			//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			IncrementAdditionsCount(16);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			// i = 0
			var newValuesVector = Avx2.Add(left[0], right[0]);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			var carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// i = 1
			var sumVector = Avx2.Add(left[1], right[1]);
			var newValuesVector2 = Avx2.Add(sumVector, carry);

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
			//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			// TODO: Check the value of _carry for overflow

			IncrementAdditionsCount(16);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IsGreaterOrEqThan(Vector256<uint>[] left, ref Vector256<int> right, ref Vector256<int> escapedFlagsVec)
		{
			// TODO: Is masking the Sign Bit really necessary.

			var sansSign = Avx2.And(left[^1], SIGN_BIT_MASK_VEC);
			escapedFlagsVec = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);

			IncrementComparisonsCount(8);
		}

		#endregion

		#region PERF

		[Conditional("PERF")]
		private void IncrementMultiplicationsCount(int amount)
		{
			MathOpCounts.NumberOfMultiplications += amount;
		}

		[Conditional("PERF")]
		private void IncrementAdditionsCount(int amount)
		{
			MathOpCounts.NumberOfAdditions += amount;
		}

		[Conditional("PERF")]
		private void IncrementNegationsCount(int amount)
		{
			MathOpCounts.NumberOfNegations += amount;
		}

		[Conditional("PERF")]
		private void IncrementConversionsCount(int amount)
		{
			MathOpCounts.NumberOfConversions += amount;
		}

		[Conditional("PERF")]
		private void IncrementSplitsCount(int amount)
		{
			MathOpCounts.NumberOfSplits += amount;
		}

		[Conditional("PERF")]
		private void IncrementComparisonsCount(int amount)
		{
			MathOpCounts.NumberOfComparisons += amount;
		}

		#endregion
	}
}
