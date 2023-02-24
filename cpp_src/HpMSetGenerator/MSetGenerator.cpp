#include "pch.h"

#include "framework.h"
#include <immintrin.h>

#include "MSetGenerator.h"
#include "fp31VecMath.h"
#include "Iterator.h"

#include <iostream>


typedef struct _MSETREQ
{
    int RowNumber;

    // ApFixedPointFormat
    int BitsBeforeBinaryPoint;
    int LimbCount;
    int NumberOfFractionalBits;
    int TotalBits;
    int TargetExponent;

    int Lanes;
    int VectorsPerRow;

    // Subdivision
    //char* subdivisionId;

    // BlockSize
    int blockSizeWidth;
    int blockSizeHeight;

    // MapCalcSettings;
    int maxIterations;
    int thresholdForComparison;
    int iterationsPerStep;

} MSETREQ;



extern "C"
{
    __declspec(dllexport) int GenerateMapSectionRow(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        int stride = mapSectionRequest.blockSizeWidth;
        int limbCount = mapSectionRequest.LimbCount;
        uint8_t bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetIterations = mapSectionRequest.maxIterations;
        int thresholdForComparison = mapSectionRequest.thresholdForComparison;

        _RPTA("Generating MapSectionRow with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        bool allRowSamplesHaveEscaped = true;
        //int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        //Iterator* _iterator = new Iterator(limbCount, bitsBeforeBp, targetIterations, thresholdForComparison);

        //VecHelper* _vh = new VecHelper();
        //__m256i* cr = _vh->createVec(limbCount);

        //for (int idx = 0; idx < vectorsPerRow; idx++)
        //{
        //    int vPtr = idx * limbCount;

        //    for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
        //    {
        //        cr[limbPtr] = crsForARow[vPtr++];
        //    }

        //    __m256i countsVec = countsForARow[idx];
        //	bool allSamplesHaveEscaped = _iterator->GenerateMapCol(cr, ciVec, countsVec);

        //    // Update the caller's counts
        //    countsForARow[idx] = countsVec;

        //	if (!allSamplesHaveEscaped)
        //	{
        //		allRowSamplesHaveEscaped = false;
        //	}
        //}

        //_vh->freeVec(cr);
        //delete _vh;
        //delete _iterator;

        return allRowSamplesHaveEscaped ? 1 : 0;
    }

    __declspec(dllexport) int BaseSimdTest(MSETREQ mapSectionRequest, int* countsForARow)
    {
        _RPTA("Running BaseSimdTest");

        typedef std::vector<__m128, aligned_allocator<__m128, sizeof(__m128)> > aligned_vector;
        aligned_vector lhs;
        aligned_vector rhs;
        
        float a = 1.0f;
        float b = 2.0f;
        float c = 3.0f;
        float d = 4.0f;
        
        float e = 5.0f;
        float f = 6.0f;
        float g = 7.0f;
        float h = 8.0f;
        
        for (std::size_t i = 0; i < 1000; ++i)
        {
        	lhs.push_back(_mm_set_ps(a, b, c, d));
        	rhs.push_back(_mm_set_ps(e, f, g, h));
        
        	a += 1.0f; b += 1.0f; c += 1.0f; d += 1.0f;
        	e += 1.0f; f += 1.0f; g += 1.0f; h += 1.0f;
        }
        
        __m128 mul = _mm_mul_ps(lhs[10], rhs[10]);

        return 0;
    }

    __declspec(dllexport) int BaseSimdTest2(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        typedef std::vector<__m256i, aligned_allocator<__m256i, sizeof(__m256i)> > aligned_vector;

        //int stride = mapSectionRequest.blockSizeWidth;
        int limbCount = mapSectionRequest.LimbCount;
        //uint8_t bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetIterations = mapSectionRequest.maxIterations;
        //int thresholdForComparison = mapSectionRequest.thresholdForComparison;

        _RPTA("Running BaseSimdTest2 with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        //Iterator* _iterator = new Iterator(limbCount, bitsBeforeBp, targetIterations, thresholdForComparison);

        aligned_vector* ci = new aligned_vector(limbCount);
        for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
        {
            ci->push_back(ciVec[limbPtr]);
        }

        aligned_vector* cr = new aligned_vector(limbCount);

        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0, h = 0;

        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
            {
                cr->push_back(crsForARow[vPtr]);
            }

            //__m256i countsVec = countsForARow[idx];
            //bool allSamplesHaveEscaped = _iterator->GenerateMapCol(cr, ci, countsVec);

            a += 14; b += 1; c += 1; d += 1;
            e += 2; f += 3; g += 4; h += 5;


            bool allSamplesHaveEscaped = false;

            // Update the caller's counts
            //countsForARow[idx] = countsVec;

            countsForARow[idx] = _mm256_set_epi32(a, b, c, d, e, f, g, h);

            cr->clear();

            if (!allSamplesHaveEscaped)
            {
                allRowSamplesHaveEscaped = false;
            }
        }

        delete cr;
        delete ci;

        return allRowSamplesHaveEscaped ? 1 : 0;

        //__m256i epi32_vec_2 = counts[0];
        //__m256i epi32_vec_3 = counts[1];
        //__m256i epi32_resultB = _mm256_add_epi32(epi32_vec_2, epi32_vec_3);
        //uint32_t* i = (uint32_t*)&epi32_resultB;
        //_RPTA("int:\t\t%d, %d, %d, %d, %d, %d, %d, %d\n", i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7]);
    }

    __declspec(dllexport) int BaseSimdTest3(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        typedef std::vector<__m256i, aligned_allocator<__m256i, sizeof(__m256i)> > aligned_vector;

        int limbCount = mapSectionRequest.LimbCount;
        uint8_t bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetIterations = mapSectionRequest.maxIterations;
        int thresholdForComparison = mapSectionRequest.thresholdForComparison;

        _RPTA("Running BaseSimdTest3 with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        fp31VecMath vMath = fp31VecMath(limbCount, bitsBeforeBp);
        Iterator iterator = Iterator(limbCount, bitsBeforeBp, targetIterations, thresholdForComparison);

        aligned_vector* ci = new aligned_vector(limbCount);
        for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
        {
            ci->push_back(ciVec[limbPtr]);
        }

        aligned_vector* cr = new aligned_vector(limbCount);

        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
            {
                cr->push_back(crsForARow[vPtr]);
            }

            __m256i countsVec = countsForARow[idx];
            bool allSamplesHaveEscaped = iterator.GenerateMapCol(cr, ci, countsVec, vMath);

            //bool allSamplesHaveEscaped = false;

            // Update the caller's counts
            //countsForARow[idx] = countsVec;

            countsForARow[idx] = _mm256_set1_epi32(1);

            cr->clear();

            if (!allSamplesHaveEscaped)
            {
                allRowSamplesHaveEscaped = false;
            }
        }

        delete cr;
        delete ci;
        //delete iterator;

        return allRowSamplesHaveEscaped ? 1 : 0;
    }

}