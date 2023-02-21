#include "pch.h"
#include "framework.h"
#include <iostream>

typedef struct _MSETREQ
{
    // ApFixedPointFormat
    int BitsBeforeBinaryPoint;
    int LimbCount;
    int NumberOfFractionalBits;
    int TotalBits;
    int TargetExponent;

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
    __declspec(dllexport) void GenerateMapSection(MSETREQ mapSectionRequest, int* counts)
    {
        int targetCount = mapSectionRequest.maxIterations;
        int limbCount = mapSectionRequest.LimbCount;

        std::cout << std::endl << "Generating MapSection with LimbCount: " << limbCount << " and Target Count:" << targetCount << std::endl;

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

}