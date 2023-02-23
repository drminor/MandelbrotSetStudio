#pragma once

#include "pch.h"
#include <immintrin.h>

class Iterator
{
public:

	bool GenerateMapCol(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i& counts);

private:

	__m256i IterateFirstRound(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags);

	__m256i Iterate(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags);

	int UpdateCounts(int limbCount, __m256i escapedFlags, __m256i& counts, __m256i& haveEscapedFlags);

};

