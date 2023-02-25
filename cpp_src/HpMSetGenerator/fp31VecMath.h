#pragma once

#include <immintrin.h>
#include <cstdint>

class Fp31VecMath
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
	__m256i _carryVectorsLong = _mm256_set1_epi64x(0);

	__m256i _signBitVecs = _mm256_set1_epi32(0);

	int _shiftAmount;
	int _inverseShiftAmount;

	int _bitsBeforeBp;
	int _targetExponent;

	const int EFFECTIVE_BITS_PER_LIMB = 31;

	//private const bool USE_DET_DEBUG = false;

public:
	int LimbCount;

	Fp31VecMath(int limbCount, int bitsBeforeBp, int targetExponent);

	~Fp31VecMath();


	void Square(__m256i* const source, __m256i* const result);

	void Add(__m256i* const left, __m256i* const right, __m256i* const result);
	void Sub(__m256i* const left, __m256i* const right, __m256i* const result);

	void IsGreaterOrEqThan(__m256i* const source, __m256i right, __m256i& escapedFlagsVec);

	__m256i* CreateLimbSet();
	__m256i* CreateWideLimbSet();
private:

	void SquareInternal(__m256i* const source, __m256i* const result);
	void SumThePartials(__m256i* const source, __m256i* const result);
	void ShiftAndTrim(__m256i* const sourceLimbsLo, __m256i* const sourceLimbsHi, __m256i* const resultLimbs);

	void Negate(__m256i* const source, __m256i* const result);
	void ConvertFrom2C(__m256i* const source, __m256i* const resultLo, __m256i* const resultHi);
	int GetSignBits(__m256i* const source, __m256i& signBitVecs);

};

