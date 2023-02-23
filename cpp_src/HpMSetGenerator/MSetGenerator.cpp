#include "pch.h"

#include "framework.h"
#include <immintrin.h>

#include "MSetGenerator.h"
#include "VecHelper.h"
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
    char* subdivisionId;

    // BlockSize
    int blockSizeWidth;
    int blockSizeHeight;

    // MapCalcSettings;
    int maxIterations;
    int threshold;
    int iterationsPerStep;

} MSETREQ;

extern "C"
{
    __declspec(dllexport) bool GenerateMapSection(MSETREQ mapSectionRequest, __m256i* crForARow, __m256i* ci, __m256i* countsForARow)
    {
        int targetCount = mapSectionRequest.maxIterations;
        int stride = mapSectionRequest.blockSizeWidth;
        int limbCount = mapSectionRequest.LimbCount;
        uint8_t bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;

        std::cout << std::endl << "Generating MapSection with LimbCount: " << limbCount << " and Target Count:" << targetCount << std::endl;

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        Iterator* _iterator = new Iterator(limbCount, bitsBeforeBp);

        VecHelper* _vh = new VecHelper();
        __m256i* cr = _vh->createVec(limbCount);

        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
            {
                cr[limbPtr] = crForARow[vPtr++];
            }

            __m256i countsVec = countsForARow[idx];
        	bool allSamplesHaveEscaped = _iterator->GenerateMapCol(stride, limbCount, cr, ci, countsVec);

        	if (!allSamplesHaveEscaped)
        	{
        		allRowSamplesHaveEscaped = false;
        	}
        }

        _vh->freeVec(cr);
        delete _vh;
        delete _iterator;

        return allRowSamplesHaveEscaped;

        //__m256i epi32_vec_2 = counts[0];
        //__m256i epi32_vec_3 = counts[1];
        //__m256i epi32_resultB = _mm256_add_epi32(epi32_vec_2, epi32_vec_3);
        //uint32_t* i = (uint32_t*)&epi32_resultB;
        //_RPTA("int:\t\t%d, %d, %d, %d, %d, %d, %d, %d\n", i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7]);
    }
}