#include "pch.h"

#include <stdio.h>

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

        buffer[0] = 1;
        buffer[1] = 2;
    }


}