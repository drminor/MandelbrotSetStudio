#pragma once

#include "pch.h"
#include "framework.h"
#include <immintrin.h>
#include "VecHelper.h"
#include "fp31VecMath.h"

#include <iostream>

#if !defined(_RPTW)
#if defined(_DEBUG)
#define _RPTW(pszFmt, ...) _CrtDbgReportW(_CRT_WARN, NULL, __LINE__, NULL, (pszFmt), __VA_ARGS__)
#define _RPTW_(dest, fmt, ...) _CrtDbgReportW((dest), NULL, __LINE__, NULL, (pszFmt), __VA_ARGS__)
#else
#define _RPTW(pszFmt, ...)
#define _RPTW(dest, pszFmt)
#endif
#endif // #if !defined(_RPTW)

#if !defined(_RPTA)
#if defined(_DEBUG)
#define _RPTA(pszFmt, ...) _CrtDbgReport(_CRT_WARN, NULL, __LINE__, NULL, (pszFmt), __VA_ARGS__)
#define _RPTA_(dest, fmt, ...) _CrtDbgReport((dest), NULL, __LINE__, NULL, (pszFmt), __VA_ARGS__)
#else
#define _RPTA(pszFmt, ...)
#define _RPTA_(dest, pszFmt)
#endif
#endif // #if !defined(_RPTA)

#if !defined(_RPTT)
#if defined(_UNICODE)
#define _RPTT _RPTW
#define _RPTT_ _RPTW_
#else
#define _RPTT _RPTA
#define _RPTT_ _RPTA_
#endif
#endif // #if !defined(_RPTT)


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
