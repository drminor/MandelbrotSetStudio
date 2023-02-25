#pragma once

#include "pch.h"
#include <immintrin.h>

//#include "simd_aligned_allocator.h"
//typedef std::vector<__m256i, aligned_allocator<__m256i, sizeof(__m256i)> > aligned_vector;

class Iterator
{
	fp31VecMath* _vMath;

	__m256i _thresholdVector;
	__m256i _targetIterationsVector;

	aligned_vector* _zrSqrs;
	aligned_vector* _ziSqrs;
	aligned_vector* _sumOfSqrs;

	aligned_vector* _zRZiSqrs;
	aligned_vector* _tempVec;

	__m256i _justOne;

public:

	Iterator(fp31VecMath* const vMath, int targetIterations, int thresholdForComparison);
	~Iterator();

	bool GenerateMapCol(aligned_vector* cr, aligned_vector* ciVec, __m256i& resultCounts);

private:

	void IterateFirstRound(aligned_vector* cr, aligned_vector* ci, aligned_vector* zr, aligned_vector* zi, __m256i& escapedFlagsVec);

	void Iterate(aligned_vector* cr, aligned_vector* ci, aligned_vector* zr, aligned_vector* zi, __m256i& escapedFlagsVec);

	int UpdateCounts(__m256i escapedFlagsVec, __m256i& counts, __m256i& resultCounts, __m256i& doneFlags, __m256i& haveEscapedFlags);

};

