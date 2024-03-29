﻿using MSS.Types;
using MSS.Types.APValues;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSS.Common
{
	public class FP31VecMath : IFP31VecMath
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

		private PairOfVec<uint> _squareResult0;
		private PairOfVec<ulong> _squareResult1;
		private PairOfVec<ulong> _squareResult2;

		private Vector256<uint>[] _negationResult;

		private Vector256<uint> _ones;

		private byte _shiftAmount;
		private byte _inverseShiftAmount;

		#endregion

		#region Constructor

		public FP31VecMath(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			_squareResult0 = new PairOfVec<uint>(LimbCount);
			_squareResult1 = new PairOfVec<ulong>(LimbCount * 2);
			_squareResult2 = new PairOfVec<ulong>(LimbCount * 2);

			_negationResult = new Vector256<uint>[LimbCount];

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

		public string Implementation => "FP31VecMath-Plain";

		#endregion

		#region Multiply and Square

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Square(Vector256<uint>[] a, Vector256<uint>[] result, ref Vector256<int> doneFlags)
		{
			// Our multiplication routines don't support 2's compliment,
			// So we must convert back from two's complement to standard.

			// The result of squaring is always positive, so we don't have to
			// convert back to two's compliment afterwards.

			//FP31VecMathHelper.CheckReservedBitIsClear(a, "Squaring");
			FP31VecMathHelper.ClearLimbSet(result);

			ConvertFrom2C(a, _squareResult0, ref doneFlags);
			SquareInternal(_squareResult0, _squareResult1);
			SumThePartials(_squareResult1, _squareResult2, ref doneFlags);
			ShiftAndTrim(_squareResult2, result);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SquareInternal(PairOfVec<uint> source, PairOfVec<ulong> result)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			var lCntr = 0;

			for (int j = 0; j < source.Lower.Length; j++)
			{
				for (int i = j; i < source.Lower.Length; i++)
				{
					var resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

					var productVector1 = Avx2.Multiply(source.Lower[j], source.Lower[i]);
					var productVector2 = Avx2.Multiply(source.Upper[j], source.Upper[i]);

					if (i > j)
					{
						//product *= 2;
						productVector1 = Avx2.ShiftLeftLogical(productVector1, 1);
						productVector2 = Avx2.ShiftLeftLogical(productVector2, 1);
					}

					// 0/1; 1/2; 2/3

					result.Lower[resultPtr] = Avx2.Add(result.Lower[resultPtr], Avx2.And(productVector1, HIGH33_MASK_VEC_L));
					result.Lower[resultPtr + 1] = Avx2.Add(result.Lower[resultPtr + 1], Avx2.ShiftRightLogical(productVector1, EFFECTIVE_BITS_PER_LIMB));

					result.Upper[resultPtr] = Avx2.Add(result.Upper[resultPtr], Avx2.And(productVector2, HIGH33_MASK_VEC_L));
					result.Upper[resultPtr + 1] = Avx2.Add(result.Upper[resultPtr + 1], Avx2.ShiftRightLogical(productVector2, EFFECTIVE_BITS_PER_LIMB));

					lCntr++;
				}
			}

			IncrementMultiplicationsCount(lCntr * 2);
			IncrementAdditionsCount(lCntr * 2);
			IncrementSplitsCount(lCntr * 2);
		}

		#endregion

		#region Multiplication Post Processing

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SumThePartials(PairOfVec<ulong> source, PairOfVec<ulong> result, ref Vector256<int> doneFlags)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			result.Lower[0] = Avx2.And(source.Lower[0], HIGH33_MASK_VEC_L);					// The low 31 bits of the sum is the result.
			result.Upper[0] = Avx2.And(source.Upper[0], HIGH33_MASK_VEC_L);					// The low 31 bits of the sum is the result.

			var carry1 = Avx2.ShiftRightLogical(source.Lower[0], EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
			var carry2 = Avx2.ShiftRightLogical(source.Upper[0], EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

			source.Lower[0] = Avx2.Xor(source.Lower[0], source.Lower[0]);
			source.Upper[0] = Avx2.Xor(source.Upper[0], source.Upper[0]);

			for (int limbPtr = 1; limbPtr < source.Lower.Length; limbPtr++)
			{
				var withCarries1 = Avx2.Add(source.Lower[limbPtr], carry1);
				var withCarries2 = Avx2.Add(source.Upper[limbPtr], carry2);

				result.Lower[limbPtr] = Avx2.And(withCarries1, HIGH33_MASK_VEC_L);			// The low 31 bits of the sum is the result.
				result.Upper[limbPtr] = Avx2.And(withCarries2, HIGH33_MASK_VEC_L);			// The low 31 bits of the sum is the result.

				carry1 = Avx2.ShiftRightLogical(withCarries1, EFFECTIVE_BITS_PER_LIMB);		// The high 31 bits of sum becomes the new carry.
				carry2 = Avx2.ShiftRightLogical(withCarries2, EFFECTIVE_BITS_PER_LIMB);		// The high 31 bits of sum becomes the new carry.

				// Clear the source so that square internal will not have to make a separate call.
				source.Lower[limbPtr] = Avx2.Xor(source.Lower[limbPtr], source.Lower[limbPtr]);
				source.Upper[limbPtr] = Avx2.Xor(source.Upper[limbPtr], source.Upper[limbPtr]);
			}

			FP31VecMathHelper.WarnIfAnyNotZero(carry1, doneFlags, $"SumThePartialsLower");
			FP31VecMathHelper.WarnIfAnyNotZero(carry1, doneFlags, $"SumThePartialsUpper");

			IncrementAdditionsCount((LimbCount - 1) * 16);
			IncrementSplitsCount(LimbCount * 16);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ShiftAndTrim(PairOfVec<ulong> source, Vector256<uint>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var sourceIndex = Math.Max(source.Lower.Length - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)
			{
				// Calculate the lo end

				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				var sourceLimb = source.Lower[limbPtr + sourceIndex];
				var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb, _shiftAmount), HIGH33_MASK_VEC_L);

				// Take the top shiftAmount of bits from the previous limb
				var prevSourceLimb = source.Lower[limbPtr + sourceIndex - 1];
				wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb, HIGH33_MASK_VEC_L), _inverseShiftAmount));

				// Calculate the hi end

				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				sourceLimb = source.Upper[limbPtr + sourceIndex];
				var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(sourceLimb, _shiftAmount), HIGH33_MASK_VEC_L);

				// Take the top shiftAmount of bits from the previous limb
				prevSourceLimb = source.Upper[limbPtr + sourceIndex - 1];
				wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSourceLimb, HIGH33_MASK_VEC_L), _inverseShiftAmount));

				var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
				var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);
				resultLimbs[limbPtr] = Avx2.Or(low128, high128);
			}

			IncrementSplitsCount(LimbCount * 32);
			IncrementConversionsCount(LimbCount * 32);
		}

		#endregion

		#region Add and Subtract

		public bool TrySub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlagsVec)
		{
			//var negateFailed = TryNegate(right, _negationResult, ref doneFlagsVec);
			Negate(right, _negationResult);
			var addSucceeded = TryAdd(left, _negationResult, result, ref doneFlagsVec);

			return addSucceeded;
		}

		public bool TryAdd(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlagsVec)
		{
			var carry = Vector256<uint>.Zero;

			for (int limbPtr = 0; limbPtr < left.Length; limbPtr++)
			{
				var sumVector = Avx2.Add(left[limbPtr], right[limbPtr]);
				var newValuesVector = Avx2.Add(sumVector, carry);

				result[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);              // The low 31 bits of the sum is the result.
				carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
			}

			IncrementAdditionsCount(LimbCount * 8);

			var anyCarryFound = FP31VecMathHelper.AnyNotZero(carry, doneFlagsVec);
			return !anyCarryFound;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlags)
		{
			Negate(right, _negationResult);
			Add(left, _negationResult, result, ref doneFlags);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlags)
		{
			var newValuesVector = Vector256<uint>.Zero;
			var carry = Vector256<uint>.Zero;

			for (int limbPtr = 0; limbPtr < left.Length; limbPtr++)
			{
				var sumVector = Avx2.Add(left[limbPtr], right[limbPtr]);
				newValuesVector = Avx2.Add(sumVector, carry);

				result[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);              // The low 31 bits of the sum is the result.
				carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
			}

			FP31VecMathHelper.WarnIfAnyCarryForAddSub(newValuesVector, doneFlags, "Add");
			//FP31VecMathHelper.WarnIfAnyNotZero(carry, doneFlags, "Add");

			IncrementAdditionsCount(LimbCount * 8);
			IncrementSplitsCount(LimbCount * 8);
		}

		#endregion

		#region Two Compliment Support

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Negate(Vector256<uint>[] source, Vector256<uint>[] result)
		{
			// NOTE: As long as the Reserved bit is clear, the only possibility of getting an overflow is when all limbs are zero and this overflow must be ignored.
			FP31VecMathHelper.CheckReservedBitIsClear(source, "Negating");

			var carry = _ones;

			for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
			{
				var notVector = Avx2.Xor(source[limbPtr], XOR_BITS_VEC);
				var newValuesVector = Avx2.Add(notVector, carry);

				result[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);
				carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
			}

			// Any carry here must be ignored.
			//WarnIfAnyNotZero(carry, doneFlags, "Negate");

			IncrementNegationsCount(LimbCount * 8);
			IncrementSplitsCount(LimbCount * 8);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ConvertFrom2C(Vector256<uint>[] source, PairOfVec<uint> result, ref Vector256<int> doneFlags)
		{
			//CheckReservedBitIsClear(source, "ConvertFrom2C");
			//var signBitFlags = GetSignBits(source, ref _signBitVecs);

			var signBitVecs = Avx2.CompareEqual(Avx2.And(source[LimbCount - 1].AsInt32(), TEST_BIT_30_VEC), ZERO_VEC);
			var signBitFlags = Avx2.MoveMask(signBitVecs.AsByte());

			if (signBitFlags == -1)
			{
				// All positive values
				for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
				{
					// TODO: Is Masking the high bits really required.
					// Take the lower 4 values and set the low halves of each result
					result.Lower[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(source[limbPtr], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

					// Take the higher 4 values and set the high halves of each result
					result.Upper[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(source[limbPtr], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
				}
			}
			else
			{
				IncrementNegationsCount(1);

				// Mixed Positive and Negative values
				var carry = _ones;

				for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
				{
					var notVector = Avx2.Xor(source[limbPtr], XOR_BITS_VEC);
					var newValuesVector = Avx2.Add(notVector, carry);
					//MathOpCounts.NumberOfAdditions += 2;

					var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);				// The low 31 bits of the sum is the result.
					carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

					//MathOpCounts.NumberOfSplits++;

					var cLimbValues = Avx2.BlendVariable(limbValues, source[limbPtr], signBitVecs.AsUInt32());

					// Take the lower 4 values and set the low halves of each result
					result.Lower[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

					// Take the higher 4 values and set the high halves of each result
					result.Upper[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
				}

				FP31VecMathHelper.WarnIfAnyNotZero(carry, doneFlags, "ConvertFrom2C");

				//IncrementNegationsCount(LimbCount * 16);
			}

			IncrementConversionsCount(LimbCount * 16);
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
		public void IsGreaterOrEqThan(Vector256<uint>[] left, Vector256<int> right, ref Vector256<int> escapedFlagsVec)
		{
			// TODO: Is masking the Sign Bit really necessary.
			var sansSign = Avx2.And(left[^1], SIGN_BIT_MASK_VEC);
			escapedFlagsVec = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);

			IncrementComparisonsCount(8);
		}

		#endregion

		#region Diagnostics

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

		#region Not Used

		//private int _squareSourceStartIndex;
		//private bool _skipSquareResultLow;

		// Copied from the Constructor
		//(_squareSourceStartIndex, _skipSquareResultLow) = CalculateSqrOpParams(LimbCount);

		//private (int sqrSrcStartIdx, bool skipSqrResLow) CalculateSqrOpParams(int limbCount)
		//{
		//	// TODO: Check the CalculateSqrOpParams method 
		//	return limbCount switch
		//	{
		//		0 => (0, false),
		//		1 => (0, false),
		//		2 => (0, false),
		//		3 => (0, true),
		//		4 => (1, false),
		//		5 => (1, true),
		//		6 => (2, false),
		//		7 => (2, true),
		//		_ => (3, false),
		//	};
		//}


		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public void SquareOptimized(Vector256<uint>[] a, Vector256<uint>[] result, ref Vector256<int> doneFlags)
		//{
		//	// Our multiplication routines don't support 2's compliment,
		//	// So we must convert back from two's complement to standard.

		//	// The result of squaring is always positive, so we don't have to convert
		//	// back to two's compliment afterwards.

		//	//FP31VecMathHelper.CheckReservedBitIsClear(a, "Squaring");
		//	FP31VecMathHelper.ClearLimbSet(result);

		//	ConvertFrom2C(a, _squareResult0, ref doneFlags);

		//	// Unoptimized
		//	SquareInternal(_squareResult0, _squareResult1);
		//	SumThePartials(_squareResult1, _squareResult2, ref doneFlags);
		//	ShiftAndTrim(_squareResult2, result);

		//	//// Optimized
		//	//SquareInternalOptimized(_squareResult0Lo, _squareResult1Lo);
		//	//SumThePartials(_squareResult1Lo, _squareResult2Lo);
		//	//var optimizedResult = new Vector256<uint>[LimbCount];
		//	//ShiftAndTrim(_squareResult2Lo, _squareResult2Hi, optimizedResult);

		//	//for (var i = 0; i < LimbCount; i++)
		//	//{
		//	//	var eqFlags = Avx2.CompareEqual(optimizedResult[i], result[i]);
		//	//	if (Avx2.MoveMask(eqFlags.AsByte()) != -1)
		//	//	{
		//	//		Debug.WriteLine("WARNING Optimized != NonOptimized.");
		//	//	}
		//	//}
		//}

		//private void SquareInternalOptimized(Vector256<uint>[] source, Vector256<ulong>[] result)
		//{
		//	// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

		//	//result.ClearManatissMems();

		//	for (int j = 0; j < source.Length; j++)
		//	{
		//		for (int i = j; i < source.Length; i++)
		//		{
		//			var resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

		//			if (resultPtr < _squareSourceStartIndex)
		//			{
		//				result[resultPtr] = Vector256<ulong>.Zero;
		//				result[resultPtr + 1] = Vector256<ulong>.Zero;
		//			}
		//			else
		//			{
		//				var productVector = Avx2.Multiply(source[j], source[i]);
		//				IncrementMultiplicationsCount(4);

		//				if (i > j)
		//				{
		//					//product *= 2;
		//					productVector = Avx2.ShiftLeftLogical(productVector, 1);
		//				}

		//				// 0/1; 1/2; 2/3

		//				if (_skipSquareResultLow & resultPtr == _squareSourceStartIndex)
		//				{
		//					result[resultPtr] = Vector256<ulong>.Zero;
		//					result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
		//				}
		//				else
		//				{
		//					result[resultPtr] = Avx2.Add(result[resultPtr], Avx2.And(productVector, HIGH33_MASK_VEC_L));
		//					result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
		//				}

		//				//MathOpCounts.NumberOfSplits++;

		//			}
		//		}
		//	}
		//}

		//private void SumThePartialOld(PairOfVec<ulong> source, PairOfVec<ulong> result)
		//{
		//	// To be used after a multiply operation.
		//	// Process the carry portion of each result bin.
		//	// This will leave each result bin with a value <= 2^32 for the final digit.
		//	// If the MSL produces a carry, throw an exception.

		//	_carryVectorsLong1 = Vector256<ulong>.Zero; //Avx2.Xor(_carryVectorsLong1, _carryVectorsLong1);
		//	_carryVectorsLong2 = Vector256<ulong>.Zero; //Avx2.Xor(_carryVectorsLong2, _carryVectorsLong2);

		//	for (int limbPtr = 0; limbPtr < source.Lower.Length; limbPtr++)
		//	{
		//		var withCarries1 = Avx2.Add(source.Lower[limbPtr], _carryVectorsLong1);
		//		var withCarries2 = Avx2.Add(source.Upper[limbPtr], _carryVectorsLong2);

		//		result.Lower[limbPtr] = Avx2.And(withCarries1, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
		//		result.Upper[limbPtr] = Avx2.And(withCarries2, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.

		//		_carryVectorsLong1 = Avx2.ShiftRightLogical(withCarries1, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
		//		_carryVectorsLong2 = Avx2.ShiftRightLogical(withCarries2, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

		//		// Clear the source so that square internal will not have to make a separate call.
		//		source.Lower[limbPtr] = Avx2.Xor(source.Lower[limbPtr], source.Lower[limbPtr]);
		//		source.Upper[limbPtr] = Avx2.Xor(source.Upper[limbPtr], source.Upper[limbPtr]);
		//	}

		//	IncrementAdditionsCount(LimbCount * 16);
		//	IncrementSplitsCount(LimbCount * 16);
		//}

		//public void AddThenSquare(Vector256<uint>[] a, Vector256<uint>[] b, Vector256<uint>[] c)
		//{
		//	Add(a, b, _additionResult);
		//	Square(_additionResult, c);
		//}

		//private bool TryNegate(Vector256<uint>[] source, Vector256<uint>[] result, ref Vector256<int> doneFlagsVec)
		//{
		//	var carry = _ones;

		//	for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
		//	{
		//		var notVector = Avx2.Xor(source[limbPtr], XOR_BITS_VEC);
		//		var newValuesVector = Avx2.Add(notVector, carry);

		//		result[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);
		//		carry = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
		//	}

		//	IncrementNegationsCount(LimbCount * 8);
		//	IncrementSplitsCount(LimbCount * 8);

		//	var anyCarryFound = FP31VecMathHelper.AnyNotZero(carry, doneFlagsVec);
		//	return !anyCarryFound;
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//private int GetSignBits(Vector256<uint>[] source, ref Vector256<int> signBitVecs)
		//{
		//	IncrementComparisonsCount(8);

		//	signBitVecs = Avx2.CompareEqual(Avx2.And(source[LimbCount - 1].AsInt32(), TEST_BIT_30_VEC), ZERO_VEC);
		//	return Avx2.MoveMask(signBitVecs.AsByte());
		//}

		#endregion
	}
}
