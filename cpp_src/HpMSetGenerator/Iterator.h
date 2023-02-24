#pragma once

#include "pch.h"
#include <immintrin.h>

class Iterator
{
	VecHelper* _vecHelper;
	fp31VecMath* _vMath;

	int _limbCount;

	//uint8_t _threshold;
	__m256i _thresholdVector;

	__m256i _targetIterationsVector;

	__m256i* _zrSqrs;
	__m256i* _ziSqrs;
	__m256i* _sumOfSqrs;

	__m256i* _zRZiSqrs;
	__m256i* _tempVec;

	__m256i _justOne;


public:

	Iterator(int limbCount, uint8_t bitsBeforeBp, int targetIterations, int thresholdForComparison);
	~Iterator();

	bool GenerateMapCol(__m256i* cr, __m256i* ci, __m256i& counts);

private:

	void IterateFirstRound(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlagsVec);

	void Iterate(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlagsVec);

	int UpdateCounts(__m256i escapedFlagsVec, __m256i& counts, __m256i& resultCounts, __m256i& doneFlags, __m256i& haveEscapedFlags);

};

