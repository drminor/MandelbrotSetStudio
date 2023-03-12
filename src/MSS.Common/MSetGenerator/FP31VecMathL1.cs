using MSS.Types;
using MSS.Types.APValues;
using System;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSS.Common
{
	public class FP31VecMathL1 : IFP31VecMath
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

		//private Vector256<uint> _carry;
		//private Vector256<ulong> _carryLong1;
		//private Vector256<ulong> _carryLong2;

		private byte _shiftAmount;
		private byte _inverseShiftAmount;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VecMathL1(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			Debug.Assert(LimbCount == 1, $"Attempting to construct a FP31VecMathL2 with a ApFixedPointFormat having a limb count of {LimbCount}.");

			_ones = Vector256.Create(1u);

			//_carry = Vector256<uint>.Zero;
			//_carryLong1 = Vector256<ulong>.Zero;
			//_carryLong2 = Vector256<ulong>.Zero;

			_shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;
			_inverseShiftAmount = (byte)(31 - _shiftAmount);

			MathOpCounts = new MathOpCounts();
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; private set; }

		public MathOpCounts MathOpCounts { get; init; }

		public string Implementation => "FP31VecMath-L1";

		#endregion

		#region Square

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

			if (signBitFlags == -1)
			{
				// All positive values

				// TODO: Is Masking the high bits really required.

				// i = 0
				// Take the lower 4 values and set the low halves of each result
				result0_Low_L0 = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L0 = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
			}
			else
			{
				// Mixed Positive and Negative values

				// i = 0
				var notVector1 = Avx2.Xor(source[0], XOR_BITS_VEC);
				var newValuesVector1 = Avx2.Add(notVector1, _ones);

				var limbValues = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);                // The low 31 bits of the sum is the result.
				var carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

				var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source[0].AsByte(), signBitVecs.AsByte())).AsUInt32();

				// Take the lower 4 values and set the low halves of each result
				result0_Low_L0 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L0 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

				var cIsZeroFlags = Avx2.CompareEqual(carry, Vector256<uint>.Zero);
				cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<uint>.AllBitsSet, signBitVecs.AsUInt32());

				var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());
				if (isZeroComp != -1)
				{
					Debug.WriteLine("Found a carry.");
				}

				IncrementNegationsCount(8);
			}

			IncrementConversionsCount(16);

			//SquareInternal(_squareResult0, _squareResult1);
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			// j = 0, i = 0, r = 0/1
			var productVector11 = Avx2.Multiply(result0_Low_L0, result0_Low_L0);
			var productVector12 = Avx2.Multiply(result0_High_L0, result0_High_L0);

			//var result1_Low_L0 = Avx2.And(productVector11, HIGH33_MASK_VEC_L);
			//var result1_Low_L1 = Avx2.ShiftRightLogical(productVector11, EFFECTIVE_BITS_PER_LIMB);

			//var result1_High_L0 = Avx2.And(productVector12, HIGH33_MASK_VEC_L);
			//var result1_High_L1 = Avx2.ShiftRightLogical(productVector12, EFFECTIVE_BITS_PER_LIMB);

			IncrementMultiplicationsCount(16);
			//IncrementSplitsCount(16);

			//ShiftAndTrim(_squareResult1, result);
			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:55, then the mantissa we are given will have the format of 16:110, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:55.

			// i = 0
			// Calculate the lo end

			//// Take the bits from the source limb, discarding the top shiftAmount of bits.
			//var wideResult1Low = Avx2.And(Avx2.ShiftLeftLogical(result1_Low_L1, _shiftAmount), HIGH33_MASK_VEC_L);
			//// Take the top shiftAmount of bits from the previous limb
			//wideResult1Low = Avx2.Or(wideResult1Low, Avx2.ShiftRightLogical(Avx2.And(result1_Low_L0, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var wideResult1Low = Avx2.And(Avx2.ShiftRightLogical(productVector11, _inverseShiftAmount), HIGH33_MASK_VEC_L);


			// Calculate the hi end

			//// Take the bits from the source limb, discarding the top shiftAmount of bits.
			//var wideResult1High = Avx2.And(Avx2.ShiftLeftLogical(result1_High_L1, _shiftAmount), HIGH33_MASK_VEC_L);
			//// Take the top shiftAmount of bits from the previous limb
			//wideResult1High = Avx2.Or(wideResult1High, Avx2.ShiftRightLogical(Avx2.And(result1_High_L0, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var wideResult1High = Avx2.And(Avx2.ShiftRightLogical(productVector12, _inverseShiftAmount), HIGH33_MASK_VEC_L);


			var result1Low = Avx2.PermuteVar8x32(wideResult1Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var result1High = Avx2.PermuteVar8x32(wideResult1High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			result[0] = Avx2.Or(result1Low, result1High);

			IncrementSplitsCount(16);
			IncrementConversionsCount(16);
		}

		#endregion

		#region Add and Subtract

		public bool TrySub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlagsVec)
		{
			//CheckReservedBitIsClear(b, "Negating B");
			//Negate(right, _negationResult);

			// i = 0
			var notVector1 = Avx2.Xor(right[0], XOR_BITS_VEC);
			var newValuesVector1 = Avx2.Add(notVector1, _ones);

			var negatedRight_L0 = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);
			var carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);

			// _carry may be non-zero if this source was the largest negative number
			// TODO: Handle negating the largest negative number.

			var cIsZeroFlags = Avx2.CompareEqual(carry, Vector256<uint>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<uint>.AllBitsSet, doneFlagsVec.AsUInt32());

			var cIsZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			if (cIsZeroComp != -1)
			{
				//Debug.WriteLine("Got a carry when negating right for Sub.");
				//return false;
			}

			IncrementNegationsCount(8);

			//Add(left, _negationResult, result);

			// i = 0
			var newValuesVector = Avx2.Add(left[0], negatedRight_L0);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                     
			//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);

			// TODO: Check the value of _carry for overflow after addition.

			IncrementAdditionsCount(8);

			if (cIsZeroComp != -1)
			{
				return false;
			}

			return true;
		}

		public void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			//CheckReservedBitIsClear(b, "Negating B");
			//Negate(right, _negationResult);

			// i = 0
			var notVector1 = Avx2.Xor(right[0], XOR_BITS_VEC);
			var newValuesVector1 = Avx2.Add(notVector1, _ones);

			var negatedRight_L0 = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);
			//_carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);

			// _carry may be non-zero if this source was the largest negative number
			// TODO: Handle negating the largest negative number.

			IncrementNegationsCount(8);

			//Add(left, _negationResult, result);

			// i = 0
			var newValuesVector = Avx2.Add(left[0], negatedRight_L0);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

			IncrementAdditionsCount(8);
		}

		public void Add(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			// i = 0
			var newValuesVector = Avx2.Add(left[0], right[0]);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

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
