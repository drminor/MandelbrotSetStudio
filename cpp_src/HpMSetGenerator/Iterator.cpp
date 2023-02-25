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

Iterator::Iterator(fp31VecMath* const vMath, int targetIterations, int thresholdForComparison)
{
    _vMath = vMath;

    _thresholdVector = _mm256_set1_epi32(thresholdForComparison);

    _targetIterationsVector = _mm256_set1_epi32(targetIterations);

    _zrSqrs = new aligned_vector(_vMath->LimbCount);
    _ziSqrs = new aligned_vector(_vMath->LimbCount);
    _sumOfSqrs = new aligned_vector(_vMath->LimbCount);

    _zRZiSqrs = new aligned_vector(_vMath->LimbCount);
    _tempVec = new aligned_vector(_vMath->LimbCount);

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

bool Iterator::GenerateMapCol(aligned_vector* cr, aligned_vector* ciVec, __m256i& resultCounts)
{
    __m256i haveEscapedFlags = _mm256_set1_epi32(0);

    __m256i doneFlags = _mm256_set1_epi32(0);

    resultCounts = _mm256_set1_epi32(0);
    __m256i counts = _mm256_set1_epi32(0);

    aligned_vector* zr = new aligned_vector(_vMath->LimbCount);
    aligned_vector* zi = new aligned_vector(_vMath->LimbCount);

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

void Iterator::IterateFirstRound(aligned_vector* cr, aligned_vector* ci, aligned_vector* zr, aligned_vector* zi, __m256i& escapedFlagsVec)
{
    for (int i = 0; i < _vMath->LimbCount; i++) {
        zr->at(i) = cr->at(i);
        zi->at(i) = ci->at(i);
    }
    //_vecHelper->copyVec(cr, zr, _limbCount);
    //_vecHelper->copyVec(ci, zi, _limbCount);

    _vMath->Square(zr, _zrSqrs);
    _vMath->Square(zi, _ziSqrs);

    _vMath->Add(_zrSqrs, _ziSqrs, _sumOfSqrs);
    _vMath->IsGreaterOrEqThan(_sumOfSqrs->at((size_t)_vMath->LimbCount - 1), _thresholdVector, escapedFlagsVec);
}

void Iterator::Iterate(aligned_vector* cr, aligned_vector* ci, aligned_vector* zr, aligned_vector* zi, __m256i& escapedFlagsVec)
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
    _vMath->IsGreaterOrEqThan(_sumOfSqrs->at((size_t)_vMath->LimbCount - 1), _thresholdVector, escapedFlagsVec);

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
    doneFlags = _mm256_or_si256(hasEscapedFlags, targetReachedCompVec);

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
