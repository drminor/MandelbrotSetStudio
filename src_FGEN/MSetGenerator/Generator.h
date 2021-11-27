#pragma once

#include <vector>
#include "SizeInt.h"
#include "qp.h"
#include "PointDd.h"
#include "SizeDd.h"

class Generator
{
public:

	Generator();

	void FillCountsVec(PointDd pos, SizeInt blockSize, SizeDd sampleSize, int targetCount, int* counts, bool* doneFlags, double* zValues);

	void FillXCountsTest(PointDd pos, SizeInt blockSize, SizeDd sampleSize, int targetCount, unsigned int* counts, bool* doneFlags, double* zValues, int yPtr);

	~Generator();

private:
	double m_Log2;

	void GetPoints(qp startC, qp delta, int extent, qp* result);
	bool QpGreaterThan(double hi, double lo, double comp);
	double GetEscapeVelocity(qp sumSqs);
	PointDd GetPointDd(double* zValues);
	void PointDdToDoubleArray(PointDd z, double* zValues);
};


