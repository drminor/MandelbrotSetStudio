#pragma once

#include <immintrin.h>
#include "VecHelper.h"

class fp31VecMath
{
	VecHelper* _vecHelper;

	int _limbCount;

	uint8_t _bitsBeforeBp;
	int _targetExponent;

	const uint8_t EFFECTIVE_BITS_PER_LIMB = 31;

	const uint32_t LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
	const __m256i HIGH33_MASK_VEC = _mm256_set1_epi32(LOW31_BITS_SET);

	const uint64_t LOW31_BITS_SET_L = (uint64_t)0x000000007FFFFFFF; // bits 0 - 30 are set.
	const __m256i HIGH33_MASK_VEC_L = _mm256_set1_epi64x(LOW31_BITS_SET_L);

	const uint32_t SIGN_BIT_MASK = 0x3FFFFFFF;
	const __m256i SIGN_BIT_MASK_VEC = _mm256_set1_epi32(SIGN_BIT_MASK);

	const uint32_t RESERVED_BIT_MASK = 0x80000000;
	const __m256i RESERVED_BIT_MASK_VEC = _mm256_set1_epi32(RESERVED_BIT_MASK);

	const int TEST_BIT_30 = 0x40000000; // bit 30 is set.
	const __m256i TEST_BIT_30_VEC = _mm256_set1_epi32(TEST_BIT_30);

	const __m256i ZERO_VEC = _mm256_set1_epi32(0);
	const __m256i ALL_BITS_SET_VEC = _mm256_set1_epi32(-1);

	const __m256i SHUFFLE_EXP_LOW_VEC = _mm256_set_epi32(0u, 0u, 1u, 1u, 2u, 2u, 3u, 3u);
	const __m256i SHUFFLE_EXP_HIGH_VEC = _mm256_set_epi32(4u, 4u, 5u, 5u, 6u, 6u, 7u, 7u);

	const __m256i SHUFFLE_PACK_LOW_VEC = _mm256_set_epi32(0u, 2u, 4u, 6u, 0u, 0u, 0u, 0u);
	const __m256i SHUFFLE_PACK_HIGH_VEC = _mm256_set_epi32(0u, 0u, 0u, 0u, 0u, 2u, 4u, 6u);

	__m256i* _squareResult0Lo;
	__m256i* _squareResult0Hi;

	__m256i* _squareResult1Lo;
	__m256i* _squareResult1Hi;

	__m256i* _squareResult2Lo;
	__m256i* _squareResult2Hi;

	__m256i* _negationResult;
	__m256i* _additionResult;

	__m256i _ones = _mm256_set1_epi32(1);

	__m256i _carryVectors = _mm256_set1_epi32(0);
	__m256i _carryVectorsLong = _mm256_set1_epi32(0);

	__m256i _signBitVecs = _mm256_set1_epi32(0);

	uint8_t _shiftAmount;
	uint8_t _inverseShiftAmount;

	//private const bool USE_DET_DEBUG = false;

public:

	fp31VecMath(int limbCount, uint8_t bitsBeforeBp);
	~fp31VecMath();


	void Square(__m256i* source, __m256i* result);

	void Add(__m256i* left, __m256i* right, __m256i* result);
	void Sub(__m256i* left, __m256i* right, __m256i* result);

	void IsGreaterOrEqThan(__m256i left, __m256i right, __m256i& escapedFlagsVec);

private:

	void SquareInternal(__m256i* source, __m256i* result);
	void SumThePartials(__m256i* source, __m256i* result);
	void ShiftAndTrim(__m256i* sourceLimbsLo, __m256i* sourceLimbsHi, __m256i* resultLimbs);

	void Negate(__m256i* source, __m256i* result);
	void ConvertFrom2C(__m256i* source, __m256i* resultLo, __m256i* resultHi);
	int GetSignBits(__m256i* source, __m256i& signBitVecs);

};

