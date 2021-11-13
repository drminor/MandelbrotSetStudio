#pragma once

#include "../QPVec/vHelper.h"
#include "qp.h"
#include "GenPt.h"
#include "qpMath.h"

namespace FGen
{
	class qpMathVec;

	class FGenMath
	{
	public:
		FGenMath(int len);
		~FGenMath();

		void Iterate(GenPt &genPt);
		void extendSingleQp(qp val, double * his, double * los);

		void InitialzeNewEntries(GenPt & genPt);

	private:
		int _len;
		qpvec::vHelper * _vhelper;
		qpMathVec * _qpCalc;
		qpMath * _qpCalcSingle;
		double * _two;
	};
}


