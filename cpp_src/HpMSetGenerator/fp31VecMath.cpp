#include "pch.h"
#include "Fp31VecMath.h"


#pragma region Constructor / Destructor

Fp31VecMath::Fp31VecMath(int limbCount, int bitsBeforeBp, int targetExponent)
{
	LimbCount = limbCount;
	_bitsBeforeBp = bitsBeforeBp;
	_targetExponent = targetExponent;

	_squareResult0Lo = CreateLimbSet();
	_squareResult0Hi = CreateLimbSet();

	_squareResult1Lo = CreateWideLimbSet();
	_squareResult1Hi = CreateWideLimbSet();

	_squareResult2Lo = CreateWideLimbSet();
	_squareResult2Hi = CreateWideLimbSet();

	_negationResult = CreateWideLimbSet();
	_additionResult = CreateWideLimbSet();

	_shiftAmount = _bitsBeforeBp;
	_inverseShiftAmount = EFFECTIVE_BITS_PER_LIMB - _shiftAmount;

	//(_squareSourceStartIndex, _skipSquareResultLow) = CalculateSqrOpParams(LimbCount);

	//MathOpCounts = new MathOpCounts();
}

Fp31VecMath::~Fp31VecMath()
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

void Fp31VecMath::Square(__m256i* const source, __m256i* const result)
{
	//CheckReservedBitIsClear(a, "Squaring");

	ClearWideLimbSet(_squareResult1Lo);
	ClearWideLimbSet(_squareResult1Hi);

	ConvertFrom2C(source, _squareResult0Lo, _squareResult0Hi);
	//MathOpCounts.NumberOfConversions++;

	SquareInternal(_squareResult0Lo, _squareResult1Lo);
	SquareInternal(_squareResult0Hi, _squareResult1Hi);

	SumThePartials(_squareResult1Lo, _squareResult2Lo);
	SumThePartials(_squareResult1Hi, _squareResult2Hi);

	ShiftAndTrim(_squareResult2Lo, _squareResult2Hi, result);
}

void Fp31VecMath::SquareInternal(__m256i* const source, __m256i* const result)
{
	// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

	//result.ClearManatissMems();

	for (int j = 0; j < LimbCount; j++)
	{
		for (int i = j; i < LimbCount; i++)
		{
			int resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

			//var productVector = Avx2.Multiply(source[j], source[i]);
			__m256i product = _mm256_mul_epu32(source[j], source[i]);
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
			
			result[resultPtr] = _mm256_add_epi64(result[resultPtr], _mm256_and_si256(product, HIGH33_MASK_VEC_L));
			result[(size_t)resultPtr + 1] = _mm256_add_epi64(result[(size_t)resultPtr + 1], _mm256_srli_epi64(product, EFFECTIVE_BITS_PER_LIMB));


			//MathOpCounts.NumberOfSplits++;
			//MathOpCounts.NumberOfAdditions += 2;
		}
	}
}

#pragma endregion

#pragma region Multiplication Post Processing

void Fp31VecMath::SumThePartials(__m256i* const source, __m256i* const result)
{
	// To be used after a multiply operation.
	// Process the carry portion of each result bin.
	// This will leave each result bin with a value <= 2^32 for the final digit.
	// If the MSL produces a carry, throw an exception.

	//_carryVectorsLong = Vector256<ulong>.Zero;

	_carryVectorsLong = _mm256_set1_epi64x(0);

	for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	{
		__m256i withCarries = _mm256_add_epi64(source[limbPtr], _carryVectorsLong);

		result[limbPtr] = _mm256_and_si256(withCarries, HIGH33_MASK_VEC_L);				// The low 31 bits of the sum is the result.
		_carryVectorsLong = _mm256_srli_epi64(withCarries, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.

		// Clear the source so that square internal will not have to make a separate call.
		source[limbPtr] = _mm256_xor_si256(source[limbPtr], source[limbPtr]);
	}
}

void Fp31VecMath::ShiftAndTrim(__m256i* const sourceLimbsLo, __m256i* const sourceLimbsHi, __m256i* const resultLimbs)
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
			__m256i source = sourceLimbsLo[(size_t)limbPtr + sourceIndex];
			__m256i wideResultLow = _mm256_and_si256(_mm256_slli_epi64(source, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			__m256i prevSource = sourceLimbsLo[(size_t)limbPtr + sourceIndex - 1];
			wideResultLow = _mm256_or_si256(wideResultLow, _mm256_srli_epi64(_mm256_and_si256(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			// Calculate the hi end

			// Take the bits from the source limb, discarding the top shiftAmount of bits.
			source = sourceLimbsHi[(size_t)limbPtr + sourceIndex];
			__m256i wideResultHigh = _mm256_and_si256(_mm256_slli_epi64(source, _shiftAmount), HIGH33_MASK_VEC_L);

			// Take the top shiftAmount of bits from the previous limb
			prevSource = sourceLimbsHi[(size_t)limbPtr + sourceIndex - 1];
			wideResultHigh = _mm256_or_si256(wideResultHigh, _mm256_srli_epi64(_mm256_and_si256(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

			__m128i low128 = _mm256_castsi256_si128(_mm256_permutevar8x32_epi32(wideResultLow, SHUFFLE_PACK_LOW_VEC));
			__m256i high128 = _mm256_permutevar8x32_epi32(wideResultHigh, SHUFFLE_PACK_HIGH_VEC);

			//resultLimbs[limbPtr] = _mm256_or_si256(low128, high128);
			resultLimbs[limbPtr] = _mm256_inserti128_si256(high128, low128, 0); // Copy high128 to dst, then over write low half with low128.

			//MathOpCounts.NumberOfSplits += 4;

		}
	}
}
#pragma endregion

#pragma region Add and Subtract

 void Fp31VecMath::Sub(__m256i* const left, __m256i* const right, __m256i* const result)
 {
	 //CheckReservedBitIsClear(b, "Negating B");

	 Negate(right, _negationResult);
	 //MathOpCounts.NumberOfConversions++;

	 Add(left, _negationResult, result);
 }
 
void Fp31VecMath::Add(__m256i* const left, __m256i* const right, __m256i* const result)
{
	_carryVectors = _mm256_xor_si256(_carryVectors, _carryVectors);

	 for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	 {
		 __m256i sumVector = _mm256_add_epi32(left[limbPtr], right[limbPtr]);
		 __m256i newValuesVector = _mm256_add_epi32(sumVector, _carryVectors);
		 //MathOpCounts.NumberOfAdditions += 2;

		 result[limbPtr] = _mm256_and_si256(newValuesVector, HIGH33_MASK_VEC);			// The low 31 bits of the sum is the result.
		 _carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
	 }
}

#pragma endregion

#pragma region Two Compliment Support

void Fp31VecMath::Negate(__m256i* const source, __m256i* const result)
{
	_carryVectors = _ones;

	for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	{
		__m256i notVector = _mm256_xor_si256(source[limbPtr], ALL_BITS_SET_VEC);
		__m256i newValuesVector = _mm256_add_epi32(notVector, _carryVectors);
		//MathOpCounts.NumberOfAdditions += 2;

		result[limbPtr] = _mm256_and_si256(newValuesVector, HIGH33_MASK_VEC); ;
		_carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
		//MathOpCounts.NumberOfSplits++;
	}
}

void Fp31VecMath::ConvertFrom2C(__m256i* const source, __m256i* const resultLo, __m256i* const resultHi)
{
	//CheckReservedBitIsClear(source, "ConvertFrom2C");

	int signBitFlags = GetSignBits(source, _signBitVecs);

	if (signBitFlags == -1)
	{
		// All positive values
		for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
		{
			// Take the lower 4 values and set the low halves of each result
			resultLo[limbPtr] = _mm256_and_si256(_mm256_permutevar8x32_epi32(source[limbPtr], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

			// Take the higher 4 values and set the low halves of each result
			resultHi[limbPtr] = _mm256_and_si256(_mm256_permutevar8x32_epi32(source[limbPtr], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
		}
	}
	else
	{
		// Mixed Positive and Negative values
		_carryVectors = _ones;

		for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
		{
			__m256i notVector = _mm256_xor_si256(source[limbPtr], ALL_BITS_SET_VEC);
			__m256i newValuesVector = _mm256_add_epi32(notVector, _carryVectors);
			//MathOpCounts.NumberOfAdditions += 2;

			__m256i limbValues = _mm256_and_si256(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
			_carryVectors = _mm256_srli_epi32(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

			//MathOpCounts.NumberOfSplits++;

			__m256i cLimbValues = (_mm256_blendv_epi8(limbValues, source[limbPtr], _signBitVecs));

			// Take the lower 4 values and set the low halves of each result
			resultLo[limbPtr] = _mm256_and_si256(_mm256_permutevar8x32_epi32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

			// Take the higher 4 values and set the low halves of each result
			resultHi[limbPtr] = _mm256_and_si256(_mm256_permutevar8x32_epi32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
		}
	}
}

int Fp31VecMath::GetSignBits(__m256i* const source, __m256i &signBitVecs)
{
	__m256i msl = source[(size_t)LimbCount - 1];
	__m256i left = _mm256_and_si256(msl, TEST_BIT_30_VEC);
	signBitVecs = _mm256_cmpeq_epi32(left, ZERO_VEC);
	
	int result = _mm256_movemask_epi8(signBitVecs);

	return result;
}

#pragma endregion

#pragma region Comparison

void Fp31VecMath::IsGreaterOrEqThan(__m256i* const source, __m256i right, __m256i& escapedFlagsVec)
{
	__m256i msl = source[(size_t)LimbCount - 1];

	__m256i sansSign = _mm256_and_si256(msl, SIGN_BIT_MASK_VEC);
	escapedFlagsVec = _mm256_cmpgt_epi32(sansSign, right);
	 
	//MathOpCounts.NumberOfGrtrThanOps++;
}


#pragma endregion

#pragma region Value Support

// Create Limb Set
#pragma warning( push )
#pragma warning( disable : 4316 )

__m256i* Fp31VecMath::CreateLimbSet()
{
	__m256i* result = new __m256i[LimbCount];
	ClearLimbSet(result);

	return result;
}

__m256i* Fp31VecMath::CreateWideLimbSet()
{
	__m256i* result = new __m256i[LimbCount * 2];
	ClearWideLimbSet(result);

	return result;
}

#pragma warning( pop )

// Clear Limb Set
void Fp31VecMath::ClearLimbSet(__m256i* const limbSet)
{
	for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
	{
		limbSet[limbPtr] = _mm256_xor_si256(limbSet[limbPtr], limbSet[limbPtr]);
	}
}

void Fp31VecMath::ClearWideLimbSet(__m256i* const limbSet)
{
	for (int limbPtr = 0; limbPtr < LimbCount * 2; limbPtr++)
	{
		limbSet[limbPtr] = _mm256_xor_si256(limbSet[limbPtr], limbSet[limbPtr]);
	}
}


#pragma endregion





