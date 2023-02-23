#include "pch.h"

#include "framework.h"
#include <immintrin.h>
#include "MSetGenerator.h"
#include "VecHelper.h"
#include "fp31VecMath.h"
#include "Iterator.h"

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

    IterateFirstRound(stride, limbCount, cr, ci, zr, zi, escapedFlags);
    int compositeIsDone = UpdateCounts(limbCount, escapedFlags, counts, haveEscapedFlags);

    while (compositeIsDone != -1)
    {
        Iterate(stride, limbCount, cr, ci, zr, zi, escapedFlags);
        compositeIsDone = UpdateCounts(limbCount, escapedFlags, counts, haveEscapedFlags);
    }

    _vh->freeVec(zr);
    _vh->freeVec(zi);
    delete _vh;

    return true;
}

__m256i Iterator::IterateFirstRound(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags)
{
    int threshold = 4;
    __m256i thresholdVector = _mm256_set1_epi32(threshold);

    VecHelper* _vh = new VecHelper();

    __m256i* zrSqrs = _vh->createVec(limbCount);
    __m256i* ziSqrs = _vh->createVec(limbCount);
    __m256i* sumOfSqrs = _vh->createVec(limbCount);

    __m256i* zRZiSqrs = _vh->createVec(limbCount);
    __m256i* tempVec = _vh->createVec(limbCount);

    _vh->copyVec(cr, zr, limbCount);
    _vh->copyVec(ci, zi, limbCount);

    fp31VecMath* _vMath = new fp31VecMath();

    _vMath->Square(zr, zrSqrs);
    _vMath->Square(zi, ziSqrs);

    //    _fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);
    //    _fp31VecMath.IsGreaterOrEqThan(ref _sumOfSqrs[^ 1], ref _thresholdVector, ref escapedFlagsVec);

    __m256i result = _mm256_set1_epi32(0);


    _vh->freeVec(zrSqrs);
    _vh->freeVec(ziSqrs);
    _vh->freeVec(sumOfSqrs);
    _vh->freeVec(zRZiSqrs);
    _vh->freeVec(tempVec);
    delete _vh;


    return result;
}

__m256i Iterator::Iterate(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags)
{

    //try
    //{
    //    // square(z.r + z.i)
    //    _fp31VecMath.Add(zrs, zis, _temp);
    //    _fp31VecMath.Square(_temp, _zRZiSqrs);

    //    // z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
    //    _fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
    //    _fp31VecMath.Sub(zis, _zISqrs, _temp);
    //    _fp31VecMath.Add(_temp, cis, zis);

    //    // z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
    //    _fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
    //    _fp31VecMath.Add(_temp, crs, zrs);

    //    _fp31VecMath.Square(zrs, _zRSqrs);
    //    _fp31VecMath.Square(zis, _zISqrs);
    //    _fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

    //    _fp31VecMath.IsGreaterOrEqThan(ref _sumOfSqrs[^ 1], ref _thresholdVector, ref escapedFlagsVec);
    //}
    //catch (Exception e)
    //{
    //    Debug.WriteLine($"Iterator received exception: {e}.");
    //    throw;
    //}

    __m256i result = _mm256_set1_epi32(0);
    return result;
}

int Iterator::UpdateCounts(int limbCount, __m256i escapedFlags, __m256i& counts, __m256i& haveEscapedFlags)
{
    int result = -1;

    return result;
}
