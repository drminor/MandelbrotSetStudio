#include "pch.h"

#include "framework.h"
#include <immintrin.h>
#include "MSetGenerator.h"
#include "fp31VecMath.h"
#include "Iterator.h"

//#include "simd_aligned_allocator.h"
//
//typedef std::vector<__m256i, aligned_allocator<__m256i, sizeof(__m256i)> > aligned_vector;


#pragma region Constructor / Destructor

Iterator::Iterator(int limbCount, uint8_t bitsBeforeBp, int targetIterations, int thresholdForComparison)
{
    _limbCount = limbCount;

    //_vMath = new fp31VecMath(limbCount, bitsBeforeBp);

    _thresholdVector = _mm256_set1_epi32(thresholdForComparison);

    _targetIterationsVector = _mm256_set1_epi32(targetIterations);

    _zrSqrs = new aligned_vector(limbCount);
    _ziSqrs = new aligned_vector(limbCount);
    _sumOfSqrs = new aligned_vector(limbCount);

    _zRZiSqrs = new aligned_vector(limbCount);
    _tempVec = new aligned_vector(limbCount);

    _justOne = _mm256_set1_epi32(1);
}

Iterator::~Iterator()
{
    delete _zrSqrs;
    delete _ziSqrs;
    delete _sumOfSqrs;
    delete _zRZiSqrs;
    delete _tempVec;

    //delete _vMath;
}

#pragma endregion

bool Iterator::GenerateMapCol(aligned_vector* cr, aligned_vector* ciVec, __m256i& resultCounts, fp31VecMath vMath)
{
    __m256i haveEscapedFlags = _mm256_set1_epi32(0);

    __m256i doneFlags = _mm256_set1_epi32(0);

    resultCounts = _mm256_set1_epi32(0);
    __m256i counts = _mm256_set1_epi32(0);

    aligned_vector* zr = new aligned_vector(_limbCount);
    aligned_vector* zi = new aligned_vector(_limbCount);

    //__m256i* resultZr = _vh->createVec(limbCount);
    //__m256i* resultZi = _vh->createVec(limbCount);

    __m256i escapedFlagsVec = _mm256_set1_epi32(0);

    IterateFirstRound(cr, ciVec, zr, zi, escapedFlagsVec, vMath);
    int compositeIsDone = UpdateCounts(escapedFlagsVec, counts, resultCounts, doneFlags, haveEscapedFlags);

    while (compositeIsDone != -1)
    {
        Iterate(cr, ciVec, zr, zi, escapedFlagsVec, vMath);
        compositeIsDone = UpdateCounts(escapedFlagsVec, counts, resultCounts, doneFlags, haveEscapedFlags);
    }


    return true;
}

void Iterator::IterateFirstRound(aligned_vector* cr, aligned_vector* ci, aligned_vector* zr, aligned_vector* zi, __m256i& escapedFlagsVec, fp31VecMath vMath)
{
    //_vecHelper->copyVec(cr, zr, _limbCount);
    //_vecHelper->copyVec(ci, zi, _limbCount);

    vMath.Square(zr, _zrSqrs);
    vMath.Square(zi, _ziSqrs);

    vMath.Add(_zrSqrs, _ziSqrs, _sumOfSqrs);
    vMath.IsGreaterOrEqThan(_sumOfSqrs->at((size_t) _limbCount - 1), _thresholdVector, escapedFlagsVec);
}

void Iterator::Iterate(aligned_vector* cr, aligned_vector* ci, aligned_vector* zr, aligned_vector* zi, __m256i& escapedFlagsVec, fp31VecMath vMath)
{

    // square(z.r + z.i)
    //    _fp31VecMath.Add(zrs, zis, _temp);
    vMath.Add(zr, zi, _tempVec);

    //    _fp31VecMath.Square(_temp, _zRZiSqrs);
    vMath.Square(_tempVec, _zRZiSqrs);

    // z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd
    //    _fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
    vMath.Sub(_zRZiSqrs, _zrSqrs, zi);

    //    _fp31VecMath.Sub(zis, _zISqrs, _temp);
    vMath.Sub(zi, _ziSqrs, _tempVec);

    //    _fp31VecMath.Add(_temp, cis, zis);
    vMath.Add(_tempVec, ci, zi);

    // z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
    //    _fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
    vMath.Sub(_zrSqrs, _ziSqrs, _tempVec);
    
    //    _fp31VecMath.Add(_temp, crs, zrs);
    vMath.Add(_tempVec, cr, zr);

    //    _fp31VecMath.Square(zrs, _zRSqrs);
    //    _fp31VecMath.Square(zis, _zISqrs);
    //    _fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

    vMath.Square(zr, _zrSqrs);
    vMath.Square(zi, _ziSqrs);
    vMath.Add(_zrSqrs, _ziSqrs, _sumOfSqrs);

    //    _fp31VecMath.IsGreaterOrEqThan(ref _sumOfSqrs[^ 1], ref _thresholdVector, ref escapedFlagsVec);
    vMath.IsGreaterOrEqThan(_sumOfSqrs->at(_limbCount - 1), _thresholdVector, escapedFlagsVec);

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
