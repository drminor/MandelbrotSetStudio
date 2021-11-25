#include "pch.h"

#include <stdio.h>
#include <cmath>

#include "qpMath.h"

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

    __declspec(dllexport) void GenerateMapSection(long hi, long lo, int exponent, unsigned int** ppArray, int size)
    {
        printf("Filling in the doubles.\n");

        qpMath* m = new qpMath();
        qp result = m->fromLongRational(hi, lo, exponent);
        delete m;

        for (int i = 0; i < size; i++)
        {
            (*ppArray)[i] = i;
        }

    }

}