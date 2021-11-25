#include "pch.h"

#include <stdio.h>
#include <cmath>

#include "qpMath.h"

typedef struct _MSETREQ
{
    char* subdivisionId;
    
    // Block Position
    int blockPositionX;
    int blockPositionY;

    // RPointDto Position
    LONGLONG positionX[2];
    LONGLONG positionY[2];
    int positionExponent;

    // BlockSize
    int blockSizeWidth;
    int blockSizeHeight;

    // RSizeDto SamplePointsDelta;
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

    __declspec(dllexport) void GenerateMapSection(MSETREQ mapSectionRequest, unsigned int** ppArray, int size)
    {
        printf("Generating MapSection.\n");

        qpMath* m = new qpMath();
        qp result = m->fromLongRational(mapSectionRequest.positionX[0], mapSectionRequest.positionX[1], mapSectionRequest.positionExponent);
        delete m;

        for (int i = 0; i < size; i++)
        {
            (*ppArray)[i] = i;
        }
    }
}