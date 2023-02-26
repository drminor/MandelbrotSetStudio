#include "pch.h"

#include "framework.h"
#include <immintrin.h>

#include "MSetGenerator.h"
#include "Fp31VecMath.h"
#include "Iterator.h"

#include <iostream>


typedef struct _MSETREQ
{
    // BlockSize
    int BlockSizeWidth;
    int BlockSizeHeight;

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

    // The row to calculate
    int RowNumber;

    // MapCalcSettings;
    int TargetIterations;
    int ThresholdForComparison;
    int iterationsPerStep;

} MSETREQ;

#pragma warning( push )
#pragma warning( disable : 4316 )
__m256i* CreateLimbSet(int limbCount) {
    return new __m256i[limbCount];
}
#pragma warning( pop )

extern "C"
{
    __declspec(dllexport) int GenerateMapSectionRow(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        int limbCount = mapSectionRequest.LimbCount;
        int bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetExponent = mapSectionRequest.TargetExponent;

        int targetIterations = mapSectionRequest.TargetIterations;
        int thresholdForComparison = mapSectionRequest.ThresholdForComparison;

        _RPTA("Generating a MapSectionRow with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        Fp31VecMath vMath = Fp31VecMath(limbCount, bitsBeforeBp, targetExponent);
        Iterator iterator = Iterator(&vMath, targetIterations, thresholdForComparison);

        __m256i* ci = CreateLimbSet(limbCount);
        for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
        {
            //ci->push_back(ciVec[limbPtr]);

            __m256i ciLimb = _mm256_loadu_si256((__m256i const*) (&ciVec[limbPtr]));
            ci[limbPtr] = ciLimb;
        }

        __m256i* cr = CreateLimbSet(limbCount);

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
            {
                __m256i crLimb = _mm256_loadu_si256((__m256i const*) (&crsForARow[vPtr + limbCount]));
                cr[limbPtr] = crLimb;
            }

            //__m256i countsVec = countsForARow[idx];

            //ymm = _mm256_loadu_si256((__m256i const*)p);
            __m256i countsVec = _mm256_loadu_si256((__m256i const*) (&countsForARow[idx]));

            bool allSamplesHaveEscaped = iterator.GenerateMapCol(cr, ci, countsVec);

            //countsForARow[idx] = countsVec;
            _mm256_storeu_si256((__m256i*)(&countsForARow[idx]), countsVec);

            if (!allSamplesHaveEscaped)
            {
                allRowSamplesHaveEscaped = false;
            }
        }

        delete[] ci;
        delete[] cr;

        return allRowSamplesHaveEscaped ? 1 : 0;
    }

    __declspec(dllexport) int BaseSimdTest(MSETREQ mapSectionRequest, int* countsForARow)
    {
        _RPTA("\n\nRunning BaseSimdTest\n");

        __m128* lhs = new __m128[100];
        __m128* rhs = new __m128[100];
        
        float a = 1.0f;
        float b = 2.0f;
        float c = 3.0f;
        float d = 4.0f;
        
        float e = 5.0f;
        float f = 6.0f;
        float g = 7.0f;
        float h = 8.0f;
        
        for (std::size_t i = 0; i < 100; ++i)
        {
            lhs[i] = _mm_set_ps(a, b, c, d);
            rhs[i] = _mm_set_ps(e, f, g, h);
        
        	a += 1.0f; b += 1.0f; c += 1.0f; d += 1.0f;
        	e += 1.0f; f += 1.0f; g += 1.0f; h += 1.0f;
        }
        
        float* lhsFVals = (float*)(& lhs[10]);
        _RPTA("lhs floats:\t\t%f, %f, %f, %f\n", lhsFVals[0], lhsFVals[1], lhsFVals[2], lhsFVals[3]);

        float* rhsFVals = (float*)(&rhs[10]);
        _RPTA("lhs floats:\t\t%f, %f, %f, %f\n", rhsFVals[0], rhsFVals[1], rhsFVals[2], rhsFVals[3]);

        __m128 mul = _mm_mul_ps(lhs[10], rhs[10]);
        float* resultFVals = (float*)&mul;
        _RPTA("floats:\t\t%f, %f, %f, %f\n", resultFVals[0], resultFVals[1], resultFVals[2], resultFVals[3]);

        delete[] lhs;
        delete[] rhs;
        return 0;
    }

    __declspec(dllexport) int BaseSimdTest2(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        //int stride = mapSectionRequest.blockSizeWidth;
        int limbCount = mapSectionRequest.LimbCount;
        //uint8_t bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetIterations = mapSectionRequest.TargetIterations;
        //int thresholdForComparison = mapSectionRequest.thresholdForComparison;

        _RPTA("\n\nRunning BaseSimdTest2 with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        //Iterator* _iterator = new Iterator(limbCount, bitsBeforeBp, targetIterations, thresholdForComparison);

        __m256i* ci = CreateLimbSet(limbCount);

        for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
        {
            ci[limbPtr] = ciVec[limbPtr];
        }

        __m256i* cr = CreateLimbSet(limbCount);


        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0, h = 0;

        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
            {
                cr[limbPtr] = crsForARow[vPtr];
            }

            //__m256i countsVec = countsForARow[idx];
            //bool allSamplesHaveEscaped = _iterator->GenerateMapCol(cr, ci, countsVec);

            a += 14; b += 1; c += 1; d += 1;
            e += 2; f += 3; g += 4; h += 5;


            bool allSamplesHaveEscaped = false;

            // Update the caller's counts
            //countsForARow[idx] = countsVec;

            countsForARow[idx] = _mm256_set_epi32(a, b, c, d, e, f, g, h);

            //cr->clear();

            if (!allSamplesHaveEscaped)
            {
                allRowSamplesHaveEscaped = false;
            }
        }

        delete[] cr;
        delete[] ci;

        return allRowSamplesHaveEscaped ? 1 : 0;

        //__m256i epi32_vec_2 = counts[0];
        //__m256i epi32_vec_3 = counts[1];
        //__m256i epi32_resultB = _mm256_add_epi32(epi32_vec_2, epi32_vec_3);
        //uint32_t* i = (uint32_t*)&epi32_resultB;
        //_RPTA("int:\t\t%d, %d, %d, %d, %d, %d, %d, %d\n", i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7]);
    }

    __declspec(dllexport) int BaseSimdTest3(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        int limbCount = mapSectionRequest.LimbCount;
        int bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetIterations = mapSectionRequest.TargetIterations;

        int targetExponent = mapSectionRequest.TargetExponent;
        int thresholdForComparison = mapSectionRequest.ThresholdForComparison;

        _RPTA("\n\nRunning BaseSimdTest3 with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        Fp31VecMath vMath = Fp31VecMath(limbCount, bitsBeforeBp, targetExponent);
        Iterator iterator = Iterator(&vMath, targetIterations, thresholdForComparison);

        __m256i* ci = CreateLimbSet(limbCount);
        for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
        {
            //ci->push_back(ciVec[limbPtr]);

            __m256i ciLimb = _mm256_loadu_si256((__m256i const*) (&ciVec[limbPtr]));
            ci[limbPtr] = ciLimb;
        }

        __m256i* cr = CreateLimbSet(limbCount);

        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (int limbPtr = 0; limbPtr < limbCount; limbPtr++)
            {
                cr[limbPtr] = crsForARow[vPtr];
            }

            __m256i countsVec = countsForARow[idx];
            bool allSamplesHaveEscaped = iterator.GenerateMapCol(cr, ci, countsVec);

            countsForARow[idx] = countsVec;

            //cr->clear();

            if (!allSamplesHaveEscaped)
            {
                allRowSamplesHaveEscaped = false;
            }
        }

        delete[] cr;
        delete[] ci;
        //delete iterator;

        return allRowSamplesHaveEscaped ? 1 : 0;
    }

    __declspec(dllexport) int BaseSimdTest4(MSETREQ mapSectionRequest, __m256i* crsForARow, __m256i* ciVec, __m256i* countsForARow)
    {
        int limbCount = mapSectionRequest.LimbCount;
        int bitsBeforeBp = mapSectionRequest.BitsBeforeBinaryPoint;
        int targetIterations = mapSectionRequest.TargetIterations;

        int targetExponent = mapSectionRequest.TargetExponent;
        int thresholdForComparison = mapSectionRequest.ThresholdForComparison;

        _RPTA("\n\nRunning BaseSimdTest3 with LimbCount: %d and Target Iterations: %d\n", limbCount, targetIterations);

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        __m256i countsVec = _mm256_set_epi32(10, 1000, 10000, 15000, 30000, 10, 10, 10);

        countsForARow[0] = countsVec;

        countsForARow[1] = countsVec;

        return 0;
    }

    /*
    

    // Constructor to broadcast the same value into all elements
    // Removed because of undesired implicit conversions:
    //Vec256b(int i) {ymm = _mm256_set1_epi32(-(i & 1));}

    // Constructor to build from two Vec128b:
    Vec256b(Vec128b const a0, Vec128b const a1) {
        ymm = set_m128ir(a0, a1);
    }
    // Constructor to convert from type __m256i used in intrinsics:
    Vec256b(__m256i const x) {
        ymm = x;
    }
    // Assignment operator to convert from type __m256i used in intrinsics:
    Vec256b & operator = (__m256i const x) {
        ymm = x;
        return *this;
    }
    // Type cast operator to convert to __m256i used in intrinsics
    operator __m256i() const {
        return ymm;
    }
    // Member function to load from array (unaligned)
    Vec256b & load(void const * p) {
        ymm = _mm256_loadu_si256((__m256i const*)p);
        return *this;
    }
    // Member function to load from array, aligned by 32
    // You may use load_a instead of load if you are certain that p points to an address
    // divisible by 32, but there is hardly any speed advantage of load_a on modern processors
    Vec256b & load_a(void const * p) {
        ymm = _mm256_load_si256((__m256i const*)p);
        return *this;
    }
    // Member function to store into array (unaligned)
    void store(void * p) const {
        _mm256_storeu_si256((__m256i*)p, ymm);
    }
    // Member function storing into array, aligned by 32
    // You may use store_a instead of store if you are certain that p points to an address
    // divisible by 32, but there is hardly any speed advantage of load_a on modern processors
    void store_a(void * p) const {
        _mm256_store_si256((__m256i*)p, ymm);
    }
    // Member function storing to aligned uncached memory (non-temporal store).
    // This may be more efficient than store_a when storing large blocks of memory if it
    // is unlikely that the data will stay in the cache until it is read again.
    // Note: Will generate runtime error if p is not aligned by 32
    void store_nt(void * p) const {
        _mm256_stream_si256((__m256i*)p, ymm);
    }
    // Member functions to split into two Vec128b:
    Vec128b get_low() const {
        return _mm256_castsi256_si128(ymm);
    }
    Vec128b get_high() const {
        return _mm256_extractf128_si256(ymm,1);
    }
    
        // Member functions to split into two Vec4ui:
    Vec4ui get_low() const {
        return _mm256_castsi256_si128(ymm);
    }
    Vec4ui get_high() const {
        return _mm256_extractf128_si256(ymm,1);
    }
    
    */


    /*
    
    
class Vec8ui : public Vec8i {
public:
    // Default constructor:
    Vec8ui() = default;
    // Constructor to broadcast the same value into all elements:
    Vec8ui(uint32_t i) {
        ymm = _mm256_set1_epi32((int32_t)i);
    }
    // Constructor to build from all elements:
    Vec8ui(uint32_t i0, uint32_t i1, uint32_t i2, uint32_t i3, uint32_t i4, uint32_t i5, uint32_t i6, uint32_t i7) {
        ymm = _mm256_setr_epi32((int32_t)i0, (int32_t)i1, (int32_t)i2, (int32_t)i3, (int32_t)i4, (int32_t)i5, (int32_t)i6, (int32_t)i7);
    }
    // Constructor to build from two Vec4ui:
    Vec8ui(Vec4ui const a0, Vec4ui const a1) {
        ymm = set_m128ir(a0, a1);
    }
    // Constructor to convert from type __m256i used in intrinsics:
    Vec8ui(__m256i const x) {
        ymm = x;
    }
    // Assignment operator to convert from type __m256i used in intrinsics:
    Vec8ui & operator = (__m256i const x) {
        ymm = x;
        return *this;
    }
    // Member function to load from array (unaligned)
    Vec8ui & load(void const * p) {
        ymm = _mm256_loadu_si256((__m256i const*)p);
        return *this;
    }
    // Member function to load from array, aligned by 32
    Vec8ui & load_a(void const * p) {
        ymm = _mm256_load_si256((__m256i const*)p);
        return *this;
    }
    // Member function to change a single element in vector
    Vec8ui const insert(int index, uint32_t value) {
        Vec8i::insert(index, (int32_t)value);
        return *this;
    }
    // Member function extract a single element from vector
    uint32_t extract(int index) const {
        return (uint32_t)Vec8i::extract(index);
    }
    // Extract a single element. Use store function if extracting more than one element.
    // Operator [] can only read an element, not write.
    uint32_t operator [] (int index) const {
        return extract(index);
    }
    // Member functions to split into two Vec4ui:
    Vec4ui get_low() const {
        return _mm256_castsi256_si128(ymm);
    }
    Vec4ui get_high() const {
        return _mm256_extractf128_si256(ymm,1);
    }
    static constexpr int elementtype() {
        return 9;
    }
};
    
    
    
    
    
    
    
    */

}