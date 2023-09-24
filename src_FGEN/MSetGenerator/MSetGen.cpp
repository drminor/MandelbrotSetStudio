#include "pch.h"

#include <stdio.h>
#include <cmath>

#include "SizeInt.h"
#include "SizeDd.h"
#include "qpMath.h"
#include "Generator.h"
#include "qpParser.h"

typedef struct _MSETREQ
{
    char* subdivisionId;

    // Block Position -- BigVectorDto
    LONGLONG blockPositionX[2];
    LONGLONG blockPositionY[2];

    // Position -- RPointDto
    LONGLONG positionX[2];
    LONGLONG positionY[2];
    int positionExponent;
    int positionPrecision;

    // BlockSize
    int blockSizeWidth;
    int blockSizeHeight;

    // SamplePointDelta -- RSizeDto
    LONGLONG samplePointDeltaWidth[2];
    LONGLONG samplePointDeltaHeight[2];
    int samplePointDeltaExponent;

    // MapCalcSettings;
    int maxIterations;
    int threshold;
    int iterationsPerStep;

} MSETREQ;


extern "C"
{
    __declspec(dllexport) void GenerateMapSection(MSETREQ mapSectionRequest, int* counts, bool* doneFlags, double* zValues)
    {
        int targetCount = mapSectionRequest.maxIterations;

        std::cout << std::endl << "Generating MapSection for blockPos: " << mapSectionRequest.blockPositionX << " and Target Count:" << targetCount << std::endl;

        int cellCount = mapSectionRequest.blockSizeWidth * mapSectionRequest.blockSizeHeight;

        //for (int i = 0; i < cellCount; i++)
        //{
        //    counts[i] = 0;
        //    doneFlags[i] = false;
        //}

        //cellCount *= 4;

        //for (int i = 0; i < cellCount; i++)
        //{
        //    zValues[i] = 0;
        //}

        qpMath* m = new qpMath();
        qp posX = m->fromLongRational(mapSectionRequest.positionX[0], mapSectionRequest.positionX[1], mapSectionRequest.positionExponent);
        qp posY = m->fromLongRational(mapSectionRequest.positionY[0], mapSectionRequest.positionY[1], mapSectionRequest.positionExponent);

        qp deltaWidth = m->fromLongRational(mapSectionRequest.samplePointDeltaWidth[0], mapSectionRequest.samplePointDeltaWidth[1], mapSectionRequest.samplePointDeltaExponent);
        qp deltaHeight = m->fromLongRational(mapSectionRequest.samplePointDeltaHeight[0], mapSectionRequest.samplePointDeltaHeight[1], mapSectionRequest.samplePointDeltaExponent);
        delete m;

        //qpParser* p = new qpParser();
        //std::string px = p->ToStr(posX);
        //std::string dw = p->ToStr(deltaWidth);
        //delete p;

        //std::cout << "posX is " << px << " and deltaWidth is " << dw;
        
        Generator* g = new Generator();

        PointDd pos = PointDd(posX, posY);
        SizeInt blockSize = SizeInt(mapSectionRequest.blockSizeWidth, mapSectionRequest.blockSizeHeight);
        SizeDd sampleSize = SizeDd(deltaWidth, deltaHeight);

        g->FillCountsVec(pos, blockSize, sampleSize, targetCount, counts, doneFlags, zValues);

        delete g;

        //for (int i = 0; i < size; i++)
        //{
        //    (*ppArray)[i] = i;
        //}
    }

    __declspec(dllexport) void GetStringValues(MSETREQ mapSectionRequest, char** px, char** py, char** deltaW, char** deltaH)
    {
        qpMath* m = new qpMath();
        qp posX = m->fromLongRational(mapSectionRequest.positionX[0], mapSectionRequest.positionX[1], mapSectionRequest.positionExponent);
        qp posY = m->fromLongRational(mapSectionRequest.positionY[0], mapSectionRequest.positionY[1], mapSectionRequest.positionExponent);

        qp deltaWidth = m->fromLongRational(mapSectionRequest.samplePointDeltaWidth[0], mapSectionRequest.samplePointDeltaWidth[1], mapSectionRequest.samplePointDeltaExponent);
        qp deltaHeight = m->fromLongRational(mapSectionRequest.samplePointDeltaHeight[0], mapSectionRequest.samplePointDeltaHeight[1], mapSectionRequest.samplePointDeltaExponent);
        delete m;

        qpParser* p = new qpParser();
        std::string sPx = p->ToStr(posX);
        std::string sPy = p->ToStr(posY);

        std::string sDw = p->ToStr(deltaWidth);
        std::string sDh = p->ToStr(deltaHeight);
        delete p;

        const char* cPx = sPx.c_str();
        const char* cPy = sPy.c_str();
        const char* cDeltaW = sDw.c_str();
        const char* cDeltaH = sDh.c_str();

        *px = (char*)::CoTaskMemAlloc(strlen(cPx) + 1);
        if (*px != 0) {
            strcpy_s(*px, strlen(cPx) + 1, cPx);
        }

        *py = (char*)::CoTaskMemAlloc(strlen(cPy) + 1);
        if (*py != 0) {
            strcpy_s(*py, strlen(cPy) + 1, cPy);
        }

        *deltaW = (char*)::CoTaskMemAlloc(strlen(cDeltaW) + 1);
        if (*deltaW != 0) {
            strcpy_s(*deltaW, strlen(cDeltaW) + 1, cDeltaW);
        }

        *deltaH = (char*)::CoTaskMemAlloc(strlen(cDeltaH) + 1);
        if (*deltaH != 0) {
            strcpy_s(*deltaH, strlen(cDeltaH) + 1, cDeltaH);
        }


        //std::cout << "posX is " << px << " and deltaWidth is " << dw;
    }


#pragma region Unused

    __declspec(dllexport) void DisplayHelloFromDLL()
    {
        printf("Hello from DLL !\n");
    }

    __declspec(dllexport) int SendBigIntUsingLongs(long hi, long lo, int exponent)
    {
        printf("Got the longs.\n");
        return 0;
    }

    __declspec(dllexport) void ConvertLongsToDoubles(long hi, long lo, int exponent, double* buffer)
    {
        printf("Filling in the doubles.\n");

        qpMath* m = new qpMath();
        qp result = m->fromLongRational(hi, lo, exponent);
        delete m;

        buffer[0] = result._hi();
        buffer[1] = result._lo();
    }

    __declspec(dllexport) void GenerateMapSection1(long hi, long lo, int exponent, unsigned int** ppArray, int size)
    {
        printf("Generating MapSection 1.\n");

        qpMath* m = new qpMath();
        qp result = m->fromLongRational(hi, lo, exponent);
        delete m;

        for (int i = 0; i < size; i++)
        {
            (*ppArray)[i] = i;
        }
    }


#pragma endregion

}