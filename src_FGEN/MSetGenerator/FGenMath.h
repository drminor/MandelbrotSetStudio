#pragma once

#include "vHelper.h"
#include "qp.h"
#include "GenPt.h"
#include "qpMath.h"

	class qpMathVec;

	class FGenMath
	{
	public:
		FGenMath(int len);
		~FGenMath();

		void Iterate(GenPt& genPt);
		void extendSingleQp(qp val, double* his, double* los);

		void InitialzeNewEntries(GenPt& genPt);

	private:
		int _len;
		vHelper* _vhelper;
		qpMathVec* _qpCalc;
		qpMath* _qpCalcSingle;
		double* _two;
	};


