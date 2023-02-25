#pragma once

#include <immintrin.h>
#include "simd_aligned_allocator.h"

typedef std::vector<__m256i, aligned_allocator<__m256i, sizeof(__m256i)> > aligned_vector;

class fp31VecMath
{
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

	const __m256i SHUFFLE_PACK_LOW_VEC = _mm256_set_epi32(0u, 2u, 4u, 6u, 1u, 1u, 1u, 1u);
	const __m256i SHUFFLE_PACK_HIGH_VEC = _mm256_set_epi32(1u, 1u, 1u, 1u, 0u, 2u, 4u, 6u);

	aligned_vector* _squareResult0Lo;
	aligned_vector* _squareResult0Hi;

	aligned_vector* _squareResult1Lo;
	aligned_vector* _squareResult1Hi;

	aligned_vector* _squareResult2Lo;
	aligned_vector* _squareResult2Hi;

	aligned_vector* _negationResult;
	aligned_vector* _additionResult;

	__m256i _ones = _mm256_set1_epi32(1);

	__m256i _carryVectors = _mm256_set1_epi32(0);
	__m256i _carryVectorsLong = _mm256_set1_epi64x(0);

	__m256i _signBitVecs = _mm256_set1_epi32(0);

	uint8_t _shiftAmount;
	uint8_t _inverseShiftAmount;

	uint8_t _bitsBeforeBp;
	int _targetExponent;

	const uint8_t EFFECTIVE_BITS_PER_LIMB = 31;

	//private const bool USE_DET_DEBUG = false;

public:

	int LimbCount;

	fp31VecMath(int limbCount, uint8_t bitsBeforeBp, int targetExponent);
	~fp31VecMath();


	void Square(aligned_vector* const source, aligned_vector* const result);

	void Add(aligned_vector* const left, aligned_vector* const right, aligned_vector* const result);
	void Sub(aligned_vector* const left, aligned_vector* const right, aligned_vector* const result);

	void IsGreaterOrEqThan(__m256i left, __m256i right, __m256i& escapedFlagsVec);

private:

	void SquareInternal(aligned_vector* const source, aligned_vector* const result);
	void SumThePartials(aligned_vector* const source, aligned_vector* const result);
	void ShiftAndTrim(aligned_vector* const sourceLimbsLo, aligned_vector* const sourceLimbsHi, aligned_vector* const resultLimbs);

	void Negate(aligned_vector* const source, aligned_vector* const result);
	void ConvertFrom2C(aligned_vector* const source, aligned_vector* const resultLo, aligned_vector* const resultHi);
	int GetSignBits(aligned_vector* const source, __m256i& signBitVecs);

};

