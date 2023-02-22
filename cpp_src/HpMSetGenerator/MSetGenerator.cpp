#include "pch.h"
#include "MSetGenerator.h"

__m256i IterateFirstRound(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags)
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

__m256i Iterate(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i* zr, __m256i* zi, __m256i& escapedFlags)
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

int UpdateCounts(int limbCount, __m256i escapedFlags, __m256i& counts, __m256i& haveEscapedFlags)
{
    int result = -1;

    return result;
}

bool GenerateMapCol(int stride, int limbCount, __m256i* cr, __m256i* ci, __m256i& counts)
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

extern "C"
{
    __declspec(dllexport) bool GenerateMapSection(MSETREQ mapSectionRequest, __m256i* crForARow, __m256i* ci, __m256i* countsForARow)
    {
        int targetCount = mapSectionRequest.maxIterations;
        int stride = mapSectionRequest.blockSizeWidth;
        int limbCount = mapSectionRequest.LimbCount;

        std::cout << std::endl << "Generating MapSection with LimbCount: " << limbCount << " and Target Count:" << targetCount << std::endl;

        bool allRowSamplesHaveEscaped = true;
        int vectorsPerRow = mapSectionRequest.VectorsPerRow;

        VecHelper* _vh = new VecHelper();
        __m256i* cr = _vh->createVec(limbCount);


        for (int idx = 0; idx < vectorsPerRow; idx++)
        {
            int vPtr = idx * limbCount;

            for (lPtr = 0; lPtr < limbCount; lPtr++)
            {
                cr[lptr] = crForARow[vPtr++];
            }

            __m256i countsVec = countsForARow[idx];
        	bool allSamplesHaveEscaped = GenerateMapCol(stride, limbCount, cr, ci, countsVec);

        	if (!allSamplesHaveEscaped)
        	{
        		allRowSamplesHaveEscaped = false;
        	}
        }

        return allRowSamplesHaveEscaped;

        //__m256i epi32_vec_2 = counts[0];
        //__m256i epi32_vec_3 = counts[1];
        //__m256i epi32_resultB = _mm256_add_epi32(epi32_vec_2, epi32_vec_3);
        //uint32_t* i = (uint32_t*)&epi32_resultB;
        //_RPTA("int:\t\t%d, %d, %d, %d, %d, %d, %d, %d\n", i[0], i[1], i[2], i[3], i[4], i[5], i[6], i[7]);
    }

    __declspec(dllexport) void GetStringValues(MSETREQ mapSectionRequest, char** px, char** py, char** deltaW, char** deltaH)
    {
        //qpMath* m = new qpMath();
        //qp posX = m->fromLongRational(mapSectionRequest.positionX[0], mapSectionRequest.positionX[1], mapSectionRequest.positionExponent);
        //qp posY = m->fromLongRational(mapSectionRequest.positionY[0], mapSectionRequest.positionY[1], mapSectionRequest.positionExponent);

        //qp deltaWidth = m->fromLongRational(mapSectionRequest.samplePointDeltaWidth[0], mapSectionRequest.samplePointDeltaWidth[1], mapSectionRequest.samplePointDeltaExponent);
        //qp deltaHeight = m->fromLongRational(mapSectionRequest.samplePointDeltaHeight[0], mapSectionRequest.samplePointDeltaHeight[1], mapSectionRequest.samplePointDeltaExponent);
        //delete m;

        //qpParser* p = new qpParser();
        //std::string sPx = p->ToStr(posX);
        //std::string sPy = p->ToStr(posY);

        //std::string sDw = p->ToStr(deltaWidth);
        //std::string sDh = p->ToStr(deltaHeight);
        //delete p;

        //const char* cPx = sPx.c_str();
        //const char* cPy = sPy.c_str();
        //const char* cDeltaW = sDw.c_str();
        //const char* cDeltaH = sDh.c_str();

        //*px = (char*)::CoTaskMemAlloc(strlen(cPx) + 1);
        //if (*px != 0) {
        //    strcpy_s(*px, strlen(cPx) + 1, cPx);
        //}

        //*py = (char*)::CoTaskMemAlloc(strlen(cPy) + 1);
        //if (*py != 0) {
        //    strcpy_s(*py, strlen(cPy) + 1, cPy);
        //}

        //*deltaW = (char*)::CoTaskMemAlloc(strlen(cDeltaW) + 1);
        //if (*deltaW != 0) {
        //    strcpy_s(*deltaW, strlen(cDeltaW) + 1, cDeltaW);
        //}

        //*deltaH = (char*)::CoTaskMemAlloc(strlen(cDeltaH) + 1);
        //if (*deltaH != 0) {
        //    strcpy_s(*deltaH, strlen(cDeltaH) + 1, cDeltaH);
        //}


        //std::cout << "posX is " << px << " and deltaWidth is " << dw;
    }

    bool GenerateMapSectionOld(MSETREQ mapSectionRequest, __m256i* counts)
    {
        //int cellCount = mapSectionRequest.blockSizeWidth * mapSectionRequest.blockSizeHeight;

        ////for (int i = 0; i < cellCount; i++)
        ////{
        ////    counts[i] = 0;
        ////    doneFlags[i] = false;
        ////}

        ////cellCount *= 4;

        ////for (int i = 0; i < cellCount; i++)
        ////{
        ////    zValues[i] = 0;
        ////}

        //qpMath* m = new qpMath();
        //qp posX = m->fromLongRational(mapSectionRequest.positionX[0], mapSectionRequest.positionX[1], mapSectionRequest.positionExponent);
        //qp posY = m->fromLongRational(mapSectionRequest.positionY[0], mapSectionRequest.positionY[1], mapSectionRequest.positionExponent);

        //qp deltaWidth = m->fromLongRational(mapSectionRequest.samplePointDeltaWidth[0], mapSectionRequest.samplePointDeltaWidth[1], mapSectionRequest.samplePointDeltaExponent);
        //qp deltaHeight = m->fromLongRational(mapSectionRequest.samplePointDeltaHeight[0], mapSectionRequest.samplePointDeltaHeight[1], mapSectionRequest.samplePointDeltaExponent);
        //delete m;

        ////qpParser* p = new qpParser();
        ////std::string px = p->ToStr(posX);
        ////std::string dw = p->ToStr(deltaWidth);
        ////delete p;

        ////std::cout << "posX is " << px << " and deltaWidth is " << dw;

        //Generator* g = new Generator();

        //PointDd pos = PointDd(posX, posY);
        //SizeInt blockSize = SizeInt(mapSectionRequest.blockSizeWidth, mapSectionRequest.blockSizeHeight);
        //SizeDd sampleSize = SizeDd(deltaWidth, deltaHeight);

        //g->FillCountsVec(pos, blockSize, sampleSize, targetCount, counts, doneFlags, zValues);

        //delete g;

        //for (int i = 0; i < size; i++)
        //{
        //    (*ppArray)[i] = i;
        //}

        return false;
    }
}