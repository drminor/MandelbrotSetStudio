#include "pch.h"
#include "fp31VecMath.h"

#pragma region Constructor / Destructor

fp31VecMath::fp31VecMath(size_t limbCount, uint8_t bitsBeforeBp)
{
	_vecHelper = new VecHelper();

	_limbCount = limbCount;
	_bitsBeforeBp = bitsBeforeBp;
	_targetExponent = _limbCount * EFFECTIVE_BITS_PER_LIMB - bitsBeforeBp;

	//ApFixedPointFormat = apFixedPointFormat;

	_squareResult0Lo = _vecHelper->createVec(limbCount);
	_squareResult0Hi = _vecHelper->createVec(limbCount);

	_squareResult1Lo = _vecHelper->createVec(limbCount * 2);
	_squareResult1Hi = _vecHelper->createVec(limbCount * 2);

	_squareResult2Lo = _vecHelper->createVec(limbCount * 2);
	_squareResult2Hi = _vecHelper->createVec(limbCount * 2);

	_negationResult = _vecHelper->createVec(limbCount);
	_additionResult = _vecHelper->createVec(limbCount);

	_ones = _vecHelper->createAndInitVec(limbCount, 1);

	_shiftAmount = _bitsBeforeBp;
	_inverseShiftAmount = EFFECTIVE_BITS_PER_LIMB - _shiftAmount;

	//(_squareSourceStartIndex, _skipSquareResultLow) = CalculateSqrOpParams(LimbCount);

	//MathOpCounts = new MathOpCounts();
}

fp31VecMath::~fp31VecMath()
{
	_vecHelper->clearVec(_limbCount, _squareResult0Lo);
	_vecHelper->clearVec(_limbCount, _squareResult0Hi);

	_vecHelper->clearVec(_limbCount * 2, _squareResult1Lo);
	_vecHelper->clearVec(_limbCount * 2, _squareResult1Hi);
	_vecHelper->clearVec(_limbCount * 2, _squareResult2Lo);
	_vecHelper->clearVec(_limbCount * 2, _squareResult2Hi);

	_vecHelper->clearVec(_limbCount, _negationResult);
	_vecHelper->clearVec(_limbCount, _additionResult);
	delete _vecHelper;
}

#pragma endregion

#pragma region Multiply and Square

void fp31VecMath::Square(__m256i* source, __m256i* result)
{
	//CheckReservedBitIsClear(a, "Squaring");

	//_vecHelper->clearVec(_limbCount, result);

	ConvertFrom2C(source, _squareResult0Lo, _squareResult0Hi);
	//MathOpCounts.NumberOfConversions++;

	SquareInternal(_squareResult0Lo, _squareResult1Lo);
	SquareInternal(_squareResult0Hi, _squareResult1Hi);

	SumThePartials(_squareResult1Lo, _squareResult2Lo);
	SumThePartials(_squareResult1Hi, _squareResult2Hi);

	ShiftAndTrim(_squareResult2Lo, _squareResult2Hi, result);
}

void fp31VecMath::SquareInternal(__m256i* source, __m256i* result)
{
	//// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

	////result.ClearManatissMems();

	//for (int j = 0; j < source.Length; j++)
	//{
	//	for (int i = j; i < source.Length; i++)
	//	{
	//		var resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

	//		var productVector = Avx2.Multiply(source[j], source[i]);
	//		IncrementNoMultiplications();

	//		if (i > j)
	//		{
	//			//product *= 2;
	//			productVector = Avx2.ShiftLeftLogical(productVector, 1);
	//		}

	//		// 0/1; 1/2; 2/3

	//		result[resultPtr] = Avx2.Add(result[resultPtr], Avx2.And(productVector, HIGH33_MASK_VEC_L));
	//		result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
	//		//MathOpCounts.NumberOfSplits++;
	//		//MathOpCounts.NumberOfAdditions += 2;
	//	}
	//}
}

#pragma endregion

#pragma region Multiplication Post Processing

void fp31VecMath::SumThePartials(__m256i* source, __m256i* result)
{
	//// To be used after a multiply operation.
	//// Process the carry portion of each result bin.
	//// This will leave each result bin with a value <= 2^32 for the final digit.
	//// If the MSL produces a carry, throw an exception.

	//_carryVectorsLong = Vector256<ulong>.Zero;

	//for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
	//{
	//	var withCarries = Avx2.Add(source[limbPtr], _carryVectorsLong);

	//	result[limbPtr] = Avx2.And(withCarries, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
	//	_carryVectorsLong = Avx2.ShiftRightLogical(withCarries, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

	//	// Clear the source so that square internal will not have to make a separate call.
	//	source[limbPtr] = Avx2.Xor(source[limbPtr], source[limbPtr]);
	//}
}

 void fp31VecMath::ShiftAndTrim(__m256i* sourceLimbsLo, __m256i* sourceLimbsHi, __m256i* resultLimbs)
{
	////ValidateIsSplit(mantissa);

	//// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
	//// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
	//// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

	//// Check to see if any of these values are larger than the FP Format.
	////_ = CheckForOverflow(resultLimbs);

	//var sourceIndex = Math.Max(sourceLimbsLo.Length - LimbCount, 0);

	//for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)
	//{
	//	if (sourceIndex > 0)
	//	{
	//		// Calculate the lo end

	//		// Take the bits from the source limb, discarding the top shiftAmount of bits.
	//		var source = sourceLimbsLo[limbPtr + sourceIndex];
	//		var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

	//		// Take the top shiftAmount of bits from the previous limb
	//		var prevSource = sourceLimbsLo[limbPtr + sourceIndex - 1];
	//		wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

	//		// Calculate the hi end

	//		// Take the bits from the source limb, discarding the top shiftAmount of bits.
	//		source = sourceLimbsHi[limbPtr + sourceIndex];
	//		var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

	//		// Take the top shiftAmount of bits from the previous limb
	//		prevSource = sourceLimbsHi[limbPtr + sourceIndex - 1];
	//		wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

	//		var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
	//		var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

	//		resultLimbs[limbPtr] = Avx2.Or(low128, high128);

	//		//MathOpCounts.NumberOfSplits += 4;
	//	}
	//	else
	//	{
	//		// Calculate the lo end

	//		// Take the bits from the source limb, discarding the top shiftAmount of bits.
	//		var source = sourceLimbsLo[limbPtr + sourceIndex];
	//		var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

	//		// Calculate the hi end

	//		// Take the bits from the source limb, discarding the top shiftAmount of bits.
	//		source = sourceLimbsHi[limbPtr + sourceIndex];
	//		var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

	//		var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
	//		var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

	//		resultLimbs[limbPtr] = Avx2.Or(low128, high128);

	//		//MathOpCounts.NumberOfSplits += 2;
	//	}
	//}
}

#pragma endregion

#pragma region Add and Subtract

 void fp31VecMath::Sub(__m256i* left, __m256i* right, __m256i* result)
 {
	 //CheckReservedBitIsClear(b, "Negating B");

	 Negate(right, _negationResult);
	 //MathOpCounts.NumberOfConversions++;

	 Add(left, _negationResult, result);
 }
 
void fp31VecMath::Add(__m256i* left, __m256i* right, __m256i* result)
{
	_carryVectors = _mm256_xor_epi32(_carryVectors, _carryVectors);

	 for (int limbPtr = 0; limbPtr < _limbCount; limbPtr++)
	 {
		 __m256i sumVector = _mm256_add_epi32(left[limbPtr], right[limbPtr]);
		 __m256i newValuesVector = _mm256_add_epi32(sumVector, _carryVectors);
		 //MathOpCounts.NumberOfAdditions += 2;

		 result[limbPtr] = _mm256_and_epi32(newValuesVector, HIGH33_MASK_VEC);			// The low 31 bits of the sum is the result.
		 _carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
	 }
}



#pragma endregion

#pragma region Two Compliment Support

void fp31VecMath::Negate(__m256i* source, __m256i* result)
{
	//_carryVectors = _ones;

	//for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
	//{
	//	var notVector = Avx2.Xor(source[limbPtr], ALL_BITS_SET_VEC);
	//	var newValuesVector = Avx2.Add(notVector, _carryVectors);
	//	//MathOpCounts.NumberOfAdditions += 2;

	//	result[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC); ;
	//	_carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
	//	//MathOpCounts.NumberOfSplits++;
	//}
}

void fp31VecMath::ConvertFrom2C(__m256i* source, __m256i* resultLo, __m256i* resultHi)
{
	////CheckReservedBitIsClear(source, "ConvertFrom2C");

	//var signBitFlags = GetSignBits(source, out _signBitVecs);

	//if (signBitFlags == -1)
	//{
	//	// All positive values
	//	for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
	//	{
	//		// Take the lower 4 values and set the low halves of each result
	//		resultLo[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(source[limbPtr], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

	//		// Take the higher 4 values and set the low halves of each result
	//		resultHi[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(source[limbPtr], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
	//	}
	//}
	//else
	//{
	//	// Mixed Positive and Negative values
	//	_carryVectors = _ones;

	//	for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
	//	{
	//		var notVector = Avx2.Xor(source[limbPtr], ALL_BITS_SET_VEC);
	//		var newValuesVector = Avx2.Add(notVector, _carryVectors);
	//		//MathOpCounts.NumberOfAdditions += 2;

	//		var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
	//		_carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

	//		//MathOpCounts.NumberOfSplits++;

	//		var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source[limbPtr].AsByte(), _signBitVecs.AsByte())).AsUInt32();

	//		// Take the lower 4 values and set the low halves of each result
	//		resultLo[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

	//		// Take the higher 4 values and set the low halves of each result
	//		resultHi[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
	//	}
	//}
}

int fp31VecMath::GetSignBits(__m256i* source, __m256i &signBitVecs)
{
	__m256i msl = source[_limbCount - 1];

	//var left = Avx2.And(msl.AsInt32(), TEST_BIT_30_VEC);
	__m256i left = _mm256_and_epi32(msl, TEST_BIT_30_VEC);
	 
	//signBitVecs = Avx2.CompareEqual(left, ZERO_VEC); // dst[i+31:i] := ( a[i+31:i] == b[i+31:i] ) ? 0xFFFFFFFF : 0
	signBitVecs = _mm256_cmpeq_epi32(left, ZERO_VEC);
	
	//var result = Avx2.MoveMask(signBitVecs.AsByte());
	int result = _mm256_movemask_epi8(signBitVecs);

	return result;

	//signBitVecs = Avx2.CompareEqual(Avx2.And(source[LimbCount - 1].AsInt32(), TEST_BIT_30_VEC), ZERO_VEC);
	//return Avx2.MoveMask(signBitVecs.AsByte());

	return 0;
}

#pragma endregion

#pragma region Comparison

void fp31VecMath::IsGreaterOrEqThan(__m256i left, __m256i right, __m256i& escapedFlagsVec)
{
	//var sansSign = Avx2.And(left, SIGN_BIT_MASK_VEC);
	//escapedFlagsVec = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);
	
	__m256i sansSign = _mm256_and_epi32(left, SIGN_BIT_MASK_VEC);
	escapedFlagsVec = _mm256_cmpgt_epi32(sansSign, right);

	 
	//MathOpCounts.NumberOfGrtrThanOps++;
}

#pragma endregion
