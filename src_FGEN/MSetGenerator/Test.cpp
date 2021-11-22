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

        qp nHRaw = qp(hi);
        qp nL = qp(lo);
        double e = std::ldexp(1, exponent);
        double factor = std::ldexp(1, 53);

        qpMath* m = new qpMath();
        qp nH = m->mulD(nHRaw, factor);
        qp n = m->add(nH, nL);
        qp result = m->mulD(n, e);
        delete m;

        buffer[0] = result._hi();
        buffer[1] = result._lo();
    }


}