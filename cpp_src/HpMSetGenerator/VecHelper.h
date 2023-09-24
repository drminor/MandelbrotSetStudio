#pragma once
#include "framework.h"
#include <immintrin.h>
#include <iostream>
class VecHelper
{
public:

	inline __m256i* createVec(const int n)
	{
		void* ptr;
		size_t  alignment;
		alignment = 32;

		ptr = _aligned_malloc(sizeof(__m256i) * n, alignment);
		if (ptr == NULL)
		{
			printf_s("Error allocation aligned memory.");
			return NULL;
		}

		__m256i* result = (__m256i*) ptr;

#pragma warning( push )
#pragma warning( disable : 6385 )
#pragma warning( disable : 6386 )

		for (int i = 0; i < n; i++)
		{
			result[i] = _mm256_xor_si256(result[i], result[i]);
		}
#pragma warning( pop )

		return result;

	}

	inline void clearVec(int n, __m256i* vec)
	{
		for (int i = 0; i < n; i++)
		{
			vec[i] = _mm256_xor_si256(vec[i], vec[i]);
		}
	}

	inline __m256i* createAndInitVec(const size_t n, const int val)
	{
		void* ptr;
		size_t  alignment;
		alignment = 32;

		ptr = _aligned_malloc(sizeof(__m256i) * n, alignment);
		if (ptr == NULL)
		{
			printf_s("Error allocation aligned memory.");
			return NULL;
		}

		__m256i* result = (__m256i*) ptr;

#pragma warning( push )
#pragma warning( disable : 6386 )

		for (int i = 0; i < n; i++)
		{
			result[i] = _mm256_set1_epi32(val);
		}
#pragma warning( pop )

		return result;
	}

	inline void copyVec(__m256i* source, __m256i* destination, int n)
	{
		for (int i = 0; i < n; i++)
		{
			destination[i] = source[i];
		}
	}

	inline void freeVec(__m256i* source)
	{
		_aligned_free(source);
	}

};

