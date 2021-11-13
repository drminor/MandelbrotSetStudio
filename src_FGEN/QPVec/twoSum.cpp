#pragma once

#include "stdafx.h"
#include "twoSum.h"
#include "mkl.h"

namespace qpvec
{

	void twoSum::two_sumA(double *a, double *b, double *s, double* err)
	{
		//double s = a + b;
		vdAdd(_len, a, b, s);

		//double bb = s - a;
		_vh->clearVec(_len, _bb);
		vdSub(_len, s, a, _bb);

		//double bbb = b - bb;
		_vh->clearVec(_len, _b_minus_bb);
		vdSub(_len, b, _bb, _b_minus_bb);

		//double sbb = s - bb;
		_vh->clearVec(_len, _s_minus_bb);
		vdSub(_len, s, _bb, _s_minus_bb);

		//double asbb = a - sbb;
		_vh->clearVec(_len, _a_minus_s_minus_bb);
		vdSub(_len, a, _s_minus_bb, _a_minus_s_minus_bb);

		//double err2 = asbb + bbb;
		vdAdd(_len, _a_minus_s_minus_bb, _b_minus_bb, err);

		//err = (a - (s - bb)) + (b - bb);
		//return s;
	}

	///* Computes fl(a+b) and err(a+b).  Assumes |a| >= |b|. */
	//inline double quick_two_sum(double a, double b, double& err)
	//{
	//	double s = a + b;
	//	err = QD_ISFINITE(s) ? b - (s - a) : 0.0;
	//	return s;
	//}

	void twoSum::quick_two_sumA(double *a, double *b, double *s, double* err)
	{
		//double s = a + b;
		vdAdd(_len, a, b, s);

		//double bb = s - a;
		_vh->clearVec(_len, _bb);
		vdSub(_len, s, a, _bb);

		//err = b - bb;
		vdSub(_len, b, _bb, err);
	}


	///* Computes fl(a-b) and err(a-b).  */
	//inline double two_diff(double a, double b, double& err)
	//{
	//	double s = a - b;
	//	if (QD_ISFINITE(s))
	//	{
	//		double bb = s - a;
	//		err = (a - (s - bb)) - (b + bb);
	//	}
	//	else
	//		err = 0.0;
	//	return s;
	//}

	void twoSum::two_diffA(double *a, double *b, double *s, double* err)
	{
		//double s = a - b;
		vdSub(_len, a, b, s);

		//double bb = s - a;
		_vh->clearVec(_len, _bb);
		vdSub(_len, s, a, _bb);

		//double bbb = b + bb;
		_vh->clearVec(_len, _b_plus_bb);
		vdAdd(_len, b, _bb, _b_plus_bb);

		//double sbb = s - bb;
		_vh->clearVec(_len, _s_minus_bb);
		vdSub(_len, s, _bb, _s_minus_bb);

		//double asbb = a - sbb;
		_vh->clearVec(_len, _a_minus_s_minus_bb);
		vdSub(_len, a, _s_minus_bb, _a_minus_s_minus_bb);

		//double err2 = asbb - bbb;
		vdSub(_len, _a_minus_s_minus_bb, _b_plus_bb, err);

		//err = (a - (s - bb)) - (b + bb);
		//return s;
	}

	void twoSum::three_sum(double *a, double *b, double *c)
	{
		//t1 = two_sum(a, b, t2);
		_vh->clearVec(_len, _t1);
		_vh->clearVec(_len, _t2);
		two_sumA(a, b, _t1, _t2);

		//a = two_sum(c, t1, t3);
		_vh->clearVec(_len, a);
		_vh->clearVec(_len, _t3);
		two_sumA(c, _t1, a, _t3);

		//b = two_sum(t2, t3, c);
		_vh->clearVec(_len, b);
		_vh->clearVec(_len, c);
		two_sumA(_t2, _t3, b, c);

	}

	void twoSum::three_sum2(double *a, double *b, double *c)
	{
		//t1 = two_sum(a, b, t2);
		_vh->clearVec(_len, _t1);
		_vh->clearVec(_len, _t2);
		two_sumA(a, b, _t1, _t2);

		//a = two_sum(c, t1, t3);
		_vh->clearVec(_len, a);
		_vh->clearVec(_len, _t3);
		two_sumA(c, _t1, a, _t3);

		//b = t2 + t3;
		_vh->clearVec(_len, b);
		vdAdd(_len, _t2, _t3, b);
	}

	twoSum::twoSum(int len)
	{
		_len = len;
		_vh = new vHelper();

		_bb = _vh->createVec(_len);
		_b_minus_bb = _vh->createVec(_len);
		_b_plus_bb = _vh->createVec(_len);

		_s_minus_bb = _vh->createVec(_len);
		_a_minus_s_minus_bb = _vh->createVec(_len);

		_t1 = _vh->createVec(_len);
		_t2 = _vh->createVec(_len);
		_t3 = _vh->createVec(_len);
	}

	twoSum::~twoSum()
	{
		delete _vh;
		delete[] _bb, _b_minus_bb, _b_plus_bb, _s_minus_bb, _a_minus_s_minus_bb;
		delete[] _t1, _t2, _t3;
	}
}