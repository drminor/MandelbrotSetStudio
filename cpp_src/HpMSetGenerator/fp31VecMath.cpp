#include "pch.h"
#include "fp31VecMath.h"

//#include "simd_aligned_allocator.h"
//
//typedef std::vector<__m256i, aligned_allocator<__m256i, sizeof(__m256i)> > aligned_vector;

#pragma region Constructor / Destructor

fp31VecMath::fp31VecMath(int limbCount, uint8_t bitsBeforeBp, int targetExponent)
{
	LimbCount = limbCount;
	_bitsBeforeBp = bitsBeforeBp;
	_targetExponent = targetExponent;

	_squareResult0Lo = new aligned_vector(limbCount);
	_squareResult0Hi = new aligned_vector(limbCount);

	_squareResult1Lo = new aligned_vector(limbCount * 2);
	_squareResult1Hi = new aligned_vector(limbCount * 2);

	_squareResult2Lo = new aligned_vector(limbCount * 2);
	_squareResult2Hi = new aligned_vector(limbCount * 2);

	_negationResult = new aligned_vector(limbCount);
	_additionResult = new aligned_vector(limbCount);

	_shiftAmount = _bitsBeforeBp;
	_inverseShiftAmount = EFFECTIVE_BITS_PER_LIMB - _shiftAmount;

	//(_squareSourceStartIndex, _skipSquareResultLow) = CalculateSqrOpParams(LimbCount);

	//MathOpCounts = new MathOpCounts();
}

fp31VecMath::~fp31VecMath()
{
	delete _squareResult0Lo;
	delete _squareResult0Hi;
	delete _squareResult1Lo;
	delete _squareResult1Hi;
	delete _squareResult2Lo;
	delete _squareResult2Hi;
	delete _negationResult;
	delete _additionResult;
}

#pragma endregion

#pragma region Multiply and Square


void fp31VecMath::Square(aligned_vector* const source, aligned_vector* const result)
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

void fp31VecMath::SquareInternal(aligned_vector* const source, aligned_vector* const result)
{
	// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

	//result.ClearManatissMems();

	for (int j = 0; j < LimbCount; j++)
	{
		for (int i = j; i < LimbCount; i++)
		{
			int resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

			//var productVector = Avx2.Multiply(source[j], source[i]);
			__m256i product = _mm256_mul_epu32(source->at(j), source->at(i));
			//IncrementNoMultiplications();

			if (i > j)
			{
				//product *= 2;
				//productVector = Avx2.ShiftLeftLogical(productVector, 1);
				product = _mm256_slli_epi64(product, 1);
			}

			// 0/1; 1/2; 2/3

			//	result[resultPtr] = Avx2.Add(result[resultPtr], Avx2.And(productVector, HIGH33_MASK_VEC_L));
			//	result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
			
			result->at(resultPtr) = _mm256_add_epi64(result->at(resultPtr), _mm256_and_si256(product, HIGH33_MASK_VEC_L));
			result->at((size_t)resultPtr + 1) =_mm256_add_epi64(result->at((size_t)resultPtr + 1), _mm256_srli_epi64(product, EFFECTIVE_BITS_PER_LIMB));


			//MathOpCounts.NumberOfSplits++;
			//MathOpCounts.NumberOfAdditions += 2;
		}
	}
}

#pragma endregion

#pragma region Multiplication Post Processing

void fp31VecMath::SumThePartials(aligned_vector* const source, aligned_vector* const result)
{
	// To be used after a multiply operation.
	// Process the carry portion of each result bin.
	// This will leave each result bin with a value <= 2^32 for the final digit.
	// If the MSL produces a carry, throw an exception.

	//_carryVectorsLong = Vector256<ulong>.Zero;

	_carryVectorsLong = _mm256_set1_epi64x(0);

	for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	{
		__m256i withCarries = _mm256_add_epi64(source->at(limbPtr), _carryVectorsLong);

		result->at(limbPtr) = _mm256_and_si256(withCarries, HIGH33_MASK_VEC_L);				// The low 31 bits of the sum is the result.
		_carryVectorsLong = _mm256_srli_epi64(withCarries, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

		// Clear the source so that square internal will not have to make a separate call.
		source->at(limbPtr) = _mm256_xor_si256(source->at(limbPtr), source->at(limbPtr));
	}
}

void fp31VecMath::ShiftAndTrim(aligned_vector* const sourceLimbsLo, aligned_vector* const sourceLimbsHi, aligned_vector* const resultLimbs)
{
	//ValidateIsSplit(mantissa);

	// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
	// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
	// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

	// Check to see if any of these values are larger than the FP Format.
	//_ = CheckForOverflow(resultLimbs);

	int resultLength = LimbCount;
	int sourceIndex = LimbCount;

	for (int limbPtr = 0; limbPtr < resultLength; limbPtr++)
	{
		if (sourceIndex > 0)
		{
			// Calculate the lo end

			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			__m256i source = sourceLimbsLo->at((size_t)limbPtr + sourceIndex);
			__m256i wideResultLow = _mm256_and_si256(_mm256_slli_epi64(source, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			__m256i prevSource = sourceLimbsLo->at((size_t)limbPtr + sourceIndex - 1);
			wideResultLow = _mm256_or_si256(wideResultLow, _mm256_srli_epi64(_mm256_and_si256(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end

			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			source = sourceLimbsHi->at((size_t)limbPtr + sourceIndex);
			__m256i wideResultHigh = _mm256_and_si256(_mm256_slli_epi64(source, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			prevSource = sourceLimbsHi->at((size_t)limbPtr + sourceIndex - 1);
			wideResultHigh = _mm256_or_si256(wideResultHigh, _mm256_srli_epi64(_mm256_and_si256(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			__m256i low128 = _mm256_permutevar8x32_epi32(wideResultLow, SHUFFLE_PACK_LOW_VEC);
			__m256i high128 = _mm256_permutevar8x32_epi32(wideResultHigh, SHUFFLE_PACK_HIGH_VEC);

			resultLimbs->at(limbPtr) = _mm256_or_si256(low128, high128);

			//MathOpCounts.NumberOfSplits += 4;

		}
	}
}
#pragma endregion

#pragma region Add and Subtract

 void fp31VecMath::Sub(aligned_vector* const left, aligned_vector* const right, aligned_vector* const result)
 {
	 //CheckReservedBitIsClear(b, "Negating B");

	 Negate(right, _negationResult);
	 //MathOpCounts.NumberOfConversions++;

	 Add(left, _negationResult, result);
 }
 
void fp31VecMath::Add(aligned_vector* const left, aligned_vector* const right, aligned_vector* const result)
{
	_carryVectors = _mm256_xor_si256(_carryVectors, _carryVectors);

	 for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	 {
		 __m256i sumVector = _mm256_add_epi32(left->at(limbPtr), right->at(limbPtr));
		 __m256i newValuesVector = _mm256_add_epi32(sumVector, _carryVectors);
		 //MathOpCounts.NumberOfAdditions += 2;

		 result->at(limbPtr) = _mm256_and_si256(newValuesVector, HIGH33_MASK_VEC);			// The low 31 bits of the sum is the result.
		 _carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
	 }
}

#pragma endregion

#pragma region Two Compliment Support

void fp31VecMath::Negate(aligned_vector* const source, aligned_vector* const result)
{
	_carryVectors = _ones;

	for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	{
		__m256i notVector = _mm256_xor_si256(source->at(limbPtr), ALL_BITS_SET_VEC);
		__m256i newValuesVector = _mm256_add_epi32(notVector, _carryVectors);
		//MathOpCounts.NumberOfAdditions += 2;

		result->at(limbPtr) = _mm256_and_si256(newValuesVector, HIGH33_MASK_VEC); ;
		_carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
		//MathOpCounts.NumberOfSplits++;
	}
}

void fp31VecMath::ConvertFrom2C(aligned_vector* const source, aligned_vector* const resultLo, aligned_vector* const resultHi)
{
	//CheckReservedBitIsClear(source, "ConvertFrom2C");

	int signBitFlags = GetSignBits(source, _signBitVecs);

	if (signBitFlags == -1)
	{
		// All positive values
		for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
		{
			// Take the lower 4 values and set the low halves of each result
			resultLo->at(limbPtr) = _mm256_and_si256(_mm256_permutevar8x32_epi32(source->at(limbPtr), SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

			// Take the higher 4 values and set the low halves of each result
			resultHi->at(limbPtr) = _mm256_and_si256(_mm256_permutevar8x32_epi32(source->at(limbPtr), SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
		}
	}
	else
	{
		// Mixed Positive and Negative values
		_carryVectors = _ones;

		for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
		{
			__m256i notVector = _mm256_xor_si256(source->at(limbPtr), ALL_BITS_SET_VEC);
			__m256i newValuesVector = _mm256_add_epi32(notVector, _carryVectors);
			//MathOpCounts.NumberOfAdditions += 2;

			__m256i limbValues = _mm256_and_si256(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
			_carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

			//MathOpCounts.NumberOfSplits++;

			__m256i cLimbValues = (_mm256_blendv_epi8(limbValues, source->at(limbPtr), _signBitVecs));

			// Take the lower 4 values and set the low halves of each result
			resultLo->at(limbPtr) = _mm256_and_si256(_mm256_permutevar8x32_epi32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

			// Take the higher 4 values and set the low halves of each result
			resultHi->at(limbPtr) = _mm256_and_si256(_mm256_permutevar8x32_epi32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
		}
	}
}

int fp31VecMath::GetSignBits(aligned_vector* const source, __m256i &signBitVecs)
{
	__m256i msl = source->at((size_t)LimbCount - 1);
	__m256i left = _mm256_and_si256(msl, TEST_BIT_30_VEC);
	signBitVecs = _mm256_cmpeq_epi32(left, ZERO_VEC);
	
	int result = _mm256_movemask_epi8(signBitVecs);

	return result;
}

#pragma endregion

#pragma region Comparison

void fp31VecMath::IsGreaterOrEqThan(aligned_vector* const source, __m256i right, __m256i& escapedFlagsVec)
{
	__m256i msl = source->at((size_t) LimbCount - 1);

	__m256i sansSign = _mm256_and_si256(msl, SIGN_BIT_MASK_VEC);
	escapedFlagsVec = _mm256_cmpgt_epi32(sansSign, right);
	 
	//MathOpCounts.NumberOfGrtrThanOps++;
}


#pragma endregion
