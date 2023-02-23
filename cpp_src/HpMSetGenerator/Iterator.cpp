#include "pch.h"

#include "framework.h"
#include <immintrin.h>
#include "MSetGenerator.h"
#include "VecHelper.h"
#include "fp31VecMath.h"
#include "Iterator.h"


#pragma region Constructor / Destructor

Iterator::Iterator(int limbCount, uint8_t bitsBeforeBp)
{
    _limbCount = limbCount;

    _vecHelper = new VecHelper();
    _vMath = new fp31VecMath(limbCount, bitsBeforeBp);

    _threshold = 4;
    _thresholdVector = _mm256_set1_epi32(_threshold);

    _zrSqrs = _vecHelper->createVec(limbCount);
    _ziSqrs = _vecHelper->createVec(limbCount);
    _sumOfSqrs = _vecHelper->createVec(limbCount);

    _zRZiSqrs = _vecHelper->createVec(limbCount);
    _tempVec = _vecHelper->createVec(limbCount);
}

Iterator::~Iterator()
{
    _vecHelper->freeVec(_zrSqrs);
    _vecHelper->freeVec(_ziSqrs);
    _vecHelper->freeVec(_sumOfSqrs);
    _vecHelper->freeVec(_zRZiSqrs);
    _vecHelper->freeVec(_tempVec);

    delete _vecHelper;
    delete _vMath;
}

#pragma endregion

bool Iterator::GenerateMapCol(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i& counts)
{
    __m256i haveEscapedFlags = _mm256_set1_epi32(0);

    //__m256i doneFlags = _mm256_set1_epi32(0);

    counts = _mm256_set1_epi32(0);
    __m256i resultCounts = _mm256_set1_epi32(0);

    VecHelper* _vh = new VecHelper();
    __m256i* zr = _vh->createVec(limbCount);
    __m256i* zi = _vh->createVec(limbCount);
    //__m256i* resultZr = _vh->createVec(limbCount);
    //__m256i* resultZi = _vh->createVec(limbCount);

    __m256i escapedFlags = _mm256_set1_epi32(0);

    IterateFirstRound(cr, ci, zr, zi, escapedFlags);
    int compositeIsDone = UpdateCounts(limbCount, escapedFlags, counts, haveEscapedFlags);

    while (compositeIsDone != -1)
    {
        Iterate(cr, ci, zr, zi, escapedFlags);
        compositeIsDone = UpdateCounts(limbCount, escapedFlags, counts, haveEscapedFlags);
    }

    _vh->freeVec(zr);
    _vh->freeVec(zi);
    delete _vh;

    return true;
}

void Iterator::IterateFirstRound(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags)
{
    int threshold = 4;
    __m256i thresholdVector = _mm256_set1_epi32(threshold);

    //VecHelper* _vh = new VecHelper();
    
    //__m256i* zrSqrs = _vh->createVec(limbCount);
    //__m256i* ziSqrs = _vh->createVec(limbCount);
    //__m256i* sumOfSqrs = _vh->createVec(limbCount);

    //__m256i* zRZiSqrs = _vh->createVec(limbCount);
    //__m256i* tempVec = _vh->createVec(limbCount);

    _vecHelper->copyVec(cr, zr, _limbCount);
    _vecHelper->copyVec(ci, zi, _limbCount);

    //fp31VecMath* _vMath = new fp31VecMath(limbCount, 8);

    _vMath->Square(zr, _zrSqrs);
    _vMath->Square(zi, _ziSqrs);

    _vMath->Add(_zrSqrs, _ziSqrs, _sumOfSqrs);
    _vMath->IsGreaterOrEqThan(_sumOfSqrs[_limbCount - 1], thresholdVector, escapedFlags);
}

void Iterator::Iterate(__m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags)
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
    _vMath->IsGreaterOrEqThan(_sumOfSqrs[_limbCount - 1], _thresholdVector, escapedFlags);

}

int Iterator::UpdateCounts(int limbCount, __m256i escapedFlags, __m256i& counts, __m256i& haveEscapedFlags)
{
    //counts = Avx2.Add(counts, _justOne);

    //// Apply the new escapedFlags, only if the doneFlags is false for each vector position
    //hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec, hasEscapedFlagsV, doneFlagsV);

    //// Compare the new Counts with the TargetIterations
    //var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, targetIterationsV);

    //var prevDoneFlagsV = doneFlagsV;

    //// If escaped or reached the target iterations, we're done 
    //doneFlagsV = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec);

    //var compositeIsDone = Avx2.MoveMask(doneFlagsV.AsByte());
    //var prevCompositeIsDone = Avx2.MoveMask(prevDoneFlagsV.AsByte());

    //if (compositeIsDone != prevCompositeIsDone)
    //{
    //    var justNowDone = Avx2.CompareEqual(prevDoneFlagsV, doneFlagsV);

    //    // Save the current count 
    //    resultCountsV = Avx2.BlendVariable(countsV, resultCountsV, justNowDone); // use First if Zero, second if 1

    //    // Save the current ZValues.
    //    for (var limbPtr = 0; limbPtr < _resultZrs.Length; limbPtr++)
    //    {
    //        resultZRs[limbPtr] = Avx2.BlendVariable(zRs[limbPtr].AsInt32(), resultZRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
    //        resultZIs[limbPtr] = Avx2.BlendVariable(zIs[limbPtr].AsInt32(), resultZIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
    //    }
    //}

    //return compositeIsDone;


    int result = -1;

    return result;
}
