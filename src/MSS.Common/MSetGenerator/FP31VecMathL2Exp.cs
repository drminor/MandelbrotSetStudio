using MSS.Common.MSetGenerator;
using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSS.Common
{
	public class FP31VecMathL2Exp : IFP31VecMath
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
		private static readonly Vector256<uint> ALL_BITS_SET_VEC = Vector256<uint>.AllBitsSet;

		private static readonly Vector256<uint> SHUFFLE_EXP_LOW_VEC = Vector256.Create(0u, 0u, 1u, 1u, 2u, 2u, 3u, 3u);
		private static readonly Vector256<uint> SHUFFLE_EXP_HIGH_VEC = Vector256.Create(4u, 4u, 5u, 5u, 6u, 6u, 7u, 7u);

		private static readonly Vector256<uint> SHUFFLE_PACK_LOW_VEC = Vector256.Create(0u, 2u, 4u, 6u, 0u, 0u, 0u, 0u);
		private static readonly Vector256<uint> SHUFFLE_PACK_HIGH_VEC = Vector256.Create(0u, 0u, 0u, 0u, 0u, 2u, 4u, 6u);

		//private PairOfVec<uint> _squareResult0;
		//private PairOfVec<ulong> _squareResult1;
		//private PairOfVec<ulong> _squareResult2;

		private Vector256<uint>[] _negationResult;

		private Vector256<uint> _ones;

		private Vector256<uint> _carry;
		private Vector256<ulong> _carryLong1;
		private Vector256<ulong> _carryLong2;

		//private Vector256<int> _signBitVecs;

		private byte _shiftAmount;
		private byte _inverseShiftAmount;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VecMathL2Exp(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			//_squareResult0 = new PairOfVec<uint>(LimbCount);
			//_squareResult1 = new PairOfVec<ulong>(LimbCount * 2);
			//_squareResult2 = new PairOfVec<ulong>(LimbCount * 2);

			_negationResult = new Vector256<uint>[LimbCount];

			_ones = Vector256.Create(1u);

			_carry = Vector256<uint>.Zero;
			_carryLong1 = Vector256<ulong>.Zero;
			_carryLong2 = Vector256<ulong>.Zero;

			//_signBitVecs = Vector256<int>.Zero;

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
				var notVector1 = Avx2.Xor(source[0], ALL_BITS_SET_VEC);
				var newValuesVector1 = Avx2.Add(notVector1, _ones);

				var limbValues = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);                // The low 31 bits of the sum is the result.
				_carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

				var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source[0].AsByte(), signBitVecs.AsByte())).AsUInt32();

				// Take the lower 4 values and set the low halves of each result
				result0_Low_L0 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L0 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// i = 1
				var notVector2 = Avx2.Xor(source[1], ALL_BITS_SET_VEC);
				var newValuesVector2 = Avx2.Add(notVector2, _carry);

				var limbValues2 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                  // The low 31 bits of the sum is the result.
				//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

				var cLimbValues2 = (Avx2.BlendVariable(limbValues2.AsByte(), source[1].AsByte(), signBitVecs.AsByte())).AsUInt32();

				// Take the lower 4 values and set the low halves of each result
				result0_Low_L1 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result0_High_L1 = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

				IncrementNegationsCount(16);
			}

			IncrementConversionsCount(16);

			//SquareInternal(_squareResult0, _squareResult1);

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.Clear();

			// j = 0, i = 0, r = 0/1
			var productVector11 = Avx2.Multiply(result0_Low_L0, result0_Low_L0);
			var productVector12 = Avx2.Multiply(result0_High_L0, result0_High_L0);

			var result1_Low_L0 = Avx2.And(productVector11, HIGH33_MASK_VEC_L);
			var result1_Low_L1 = Avx2.ShiftRightLogical(productVector11, EFFECTIVE_BITS_PER_LIMB);

			var result1_High_L0 = Avx2.And(productVector12, HIGH33_MASK_VEC_L);
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

			IncrementMultiplicationsCount(24);
			IncrementAdditionsCount(16);
			IncrementSplitsCount(16);

			//SumThePartials(_squareResult1, _squareResult2);

			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			// i = 0
			//var result2_Low_L0 = Avx2.And(result1_Low_L0, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			//var result2_High_L0 = Avx2.And(result1_High_L0, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(result1_Low_L0, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(result1_High_L0, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			// i = 1
			var partialSum1Low = Avx2.Add(result1_Low_L1, _carryLong1);
			var partialSum1High = Avx2.Add(result1_High_L1, _carryLong2);

			var result2_Low_L1 = Avx2.And(partialSum1Low, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			var result2_High_L1 = Avx2.And(partialSum1High, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			//_squareResult2.Lower[1] = Avx2.And(result1_Low_L1, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			//result2_High_L1 = Avx2.And(result1_High_L1, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			//_carryLong1 = Avx2.ShiftRightLogical(result1_Low_L1, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			//_carryLong2 = Avx2.ShiftRightLogical(result1_High_L1, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			_carryLong1 = Avx2.ShiftRightLogical(partialSum1Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(partialSum1High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.


			// i = 2
			var partialSum2Low = Avx2.Add(result1_Low_L2, _carryLong1);
			var partialSum2High = Avx2.Add(result1_High_L2, _carryLong2);

			var result2_Low_L2 = Avx2.And(partialSum2Low, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			var result2_High_L2 = Avx2.And(partialSum2High, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(partialSum2Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(partialSum2High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			// i = 3
			var partialSum3Low = Avx2.Add(result1_Low_L3, _carryLong1);
			var partialSum3High = Avx2.Add(result1_High_L3, _carryLong2);

			var result2_Low_L3 = Avx2.And(partialSum3Low, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			var result2_High_L3 = Avx2.And(partialSum3High, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			// TODO: Throw overflow if any bit:31 - 63 of withCarries is set.
			//_carryLong1 = Avx2.ShiftRightLogical(partialSum3Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			//_carryLong2 = Avx2.ShiftRightLogical(partialSum3High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			IncrementAdditionsCount(24);
			IncrementSplitsCount(28);

			//ShiftAndTrim(_squareResult2, result);

			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			//var sourceOffset = LimbCount;

			//for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)

			// i = 0
			//var destLimbPtr = 0;
			//var sourceLimbPtr = 2;
			//var prevSourceLimbPtr = 1;

			// Calculate the lo end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var sourceLimb1Low = result2_Low_L2; // _squareResult2.Lower[sourceLimbPtr];
			var wideResult1Low = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb1Low, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			var prevsourceLimb1Low = result2_Low_L1; //   _squareResult2.Lower[prevSourceLimbPtr];
			wideResult1Low = Avx2.Or(wideResult1Low, Avx2.ShiftRightLogical(Avx2.And(prevsourceLimb1Low, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var sourceLimb1High = result2_High_L2; //  _squareResult2.Upper[sourceLimbPtr];
			var wideResult1High = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb1High, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			var prevSourceLimb1High = result2_High_L1; // _squareResult2.Upper[prevSourceLimbPtr];
			wideResult1High = Avx2.Or(wideResult1High, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb1High, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var result1Low = Avx2.PermuteVar8x32(wideResult1Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var result1High = Avx2.PermuteVar8x32(wideResult1High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			result[0] = Avx2.Or(result1Low, result1High);

			// i = 1
			//var destLimbPtr = 1;
			//prevSourceLimbPtr = sourceLimbPtr;
			//var sourceLimbPtr = 3;

			// Calculate the lo end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var sourceLimb2Low = result2_Low_L3; // _squareResult2.Lower[sourceLimbPtr];
			var wideResult2Low = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb2Low, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			//var prevSourceLimb2Low = source.Lower[prevSourceLimbPtr];
			wideResult2Low = Avx2.Or(wideResult2Low, Avx2.ShiftRightLogical(Avx2.And(sourceLimb1Low, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var sourceLimb2High = result2_High_L3;  // _squareResult2.Upper[sourceLimbPtr];
			var wideResult2High = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb2High, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			//var prevSourceLimb2High = source.Upper[prevSourceLimbPtr];
			wideResult2High = Avx2.Or(wideResult2High, Avx2.ShiftRightLogical(Avx2.And(sourceLimb1High, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var result2Low = Avx2.PermuteVar8x32(wideResult2Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var result2High = Avx2.PermuteVar8x32(wideResult2High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			result[1] = Avx2.Or(result2Low, result2High);

			IncrementSplitsCount(32);
			IncrementConversionsCount(32);
		}

		//private void SquareInternal(PairOfVec<uint> source, PairOfVec<ulong> result)
		//{
		//	// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

		//	//result.Clear();

		//	// j = 0, i = 0, r = 0/1
		//	var productVector11 = Avx2.Multiply(source.Lower[0], source.Lower[0]);
		//	var productVector12 = Avx2.Multiply(source.Upper[0], source.Upper[0]);

		//	result.Lower[0] = Avx2.And(productVector11, HIGH33_MASK_VEC_L);
		//	result.Lower[1] = Avx2.ShiftRightLogical(productVector11, EFFECTIVE_BITS_PER_LIMB);

		//	result.Upper[0] = Avx2.And(productVector12, HIGH33_MASK_VEC_L);
		//	result.Upper[1] = Avx2.ShiftRightLogical(productVector12, EFFECTIVE_BITS_PER_LIMB);


		//	// j = 0, i = 1, r = 1/2
		//	var productVector21 = Avx2.ShiftLeftLogical(Avx2.Multiply(source.Lower[0], source.Lower[1]), 1);
		//	var productVector22 = Avx2.ShiftLeftLogical(Avx2.Multiply(source.Upper[0], source.Upper[1]), 1);

		//	result.Lower[1] = Avx2.Add(result.Lower[1], Avx2.And(productVector21, HIGH33_MASK_VEC_L));
		//	result.Lower[2] = Avx2.ShiftRightLogical(productVector21, EFFECTIVE_BITS_PER_LIMB);

		//	result.Upper[1] = Avx2.Add(result.Upper[1], Avx2.And(productVector22, HIGH33_MASK_VEC_L));
		//	result.Upper[2] = Avx2.ShiftRightLogical(productVector22, EFFECTIVE_BITS_PER_LIMB);


		//	// j = 1, i = 1, r = 2/3
		//	var productVector31 = Avx2.Multiply(source.Lower[1], source.Lower[1]);
		//	var productVector32 = Avx2.Multiply(source.Upper[1], source.Upper[1]);

		//	result.Lower[2] = Avx2.Add(result.Lower[2], Avx2.And(productVector31, HIGH33_MASK_VEC_L));
		//	result.Lower[3] = Avx2.ShiftRightLogical(productVector31, EFFECTIVE_BITS_PER_LIMB);

		//	result.Upper[2] = Avx2.Add(result.Upper[2], Avx2.And(productVector32, HIGH33_MASK_VEC_L));
		//	result.Upper[3] = Avx2.ShiftRightLogical(productVector32, EFFECTIVE_BITS_PER_LIMB);

		//	IncrementMultiplicationsCount(24);
		//	IncrementAdditionsCount(16);
		//	IncrementSplitsCount(16);
		//}

		#region Multiplication Post Processing

		//private void SumThePartials(PairOfVec<ulong> source, PairOfVec<ulong> result)
		//{
		//	// To be used after a multiply operation.
		//	// Process the carry portion of each result bin.
		//	// This will leave each result bin with a value <= 2^32 for the final digit.
		//	// If the MSL produces a carry, throw an exception.

		//	// i = 0
		//	result.Lower[0] = Avx2.And(source.Lower[0], HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
		//	result.Upper[0] = Avx2.And(source.Upper[0], HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

		//	_carryLong1 = Avx2.ShiftRightLogical(source.Lower[0], EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
		//	_carryLong2 = Avx2.ShiftRightLogical(source.Upper[0], EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

		//	//// Clear the source so that square internal will not have to make a separate call.
		//	//source.Lower[0] = Avx2.Xor(source.Lower[0], source.Lower[0]);
		//	//source.Upper[0] = Avx2.Xor(source.Upper[0], source.Upper[0]);

		//	// i = 1
		//	var partialSum1Low = Avx2.Add(source.Lower[1], _carryLong1);
		//	var partialSum1High = Avx2.Add(source.Upper[1], _carryLong2);

		//	result.Lower[1] = Avx2.And(partialSum1Low, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
		//	result.Upper[1] = Avx2.And(partialSum1High, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

		//	_carryLong1 = Avx2.ShiftRightLogical(partialSum1Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
		//	_carryLong2 = Avx2.ShiftRightLogical(partialSum1High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

		//	//// Clear the source so that square internal will not have to make a separate call.
		//	//source.Lower[1] = Avx2.Xor(source.Lower[1], source.Lower[1]);
		//	//source.Upper[1] = Avx2.Xor(source.Upper[1], source.Upper[1]);

		//	// i = 2
		//	var partialSum2Low = Avx2.Add(source.Lower[2], _carryLong1);
		//	var partialSum2High = Avx2.Add(source.Upper[2], _carryLong2);

		//	result.Lower[2] = Avx2.And(partialSum2Low, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
		//	result.Upper[2] = Avx2.And(partialSum2High, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

		//	_carryLong1 = Avx2.ShiftRightLogical(partialSum2Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
		//	_carryLong2 = Avx2.ShiftRightLogical(partialSum2High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

		//	//// Clear the source so that square internal will not have to make a separate call.
		//	//source.Lower[2] = Avx2.Xor(source.Lower[2], source.Lower[2]);
		//	//source.Upper[2] = Avx2.Xor(source.Upper[2], source.Upper[2]);

		//	// i = 3
		//	var partialSum3Low = Avx2.Add(source.Lower[3], _carryLong1);
		//	var partialSum3High = Avx2.Add(source.Upper[3], _carryLong2);

		//	result.Lower[3] = Avx2.And(partialSum3Low, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
		//	result.Upper[3] = Avx2.And(partialSum3High, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

		//	// TODO: Throw overflow if any bit:31 - 63 of withCarries is set.
		//	//_carryLong1 = Avx2.ShiftRightLogical(partialSum3Low, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
		//	//_carryLong2 = Avx2.ShiftRightLogical(partialSum3High, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

		//	//// Clear the source so that square internal will not have to make a separate call.
		//	//source.Lower[3] = Avx2.Xor(source.Lower[3], source.Lower[3]);
		//	//source.Upper[3] = Avx2.Xor(source.Upper[3], source.Upper[3]);

		//	IncrementAdditionsCount(24);
		//	IncrementSplitsCount(28);
		//}

		//private void ShiftAndTrim(PairOfVec<ulong> source, Vector256<uint>[] resultLimbs)
		//{
		//	//ValidateIsSplit(mantissa);

		//	// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
		//	// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
		//	// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

		//	// Check to see if any of these values are larger than the FP Format.
		//	//_ = CheckForOverflow(resultLimbs);

		//	var sourceOffset = LimbCount;

		//	//for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)

		//	// i = 0
		//	var destLimbPtr = 0;
		//	var sourceLimbPtr = destLimbPtr + sourceOffset;
		//	var prevSourceLimbPtr = sourceLimbPtr - 1;

		//	// Calculate the lo end
		//	// Take the bits from the source limb, discarding the top shiftAmount of bits.
		//	var sourceLimb1Low = source.Lower[sourceLimbPtr];
		//	var wideResult1Low = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb1Low, _shiftAmount), HIGH33_MASK_VEC_L);

		//	// Take the top shiftAmount of bits from the previous limb
		//	var prevsourceLimb1Low = source.Lower[prevSourceLimbPtr];
		//	wideResult1Low = Avx2.Or(wideResult1Low, Avx2.ShiftRightLogical(Avx2.And(prevsourceLimb1Low, HIGH33_MASK_VEC_L), _inverseShiftAmount));

		//	// Calculate the hi end
		//	// Take the bits from the source limb, discarding the top shiftAmount of bits.
		//	var sourceLimb1High = source.Upper[sourceLimbPtr];
		//	var wideResult1High = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb1High, _shiftAmount), HIGH33_MASK_VEC_L);

		//	// Take the top shiftAmount of bits from the previous limb
		//	var prevSourceLimb1High = source.Upper[prevSourceLimbPtr];
		//	wideResult1High = Avx2.Or(wideResult1High, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb1High, HIGH33_MASK_VEC_L), _inverseShiftAmount));

		//	var result1Low = Avx2.PermuteVar8x32(wideResult1Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
		//	var result1High = Avx2.PermuteVar8x32(wideResult1High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
		//	resultLimbs[destLimbPtr] = Avx2.Or(result1Low, result1High);

		//	// i = 1
		//	destLimbPtr++;
		//	//prevSourceLimbPtr = sourceLimbPtr;
		//	sourceLimbPtr++;

		//	// Calculate the lo end
		//	// Take the bits from the source limb, discarding the top shiftAmount of bits.
		//	var sourceLimb2Low = source.Lower[sourceLimbPtr];
		//	var wideResult2Low = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb2Low, _shiftAmount), HIGH33_MASK_VEC_L);

		//	// Take the top shiftAmount of bits from the previous limb
		//	//var prevSourceLimb2Low = source.Lower[prevSourceLimbPtr];
		//	wideResult2Low = Avx2.Or(wideResult2Low, Avx2.ShiftRightLogical(Avx2.And(sourceLimb1Low, HIGH33_MASK_VEC_L), _inverseShiftAmount));

		//	// Calculate the hi end
		//	// Take the bits from the source limb, discarding the top shiftAmount of bits.
		//	var sourceLimb2High = source.Upper[sourceLimbPtr];
		//	var wideResult2High = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb2High, _shiftAmount), HIGH33_MASK_VEC_L);

		//	// Take the top shiftAmount of bits from the previous limb
		//	//var prevSourceLimb2High = source.Upper[prevSourceLimbPtr];
		//	wideResult2High = Avx2.Or(wideResult2High, Avx2.ShiftRightLogical(Avx2.And(sourceLimb1High, HIGH33_MASK_VEC_L), _inverseShiftAmount));

		//	var result2Low = Avx2.PermuteVar8x32(wideResult2Low.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
		//	var result2High = Avx2.PermuteVar8x32(wideResult2High.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
		//	resultLimbs[destLimbPtr] = Avx2.Or(result2Low, result2High);

		//	IncrementSplitsCount(32);
		//	IncrementConversionsCount(32);
		//}

		#endregion

		#endregion

		#region Add and Subtract

		public void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			//CheckReservedBitIsClear(b, "Negating B");
			//Negate(right, _negationResult);

			// i = 0
			var notVector1 = Avx2.Xor(right[0], ALL_BITS_SET_VEC);
			var newValuesVector1 = Avx2.Add(notVector1, _ones);

			var negatedRight_L0 = Avx2.And(newValuesVector1, HIGH33_MASK_VEC); ;
			_carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);

			// i = 1
			var notVector2 = Avx2.Xor(right[1], ALL_BITS_SET_VEC);
			var newValuesVector2 = Avx2.Add(notVector2, _carry);

			var negated_Right_L1 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC); ;
			//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);

			// _carry may be non-zero if this source was the largest negative number
			// TODO: Handle negating the largest negative number.

			IncrementNegationsCount(16);

			//Add(left, _negationResult, result);

			// i = 0
			var newValuesVector = Avx2.Add(left[0], negatedRight_L0);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// i = 1
			var sumVector = Avx2.Add(left[1], negated_Right_L1);
			newValuesVector2 = Avx2.Add(sumVector, _carry);

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
																							//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

			IncrementAdditionsCount(16);
		}

		public void Add(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			// i = 0
			var newValuesVector = Avx2.Add(left[0], right[0]);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
			_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);      // The high 31 bits of sum becomes the new carry.

			// i = 1
			var sumVector = Avx2.Add(left[1], right[1]);
			var newValuesVector2 = Avx2.Add(sumVector, _carry);

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
			//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

			IncrementAdditionsCount(16);
		}

		#endregion

		#region Two Compliment Support

		//private void Negate(Vector256<uint>[] source, Vector256<uint>[] result)
		//{
		//	// i = 0
		//	var notVector1 = Avx2.Xor(source[0], ALL_BITS_SET_VEC);
		//	var newValuesVector1 = Avx2.Add(notVector1, _ones);

		//	result[0] = Avx2.And(newValuesVector1, HIGH33_MASK_VEC); ;
		//	_carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);

		//	// i = 1
		//	var notVector2 = Avx2.Xor(source[1], ALL_BITS_SET_VEC);
		//	var newValuesVector2 = Avx2.Add(notVector2, _carry);

		//	result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC); ;
		//	//_carry = Avx2.ShiftRightLogical(newValuesVector2, EFFECTIVE_BITS_PER_LIMB);

		//	// _carry may be non-zero if this source was the largest negative number
		//	// TODO: Handle negating the largest negative number.

		//	IncrementNegationsCount(16);
		//}

		//private void ConvertFrom2C(Vector256<uint>[] source, PairOfVec<uint> result)
		//{
		//	//CheckReservedBitIsClear(source, "ConvertFrom2C");

		//	var signBitFlags = GetSignBits(source, ref _signBitVecs);

		//	if (signBitFlags == -1)
		//	{
		//		// All positive values

		//		// TODO: Is Masking the high bits really required.

		//		// i = 0
		//		// Take the lower 4 values and set the low halves of each result
		//		result.Lower[0] = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

		//		// Take the higher 4 values and set the high halves of each result
		//		result.Upper[0] = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

		//		// i = 1
		//		// Take the lower 4 values and set the low halves of each result
		//		result.Lower[1] = Avx2.And(Avx2.PermuteVar8x32(source[1], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

		//		// Take the higher 4 values and set the high halves of each result
		//		result.Upper[1] = Avx2.And(Avx2.PermuteVar8x32(source[1], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
		//	}
		//	else
		//	{
		//		// Mixed Positive and Negative values

		//		// i = 0
		//		var notVector1 = Avx2.Xor(source[0], ALL_BITS_SET_VEC);
		//		var newValuesVector1 = Avx2.Add(notVector1, _ones);

		//		var limbValues = Avx2.And(newValuesVector1, HIGH33_MASK_VEC);                // The low 31 bits of the sum is the result.
		//		_carry = Avx2.ShiftRightLogical(newValuesVector1, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

		//		var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source[0].AsByte(), _signBitVecs.AsByte())).AsUInt32();

		//		// Take the lower 4 values and set the low halves of each result
		//		result.Lower[0] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

		//		// Take the higher 4 values and set the high halves of each result
		//		result.Upper[0] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

		//		// i = 1
		//		var notVector2 = Avx2.Xor(source[1], ALL_BITS_SET_VEC);
		//		var newValuesVector2 = Avx2.Add(notVector2, _carry);

		//		var limbValues2 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                  // The low 31 bits of the sum is the result.
		//																						//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

		//		var cLimbValues2 = (Avx2.BlendVariable(limbValues2.AsByte(), source[1].AsByte(), _signBitVecs.AsByte())).AsUInt32();

		//		// Take the lower 4 values and set the low halves of each result
		//		result.Lower[1] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

		//		// Take the higher 4 values and set the high halves of each result
		//		result.Upper[1] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

		//		// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

		//		IncrementNegationsCount(16);
		//	}

		//	IncrementConversionsCount(16);
		//}

		//private int GetSignBits(Vector256<uint>[] source, ref Vector256<int> signBitVecs)
		//{
		//	IncrementComparisonsCount(8);

		//	signBitVecs = Avx2.CompareEqual(Avx2.And(source[LimbCount - 1].AsInt32(), TEST_BIT_30_VEC), ZERO_VEC);
		//	return Avx2.MoveMask(signBitVecs.AsByte());
		//}

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
