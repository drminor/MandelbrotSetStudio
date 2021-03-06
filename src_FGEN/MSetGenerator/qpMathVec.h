#pragma once

#include "qp.h"
#include "twoSum.h"
#include "twoProd.h"

//using namespace qpvec;

class qpMathVec
{

public:
	qpMathVec(int len);
	~qpMathVec();

	void addQps(double* ahis, double* alos, double* bhis, double* blos, double* rhis, double* rlos);
	void subQps(double* ahis, double* alos, double* bhis, double* blos, double* rhis, double* rlos);

	void addDToQps(double* ahis, double* alos, double* b, double* rhis, double* rlos);
	void subDFromQps(double* ahis, double* alos, double* b, double* rhis, double* rlos);


	void mulQpByD(double* his, double* los, double* f, double* rhis, double* rlos);


	void mulQpByQp(double* ahis, double* alos, double* bhis, double* blos, double* rhis, double* rlos);


	void sqrQp(double* ahis, double* alos, double* rhis, double* rlos);

	void extendSingleQp(qp val, double* his, double* los);
	void clearVec(double* his, double* los);

	void fillQpVector(double* his, double* los, qp* result);

	int GetBlockWidth()
	{
		return _len;
	}

private:
	int _len;
	twoSum* _twoSum;
	twoProd* _twoProd;

	double* _t1;

	double* _e1;
	double* _e2;
	double* _e3;

	double* _p1;
	double* _p2;
	double* _p3;
	double* _two;

	void initWorkingVectors();
};


