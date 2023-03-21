#pragma once

#include "pch.h"
#include <immintrin.h>
#include <array>

class Iterator
{
	Fp31VecMath* _vMath;

	__m256i _thresholdVector;
	__m256i _targetIterationsVector;

	__m256i* _zrSqrs;
	__m256i* _ziSqrs;
	__m256i* _sumOfSqrs;

	__m256i* _zRZiSqrs;
	__m256i* _tempVec;

	__m256i _justOne;

public:

	Iterator(Fp31VecMath* const vMath, int targetIterations, int thresholdForComparison);
	~Iterator();

	bool GenerateMapCol(__m256i* const cr, __m256i* const ciVec, __m256i& resultCounts);

private:

	void IterateFirstRound(__m256i* const cr, __m256i* const ci, __m256i* const zr, __m256i* const zi, __m256i& escapedFlagsVec);

	void Iterate(__m256i* const cr, __m256i* const ci, __m256i* const zr, __m256i* const zi, __m256i& escapedFlagsVec);

	int UpdateCounts(__m256i escapedFlagsVec, __m256i& counts, __m256i& resultCounts, __m256i& doneFlags, __m256i& haveEscapedFlags);

};

