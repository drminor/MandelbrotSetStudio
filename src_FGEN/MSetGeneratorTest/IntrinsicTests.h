#pragma once

#include <cxxtest/TestSuite.h>

#include <immintrin.h>

class IntrinsicTest : public CxxTest::TestSuite
{

public:

	void testMultiplication(void)
	{
		TS_ASSERT_EQUALS(2 * 2, 4);
	}

	void testAddition(void)
	{
		TS_ASSERT(1 + 1 > 1);
		TS_ASSERT_EQUALS(1 + 1, 2);
	}

	void testVecAdd(void)
	{
		__m256i a = _mm256_set_epi32(8, 7, 6, 5, 4, 3, 2, 1);

		__m256i b = _mm256_set_epi32(18, 17, 16, 15, 14, 13, 12, 11);

		__m256i c = _mm256_add_epi32(a, b);

		// should equal [12,14,16,18,20,22,24,26]

		int d[8];

		_mm256_storeu_epi32(d, c);

		TS_ASSERT(d[0] == 12);

	}

};


