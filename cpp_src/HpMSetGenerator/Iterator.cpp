#include "pch.h"

#include "framework.h"
#include <immintrin.h>
#include "MSetGenerator.h"
#include "Fp31VecMath.h"
#include "Iterator.h"

#include <array>

#pragma region Constructor / Destructor

Iterator::Iterator(Fp31VecMath* const vMath, int targetIterations, int thresholdForComparison)
{
    _vMath = vMath;

    _thresholdVector = _mm256_set1_epi32(thresholdForComparison);

    _targetIterationsVector = _mm256_set1_epi32(targetIterations);

    _zrSqrs = _vMath->CreateLimbSet();
    _ziSqrs = _vMath->CreateLimbSet();
    _sumOfSqrs = _vMath->CreateLimbSet();

    _zRZiSqrs = _vMath->CreateLimbSet();
    _tempVec = _vMath->CreateLimbSet();

    _justOne = _mm256_set1_epi32(1);
}

Iterator::~Iterator()
{
    delete _zrSqrs;
    delete _ziSqrs;
    delete _sumOfSqrs;
    delete _zRZiSqrs;
    delete _tempVec;
}

#pragma endregion

bool Iterator::GenerateMapCol(__m256i* const cr, __m256i* const ciVec, __m256i& resultCounts)
{
    __m256i haveEscapedFlags = _mm256_set1_epi32(0);

    __m256i doneFlags = _mm256_set1_epi32(0);

    resultCounts = _mm256_set1_epi32(0);
    __m256i counts = _mm256_set1_epi32(0);

    __m256i* zr = _vMath->CreateLimbSet();
    __m256i* zi = _vMath->CreateLimbSet();

    //__m256i* resultZr = _vh->createVec(limbCount);
    //__m256i* resultZi = _vh->createVec(limbCount);

    __m256i escapedFlagsVec = _mm256_set1_epi32(0);

    IterateFirstRound(cr, ciVec, zr, zi, escapedFlagsVec);
    int compositeIsDone = UpdateCounts(escapedFlagsVec, counts, resultCounts, doneFlags, haveEscapedFlags);

    while (compositeIsDone != -1)
    {
        Iterate(cr, ciVec, zr, zi, escapedFlagsVec);
        compositeIsDone = UpdateCounts(escapedFlagsVec, counts, resultCounts, doneFlags, haveEscapedFlags);
    }

    int allEscaped = _mm256_movemask_epi8(haveEscapedFlags);

    return allEscaped == -1 ? true : false;
}

void Iterator::IterateFirstRound(__m256i* const cr, __m256i* const ci, __m256i* const zr, __m256i* const zi, __m256i& escapedFlagsVec)
{
    for (int limbPtr = 0; limbPtr < _vMath->LimbCount; limbPtr++) {
        zr[limbPtr] = cr[limbPtr];
        zi[limbPtr] = ci[limbPtr];
    }

    _vMath->Square(zr, _zrSqrs);
    _vMath->Square(zi, _ziSqrs);

    _vMath->Add(_zrSqrs, _ziSqrs, _sumOfSqrs);
    _vMath->IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, escapedFlagsVec);
}

void Iterator::Iterate(__m256i* const cr, __m256i* const ci, __m256i* const zr, __m256i* const zi, __m256i& escapedFlagsVec)
{

    // square(z.r + z.i)
    _vMath->Add(zr, zi, _tempVec);

    _vMath->Square(_tempVec, _zRZiSqrs);

    // z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd
    _vMath->Sub(_zRZiSqrs, _zrSqrs, zi);

    _vMath->Sub(zi, _ziSqrs, _tempVec);

    _vMath->Add(_tempVec, ci, zi);

    // z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
    _vMath->Sub(_zrSqrs, _ziSqrs, _tempVec);
    
    _vMath->Add(_tempVec, cr, zr);

    _vMath->Square(zr, _zrSqrs);
    _vMath->Square(zi, _ziSqrs);
    _vMath->Add(_zrSqrs, _ziSqrs, _sumOfSqrs);

    _vMath->IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, escapedFlagsVec);

}

int Iterator::UpdateCounts(__m256i escapedFlagsVec, __m256i& counts, __m256i& resultCounts, __m256i& doneFlags, __m256i& hasEscapedFlags)
{
    counts = _mm256_add_epi32(counts, _justOne);

    // Apply the new escapedFlags, only if the doneFlags is false for each vector position
    hasEscapedFlags = _mm256_blendv_epi8(escapedFlagsVec, hasEscapedFlags, doneFlags);

    // Compare the new Counts with the TargetIterations
    const __m256i targetReachedCompVec = _mm256_cmpgt_epi32(counts, _targetIterationsVector);

    const __m256i prevDoneFlags = doneFlags;

    // If escaped or reached the target iterations, we're done 
    doneFlags = _mm256_or_si256(hasEscapedFlags, targetReachedCompVec);

    int compositeIsDone = _mm256_movemask_epi8(doneFlags);
    int prevCompositeIsDone = _mm256_movemask_epi8(prevDoneFlags);

    if (compositeIsDone != prevCompositeIsDone)
    {
        __m256i justNowDone = _mm256_cmpeq_epi32(prevDoneFlags, doneFlags);

        // Save the current count 
        resultCounts = _mm256_blendv_epi8(counts, resultCounts, justNowDone); // use First if Zero, second if 1
    }
    
    return compositeIsDone;
}


/*

// Select between two sources, byte by byte. Used in various functions and operators
// Corresponds to this pseudocode:
// for (int i = 0; i < 32; i++) result[i] = s[i] ? a[i] : b[i];
// Each byte in s must be either 0 (false) or 0xFF (true). No other values are allowed.
// Only bit 7 in each byte of s is checked,
static inline __m256i selectb (__m256i const s, __m256i const a, __m256i const b) {
    return _mm256_blendv_epi8 (b, a, s);
}

// horizontal_and. Returns true if all bits are 1
static inline bool horizontal_and (Vec256b const a) {
    return _mm256_testc_si256(a,_mm256_set1_epi32(-1)) != 0;
}

// horizontal_or. Returns true if at least one bit is 1
static inline bool horizontal_or (Vec256b const a) {
    return ! _mm256_testz_si256(a,a);
}


*/
