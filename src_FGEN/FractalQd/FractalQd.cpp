// FractalQd.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"

//#include <qd/dd_real.h>

#define FQD_CALL __stdcall 
#define FQD_DLL_EXPIMP  __declspec(dllexport)

typedef struct { 
	double Hi;
	double Lo;
} Qd;

typedef struct {
	Qd X;
	Qd Y;
} QdPoint;

typedef struct {
	QdPoint LeftBot;
	QdPoint RightTop;
} QdBox;

#include <string>
using namespace std;

//dd_real GetFromQd(Qd qd) {
//	dd_real result = dd_real(qd.Hi, qd.Lo);
//	return result;
//}

extern "C"
{
	FQD_DLL_EXPIMP void FQD_CALL GetSamplePoints(Qd start, Qd end, int n, Qd array[])
	{
		double d = 2.0;
		for (int i = 0; i < n; i++)
		{
			array[i].Hi = d + i;
			array[i].Lo = d;
		}
	}

	FQD_DLL_EXPIMP void FQD_CALL CalcMap(int maxIterations, int xLen, Qd xVals[], int yLen, Qd yVals[], int cnts[])
	{
		int ptr = 0;

		for (int j = 0; j < yLen; j++)
		{
			for (int i = 0; i < xLen; i++)
			{
				int cntr = 0;
				cnts[ptr++] = cntr++;
			}
		}
	}

	//FQD_DLL_EXPIMP void FQD_CALL TryComplex(Qd inputVar, Qd *outputVar, int n, Qd array[])
	//{
	//	outputVar->Hi = ++inputVar.Hi;
	//	outputVar->Lo = ++inputVar.Lo;

	//	array[0].Hi = 99.0;
	//	array[0].Lo = 98.0;
	//	array[1].Hi = 97.0;
	//	array[1].Lo = 96.0;
	//}

	FQD_DLL_EXPIMP double FQD_CALL quick_two_sum(double a, double b)
	{
		return a + b;
	}

	FQD_DLL_EXPIMP double FQD_CALL Parse(const char* strVal, double *lo)
	{
		double d = 2.0;
		*lo = 1.3;

		return d;
	}

	FQD_DLL_EXPIMP void FQD_CALL GetDigits(double hi, double lo, char* outStr, int nd)
	{
		string strVal = "This is a test";
		const char *tempOut = strVal.c_str();

		size_t len = strVal.length();

		strncpy_s(outStr, len + 1, tempOut, len);
	}


	//FQD_DLL_EXPIMP void FQD_CALL GetSamplePoints(Qd start, Qd end, int n, Qd array[])
	//{
	//	dd_real s = GetFromQd(start);
	//	dd_real e = GetFromQd(end);

	//	dd_real diff = e - s;
	//	dd_real unit = diff / n;

	//	for (int i = 0; i < n; i++)
	//	{
	//		dd_real sp = s + i * unit;
	//		array[i].Hi = sp._hi();
	//		array[i].Lo = sp._lo();
	//	}
	//}

	//FQD_DLL_EXPIMP void FQD_CALL CalcMap(int maxIterations, int xLen, Qd xVals[], int yLen, Qd yVals[], int cnts[])
	//{
	//	dd_real zx = 0;
	//	dd_real zy = 0;

	//	dd_real zxSquared = 0;
	//	dd_real zySquared = 0;

	//	int ptr = 0;

	//	for (int j = 0; j < yLen; j++)
	//	{
	//		dd_real cy = GetFromQd(yVals[j]);
	//		for (int i = 0; i < xLen; i++)
	//		{
	//			dd_real cx = GetFromQd(xVals[i]);

	//			zx = 0;
	//			zy = 0;
	//			zxSquared = 0;
	//			zySquared = 0;

	//			int cntr;
	//			for (cntr = 0; cntr < maxIterations; cntr++)
	//			{
	//				zy = 2 * zx * zy + cy;
	//				zx = zxSquared - zySquared + cx;

	//				zxSquared = zx * zx;
	//				zySquared = zy * zy;

	//				if (zxSquared + zySquared > 4)
	//				{
	//					break;
	//				}
	//			}
	//			cnts[ptr++] = cntr;
	//		}

	//	}
	//}

	//FQD_DLL_EXPIMP void FQD_CALL TryComplex(Qd inputVar, Qd *outputVar, int n, Qd array[])
	//{
	//	outputVar->Hi = ++inputVar.Hi;
	//	outputVar->Lo = ++inputVar.Lo;

	//	array[0].Hi = 99.0;
	//	array[0].Lo = 98.0;
	//	array[1].Hi = 97.0;
	//	array[1].Lo = 96.0;
	//}

	//FQD_DLL_EXPIMP double FQD_CALL quick_two_sum(double a, double b)
	//{
	//	double temp = 10;
	//	return qd::quick_two_sum(a, b, temp);
	//}

	//FQD_DLL_EXPIMP double FQD_CALL Parse(const char* strVal, double *lo)
	//{
	//	string temp = string(strVal);
	//	dd_real a = dd_real(temp);

	//	*lo = a._lo(); //  a->_lo();
	//	double h = a._hi(); // a->_hi();

	//	return h;
	//}

	//FQD_DLL_EXPIMP void FQD_CALL GetDigits(double hi, double lo, char* outStr, int nd)
	//{
	//	dd_real temp = dd_real(hi, lo);
	//	string strVal = temp.to_string(nd);
	//	const char *tempOut = strVal.c_str();

	//	size_t len = strVal.length();

	//	strncpy_s(outStr, len + 1, tempOut, len);
	//}

}









