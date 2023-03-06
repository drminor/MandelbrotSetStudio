using MSS.Common.MSetGenerator;
using MSS.Types;
using MSS.Types.APValues;
using System;
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

		private PairOfVec<uint> _squareResult0;
		private PairOfVec<ulong> _squareResult1;
		private PairOfVec<ulong> _squareResult2;

		private Vector256<uint>[] _negationResult;

		private Vector256<uint> _ones;

		private Vector256<uint> _carry;
		private Vector256<ulong> _carryLong1;
		private Vector256<ulong> _carryLong2;

		private Vector256<int> _signBitVecs;

		private byte _shiftAmount;
		private byte _inverseShiftAmount;

		private int _squareSourceStartIndex;
		private bool _skipSquareResultLow;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VecMathL2Exp(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			_squareResult0 = new PairOfVec<uint>(LimbCount);
			_squareResult1 = new PairOfVec<ulong>(LimbCount * 2);
			_squareResult2 = new PairOfVec<ulong>(LimbCount * 2);

			_negationResult = new Vector256<uint>[LimbCount];

			_ones = Vector256.Create(1u);

			_carry = Vector256<uint>.Zero;
			_carryLong1 = Vector256<ulong>.Zero;
			_carryLong2 = Vector256<ulong>.Zero;

			_signBitVecs = Vector256<int>.Zero;

			_shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;
			_inverseShiftAmount = (byte)(31 - _shiftAmount);

			(_squareSourceStartIndex, _skipSquareResultLow) = CalculateSqrOpParams(LimbCount);

			MathOpCounts = new MathOpCounts();
		}

		private (int sqrSrcStartIdx, bool skipSqrResLow) CalculateSqrOpParams(int limbCount)
		{
			// TODO: Check the CalculateSqrOpParams method 
			return limbCount switch
			{
				0 => (0, false),
				1 => (0, false),
				2 => (0, false),
				3 => (0, true),
				4 => (1, false),
				5 => (1, true),
				6 => (2, false),
				7 => (2, true),
				_ => (3, false),
			};
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; private set; }

		public MathOpCounts MathOpCounts { get; init; }

		public string Implementation => "FP31VecMath-L2Exp";

		#endregion

		#region Multiply and Square

		public void Square(Vector256<uint>[] a, Vector256<uint>[] result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			//CheckReservedBitIsClear(a, "Squaring");

			FP31VecMathHelper.ClearLimbSet(result);

			ConvertFrom2C(a, _squareResult0);
			//MathOpCounts.NumberOfConversions++;

			// Unoptimized
			SquareInternal(_squareResult0, _squareResult1);
			SumThePartials(_squareResult1, _squareResult2);
			ShiftAndTrim(_squareResult2, result);

			//// Optimized

			//SquareInternalOptimized(_squareResult0Lo, _squareResult1Lo);
			//SumThePartials(_squareResult1Lo, _squareResult2Lo);
			//var optimizedResult = new Vector256<uint>[LimbCount];
			//ShiftAndTrim(_squareResult2Lo, _squareResult2Hi, optimizedResult);

			//for (var i = 0; i < LimbCount; i++)
			//{
			//	var eqFlags = Avx2.CompareEqual(optimizedResult[i], result[i]);
			//	if (Avx2.MoveMask(eqFlags.AsByte()) != -1)
			//	{
			//		Debug.WriteLine("WARNING Optimized != NonOptimized.");
			//	}
			//}
		}

		private void SquareInternal(PairOfVec<uint> source, PairOfVec<ulong> result)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.Clear();

			// j = 0, i = 0, r = 0/1
			var productVector1 = Avx2.Multiply(source.Lower[0], source.Lower[0]);
			var productVector2 = Avx2.Multiply(source.Upper[0], source.Upper[0]);

			result.Lower[0] = Avx2.And(productVector1, HIGH33_MASK_VEC_L);
			result.Lower[1] = Avx2.ShiftRightLogical(productVector1, EFFECTIVE_BITS_PER_LIMB);

			result.Upper[0] = Avx2.And(productVector2, HIGH33_MASK_VEC_L);
			result.Upper[1] = Avx2.ShiftRightLogical(productVector2, EFFECTIVE_BITS_PER_LIMB);


			// j = 0, i = 1, r = 1/2
			var productVector12 = Avx2.ShiftLeftLogical(Avx2.Multiply(source.Lower[0], source.Lower[1]), 1);
			var productVector22 = Avx2.ShiftLeftLogical(Avx2.Multiply(source.Upper[0], source.Upper[1]), 1);

			result.Lower[1] = Avx2.Add(result.Lower[1], Avx2.And(productVector12, HIGH33_MASK_VEC_L));
			result.Lower[2] = Avx2.ShiftRightLogical(productVector12, EFFECTIVE_BITS_PER_LIMB);

			result.Upper[1] = Avx2.Add(result.Upper[1], Avx2.And(productVector22, HIGH33_MASK_VEC_L));
			result.Upper[2] = Avx2.ShiftRightLogical(productVector22, EFFECTIVE_BITS_PER_LIMB);


			// j = 1, i = 1, r = 2/3
			var productVector13 = Avx2.Multiply(source.Lower[1], source.Lower[1]);
			var productVector23 = Avx2.Multiply(source.Upper[1], source.Upper[1]);

			result.Lower[2] = Avx2.Add(result.Lower[2], Avx2.And(productVector13, HIGH33_MASK_VEC_L));
			result.Lower[3] = Avx2.ShiftRightLogical(productVector13, EFFECTIVE_BITS_PER_LIMB);

			result.Upper[2] = Avx2.Add(result.Upper[2], Avx2.And(productVector23, HIGH33_MASK_VEC_L));
			result.Upper[3] = Avx2.ShiftRightLogical(productVector23, EFFECTIVE_BITS_PER_LIMB);

			IncrementMultiplicationsCount(24);
			IncrementAdditionsCount(16);
			IncrementSplitsCount(16);
		}

		private void SquareInternalOptimized(Vector256<uint>[] source, Vector256<ulong>[] result)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.ClearManatissMems();

			for (int j = 0; j < source.Length; j++)
			{
				for (int i = j; i < source.Length; i++)
				{
					var resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

					if (resultPtr < _squareSourceStartIndex)
					{
						result[resultPtr] = Vector256<ulong>.Zero;
						result[resultPtr + 1] = Vector256<ulong>.Zero;
					}
					else
					{
						var productVector = Avx2.Multiply(source[j], source[i]);
						IncrementMultiplicationsCount(4);

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						// 0/1; 1/2; 2/3

						if (_skipSquareResultLow & resultPtr == _squareSourceStartIndex)
						{
							result[resultPtr] = Vector256<ulong>.Zero;
							result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
						}
						else
						{
							result[resultPtr] = Avx2.Add(result[resultPtr], Avx2.And(productVector, HIGH33_MASK_VEC_L));
							result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
						}

						//MathOpCounts.NumberOfSplits++;

					}
				}
			}
		}

		#endregion

		#region Multiplication Post Processing

		private void SumThePartials(PairOfVec<ulong> source, PairOfVec<ulong> result)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			// i = 0
			result.Lower[0] = Avx2.And(source.Lower[0], HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			result.Upper[0] = Avx2.And(source.Upper[0], HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(source.Lower[0], EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(source.Upper[0], EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			// Clear the source so that square internal will not have to make a separate call.
			source.Lower[0] = Avx2.Xor(source.Lower[0], source.Lower[0]);
			source.Upper[0] = Avx2.Xor(source.Upper[0], source.Upper[0]);

			// i = 1
			var partialSum1 = Avx2.Add(source.Lower[1], _carryLong1);
			var partialSum2 = Avx2.Add(source.Upper[1], _carryLong2);

			result.Lower[1] = Avx2.And(partialSum1, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			result.Upper[1] = Avx2.And(partialSum2, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(partialSum1, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(partialSum2, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			// Clear the source so that square internal will not have to make a separate call.
			source.Lower[1] = Avx2.Xor(source.Lower[1], source.Lower[1]);
			source.Upper[1] = Avx2.Xor(source.Upper[1], source.Upper[1]);

			// i = 2
			var partialSum12 = Avx2.Add(source.Lower[2], _carryLong1);
			var partialSum22 = Avx2.Add(source.Upper[2], _carryLong2);

			result.Lower[2] = Avx2.And(partialSum12, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			result.Upper[2] = Avx2.And(partialSum22, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			_carryLong1 = Avx2.ShiftRightLogical(partialSum12, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			_carryLong2 = Avx2.ShiftRightLogical(partialSum22, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			// Clear the source so that square internal will not have to make a separate call.
			source.Lower[2] = Avx2.Xor(source.Lower[2], source.Lower[2]);
			source.Upper[2] = Avx2.Xor(source.Upper[2], source.Upper[2]);

			// i = 3
			var partialSum13 = Avx2.Add(source.Lower[3], _carryLong1);
			var partialSum23 = Avx2.Add(source.Upper[3], _carryLong2);

			result.Lower[3] = Avx2.And(partialSum13, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
			result.Upper[3] = Avx2.And(partialSum23, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

			// TODO: Throw overflow if any bit:31 - 63 of withCarries is set.
			//_carryLong1 = Avx2.ShiftRightLogical(partialSum13, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
			//_carryLong2 = Avx2.ShiftRightLogical(partialSum23, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

			// Clear the source so that square internal will not have to make a separate call.
			source.Lower[3] = Avx2.Xor(source.Lower[3], source.Lower[3]);
			source.Upper[3] = Avx2.Xor(source.Upper[3], source.Upper[3]);
		}

		private void ShiftAndTrim(PairOfVec<ulong> source, Vector256<uint>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var sourceOffset = LimbCount;

			//for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)

			// i = 0
			var destLimbPtr = 0;
			var sourceLimbPtr = destLimbPtr + sourceOffset;
			var prevSourceLimbPtr = sourceLimbPtr - 1;

			// Calculate the lo end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var sourceLimb1 = source.Lower[sourceLimbPtr];
			var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb1, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			var prevSourceLimb1 = source.Lower[prevSourceLimbPtr];
			wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb1, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			var sourceLimb2 = source.Upper[sourceLimbPtr];
			var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb2, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			var prevSourceLimb2 = source.Upper[prevSourceLimbPtr];
			wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb2, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			resultLimbs[destLimbPtr] = Avx2.Or(low128, high128);

			// i = 1
			destLimbPtr++;
			//sourceLimbPtr = prevSourceLimbPtr;
			prevSourceLimbPtr++;

			// Calculate the lo end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			//var sourceLimb1 = source.Lower[sourceLimbPtr];
			var wideResultLow2 = Avx2.And(Avx2.ShiftLeftLogical(prevSourceLimb1, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			var prevSourceLimb12 = source.Lower[prevSourceLimbPtr];
			wideResultLow2 = Avx2.Or(wideResultLow2, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb12, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end
			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			//sourceLimb2 = source.Upper[sourceLimbPtr];
			var wideResultHigh2 = Avx2.And(Avx2.ShiftLeftLogical(prevSourceLimb2, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			var prevSourceLimb22 = source.Upper[prevSourceLimbPtr];
			wideResultHigh2 = Avx2.Or(wideResultHigh2, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb22, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			var low1282 = Avx2.PermuteVar8x32(wideResultLow2.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
			var high1282 = Avx2.PermuteVar8x32(wideResultHigh2.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
			resultLimbs[destLimbPtr] = Avx2.Or(low1282, high1282);

			//MathOpCounts.NumberOfSplits += 4;

			IncrementSplitsCount(32);
			IncrementConversionsCount(32);
		}

		#endregion

		#region Add and Subtract

		public void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result)
		{
			//CheckReservedBitIsClear(b, "Negating B");
			Negate(right, _negationResult);
			Add(left, _negationResult, result);
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

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);                         // The low 31 bits of the sum is the result.
																							 //_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

			IncrementAdditionsCount(16);
		}

		//public void AddThenSquare(Vector256<uint>[] a, Vector256<uint>[] b, Vector256<uint>[] c)
		//{
		//	Add(a, b, _additionResult);
		//	Square(_additionResult, c);
		//}

		#endregion

		#region Two Compliment Support

		private void Negate(Vector256<uint>[] source, Vector256<uint>[] result)
		{
			// i = 0
			var notVector = Avx2.Xor(source[0], ALL_BITS_SET_VEC);
			var newValuesVector = Avx2.Add(notVector, _ones);

			result[0] = Avx2.And(newValuesVector, HIGH33_MASK_VEC); ;
			_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);

			// i = 1
			var notVector2 = Avx2.Xor(source[1], ALL_BITS_SET_VEC);
			var newValuesVector2 = Avx2.Add(notVector2, _carry);

			result[1] = Avx2.And(newValuesVector2, HIGH33_MASK_VEC); ;
			//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);

			// _carry may be non-zero if this source was the largest negative number
			// TODO: Handle negating the largest negative number.

			IncrementNegationsCount(16);
		}

		private void ConvertFrom2C(Vector256<uint>[] source, PairOfVec<uint> result)
		{
			//CheckReservedBitIsClear(source, "ConvertFrom2C");

			var signBitFlags = GetSignBits(source, ref _signBitVecs);

			if (signBitFlags == -1)
			{
				// All positive values

				// TODO: Is Masking the high bits really required.

				// i = 0
				// Take the lower 4 values and set the low halves of each result
				result.Lower[0] = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result.Upper[0] = Avx2.And(Avx2.PermuteVar8x32(source[0], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// i = 1
				// Take the lower 4 values and set the low halves of each result
				result.Lower[1] = Avx2.And(Avx2.PermuteVar8x32(source[1], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result.Upper[1] = Avx2.And(Avx2.PermuteVar8x32(source[1], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
			}
			else
			{
				// Mixed Positive and Negative values
				var notVector = Avx2.Xor(source[0], ALL_BITS_SET_VEC);
				var newValuesVector = Avx2.Add(notVector, _ones);

				var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);				// The low 31 bits of the sum is the result.
				_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

				var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source[0].AsByte(), _signBitVecs.AsByte())).AsUInt32();

				// Take the lower 4 values and set the low halves of each result
				result.Lower[0] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result.Upper[0] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				var notVector2 = Avx2.Xor(source[1], ALL_BITS_SET_VEC);
				var newValuesVector2 = Avx2.Add(notVector2, _carry);

				var limbValues2 = Avx2.And(newValuesVector2, HIGH33_MASK_VEC);					// The low 31 bits of the sum is the result.
				//_carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

				var cLimbValues2 = (Avx2.BlendVariable(limbValues2.AsByte(), source[1].AsByte(), _signBitVecs.AsByte())).AsUInt32();

				// Take the lower 4 values and set the low halves of each result
				result.Lower[1] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

				// Take the higher 4 values and set the high halves of each result
				result.Upper[1] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues2, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				// TODO: Check the value of _carry for overflow for those _signBitVec values that are zero.

				IncrementNegationsCount(16);
			}

			IncrementConversionsCount(16);
		}

		private int GetSignBits(Vector256<uint>[] source, ref Vector256<int> signBitVecs)
		{
			IncrementComparisonsCount(8);

			//var msl = source[LimbCount - 1];

			//var left = Avx2.And(msl.AsInt32(), TEST_BIT_30_VEC);
			//signBitVecs = Avx2.CompareEqual(left, ZERO_VEC); // dst[i+31:i] := ( a[i+31:i] == b[i+31:i] ) ? 0xFFFFFFFF : 0
			//var result = Avx2.MoveMask(signBitVecs.AsByte());

			//return result;

			signBitVecs = Avx2.CompareEqual(Avx2.And(source[LimbCount - 1].AsInt32(), TEST_BIT_30_VEC), ZERO_VEC);
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
