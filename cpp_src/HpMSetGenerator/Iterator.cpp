#include "pch.h"

#include "framework.h"
#include <immintrin.h>
#include "MSetGenerator.h"
#include "VecHelper.h"
#include "fp31VecMath.h"
#include "Iterator.h"

#pragma region Constructor / Destructor

Iterator::Iterator(int limbCount, uint8_t bitsBeforeBp, int targetIterations, int thresholdForComparison)
{
    _limbCount = limbCount;

    _vecHelper = new VecHelper();
    _vMath = new fp31VecMath(limbCount, bitsBeforeBp);

    _thresholdVector = _mm256_set1_epi32(thresholdForComparison);

    _targetIterationsVector = _mm256_set1_epi32(targetIterations);

    _zrSqrs = _vecHelper->createVec(limbCount);
    _ziSqrs = _vecHelper->createVec(limbCount);
    _sumOfSqrs = _vecHelper->createVec(limbCount);

    _zRZiSqrs = _vecHelper->createVec(limbCount);
    _tempVec = _vecHelper->createVec(limbCount);

    _justOne = _mm256_set1_epi32(1);
}

Iterator::~Iterator()
{
    //_vecHelper->freeVec(_zrSqrs);
    //_vecHelper->freeVec(_ziSqrs);
    //_vecHelper->freeVec(_sumOfSqrs);
    //_vecHelper->freeVec(_zRZiSqrs);
    //_vecHelper->freeVec(_tempVec);

    delete _vecHelper;
    delete _vMath;
}

#pragma endregion

bool Iterator::GenerateMapCol(__m256i* cr, __m256i* ci, __m256i& resultCounts)
{
    __m256i haveEscapedFlags = _mm256_set1_epi32(0);

    __m256i doneFlags = _mm256_set1_epi32(0);

    resultCounts = _mm256_set1_epi32(0);
    __m256i counts = _mm256_set1_epi32(0);

    VecHelper* _vh = new VecHelper();
    __m256i* zr = _vh->createVec(_limbCount);
    __m256i* zi = _vh->createVec(_limbCount);
    //__m256i* resultZr = _vh->createVec(limbCount);
    //__m256i* resultZi = _vh->createVec(limbCount);

    __m256i escapedFlagsVec = _mm256_set1_epi32(0);

    IterateFirstRound(cr, ci, zr, zi, escapedFlagsVec);
    int compositeIsDone = UpdateCounts(escapedFlagsVec, counts, resultCounts, doneFlags, haveEscapedFlags);

    while (compositeIsDone != -1)
    {
        Iterate(cr, ci, zr, zi, escapedFlagsVec);
        compositeIsDone = UpdateCounts(escapedFlagsVec, counts, resultCounts, doneFlags, haveEscapedFlags);
    }

    _vh->freeVec(zr);
    _vh->freeVec(zi);
    delete _vh;

    return true;
}

void Iterator::IterateFirstRound(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlagsVec)
{
    _vecHelper->copyVec(cr, zr, _limbCount);
    _vecHelper->copyVec(ci, zi, _limbCount);

    _vMath->Square(zr, _zrSqrs);
    _vMath->Square(zi, _ziSqrs);

    _vMath->Add(_zrSqrs, _ziSqrs, _sumOfSqrs);
    _vMath->IsGreaterOrEqThan(_sumOfSqrs[_limbCount - 1], _thresholdVector, escapedFlagsVec);
}

void Iterator::Iterate(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlagsVec)
{

    // square(z.r + z.i)
    //    _fp31VecMath.Add(zrs, zis, _temp);
    _vMath->Add(zr, zi, _tempVec);

    //    _fp31VecMath.Square(_temp, _zRZiSqrs);
    _vMath->Square(_tempVec, _zRZiSqrs);

    // z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd
    //    _fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
    _vMath->Sub(_zRZiSqrs, _zrSqrs, zi);

    //    _fp31VecMath.Sub(zis, _zISqrs, _temp);
    _vMath->Sub(zi, _ziSqrs, _tempVec);

    //    _fp31VecMath.Add(_temp, cis, zis);
    _vMath->Add(_tempVec, ci, zi);

    // z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
    //    _fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
    _vMath->Sub(_zrSqrs, _ziSqrs, _tempVec);
    
    //    _fp31VecMath.Add(_temp, crs, zrs);
    _vMath->Add(_tempVec, cr, zr);

    //    _fp31VecMath.Square(zrs, _zRSqrs);
    //    _fp31VecMath.Square(zis, _zISqrs);
    //    _fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

    _vMath->Square(zr, _zrSqrs);
    _vMath->Square(zi, _ziSqrs);
    _vMath->Add(_zrSqrs, _ziSqrs, _sumOfSqrs);

    //    _fp31VecMath.IsGreaterOrEqThan(ref _sumOfSqrs[^ 1], ref _thresholdVector, ref escapedFlagsVec);
    _vMath->IsGreaterOrEqThan(_sumOfSqrs[_limbCount - 1], _thresholdVector, escapedFlagsVec);

}

int Iterator::UpdateCounts(__m256i escapedFlagsVec, __m256i& counts, __m256i& resultCounts, __m256i& doneFlags, __m256i& hasEscapedFlags)
{
    //counts = Avx2.Add(counts, _justOne);

    counts = _mm256_add_epi32(counts, _justOne);

    // Apply the new escapedFlags, only if the doneFlags is false for each vector position
    //hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec, hasEscapedFlagsV, doneFlagsV);
    hasEscapedFlags = _mm256_blendv_epi8(escapedFlagsVec, hasEscapedFlags, doneFlags);

    // Compare the new Counts with the TargetIterations
    //var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsV);
    __m256i targetReachedCompVec = _mm256_cmpgt_epi32(counts, _targetIterationsVector);

    __m256i prevDoneFlags = doneFlags;

    // If escaped or reached the target iterations, we're done 
    //doneFlagsV = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec);
    doneFlags = _mm256_or_epi32(hasEscapedFlags, targetReachedCompVec);

    //var compositeIsDone = Avx2.MoveMask(doneFlagsV.AsByte());
    //var prevCompositeIsDone = Avx2.MoveMask(prevDoneFlagsV.AsByte());

    int compositeIsDone = _mm256_movemask_epi8(doneFlags);
    int prevCompositeIsDone = _mm256_movemask_epi8(prevDoneFlags);

    if (compositeIsDone != prevCompositeIsDone)
    {
        //var justNowDone = Avx2.CompareEqual(prevDoneFlagsV, doneFlagsV);
        __m256i justNowDone = _mm256_cmpeq_epi32(prevDoneFlags, doneFlags);

        // Save the current count 
        //resultCountsV = Avx2.BlendVariable(countsV, resultCountsV, justNowDone); // use First if Zero, second if 1
        resultCounts = _mm256_blendv_epi8(counts, resultCounts, justNowDone);
    }

    return compositeIsDone;
}
