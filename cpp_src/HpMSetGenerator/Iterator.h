#pragma once

#include "pch.h"
#include <immintrin.h>

class Iterator
{
	VecHelper* _vecHelper;
	fp31VecMath* _vMath;

	int _limbCount;

	uint8_t _threshold;
	__m256i _thresholdVector;

	__m256i* _zrSqrs;
	__m256i* _ziSqrs;
	__m256i* _sumOfSqrs;

	__m256i* _zRZiSqrs;
	__m256i* _tempVec;


public:

	Iterator(int limbCount, uint8_t bitsBeforeBp);
	~Iterator();

	bool GenerateMapCol(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i& counts);

private:

	void IterateFirstRound(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags);

	void Iterate(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags);

	int UpdateCounts(int limbCount, __m256i escapedFlags, __m256i& counts, __m256i& haveEscapedFlags);

};

