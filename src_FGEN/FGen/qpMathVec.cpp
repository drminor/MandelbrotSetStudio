#pragma once

#include "stdafx.h"
#include "qpMathVec.h"
#include "../QPVec/qpvec.h"

using namespace qpvec;

namespace FGen
{

	qpMathVec::qpMathVec(int len)
	{
		_len = len;
		initWorkingVectors();
	}

	void qpMathVec::initWorkingVectors()
	{
		_twoSum = new twoSum(_len);
		_twoProd = new twoProd(_len);

		_t1 = new double[_len];

		_e1 = new double[_len];
		_e2 = new double[_len];
		_e3 = new double[_len];

		_p1 = new double[_len];
		_p2 = new double[_len];
		_p3 = new double[_len];

		//for (int i = 0; i < _len; i++) {
		//	_t1[i] = 0.0;

		//	_e1[i] = 0.0;
		//	_e2[i] = 0.0;
		//	_e3[i] = 0.0;

		//	_p1[i] = 0.0;
		//	_p2[i] = 0.0;
		//	_p3[i] = 0.0;
		//}
	}

	qpMathVec::~qpMathVec()
	{
		delete _twoSum;
		delete _twoProd;
		delete[] _t1, _e1, _e2, _e3, _p1, _p2, _p3;
	}

	void qpMathVec::addQps(double * ahis, double * alos, double * bhis, double * blos, double * rhis, double * rlos)
	{
		_twoSum->two_sumA(ahis, bhis, rhis, _e1);

		_twoSum->two_sumA(alos, blos, _t1, _e2);
		_twoSum->two_sumA(_e1, _t1, rlos, _e3);

		vdAdd(_len, _e2, _e3, _e1);

		_twoSum->three_sum2(rhis, rlos, _e1);


		//dd_real& operator+=(dd_real const& b)
		//{
		//	double s2;
		//	x[0] = qd::two_sum(x[0], b.x[0], e1);
		//	if (QD_ISFINITE(x[0]))
		//	{
		//		double t2, t1 = qd::two_sum(x[1], b.x[1], e2);
		//		x[1] = qd::two_sum(e1, t1, e3);
		//		e3 += e2;
		//		qd::three_sum(x[0], x[1], e3);
		//	}
		//	else
		//	{
		//		x[1] = 0.0;
		//	}
		//	return *this;
		//}
	}

	void qpMathVec::subQps(double * ahis, double * alos, double * bhis, double * blos, double * rhis, double * rlos)
	{
		_twoSum->two_diffA(ahis, bhis, rhis, _e1);

		_twoSum->two_diffA(alos, blos, _t1, _e2);
		_twoSum->two_sumA(_e1, _t1, rlos, _e3);

		vdAdd(_len, _e2, _e3, _e1);

		_twoSum->three_sum2(rhis, rlos, _e1);

		//dd_real& operator-=(dd_real const& b)
		//{
		//	double s2;
		//	x[0] = qd::two_sum(x[0], -b.x[0], s2);
		//	if (QD_ISFINITE(x[0]))
		//	{
		//		double t2, t1 = qd::two_sum(x[1], -b.x[1], t2);
		//		x[1] = qd::two_sum(s2, t1, t1);
		//		t1 += t2;
		//		qd::three_sum(x[0], x[1], t1);
		//	}
		//	else
		//	{
		//		x[1] = 0.0;
		//	}
		//	return *this;
		//}
	}

	void qpMathVec::addDToQps(double * ahis, double * alos, double * b, double * rhis, double * rlos)
	{
		_twoSum->two_sumA(ahis, b, rhis, _e1);

		_twoSum->two_sumA(alos, _e1, rlos, _e2);
		_twoSum->three_sum2(rhis, rlos, _e2);

		//dd_real& operator+=(double b)
		//{
		//	double s2;
		//	x[0] = qd::two_sum(x[0], b, s2);
		//	if (QD_ISFINITE(x[0]))
		//	{
		//		x[1] = qd::two_sum(x[1], s2, s2);
		//		qd::three_sum(x[0], x[1], s2);
		//	}
		//	else
		//	{
		//		x[1] = 0.0;
		//	}
		//	return *this;
		//}
	}

	void qpMathVec::subDFromQps(double * ahis, double * alos, double * b, double * rhis, double * rlos)
	{
		_twoSum->two_diffA(ahis, b, rhis, _e1);

		_twoSum->two_sumA(alos, _e1, rlos, _e2);
		_twoSum->three_sum2(rhis, rlos, _e2);

		//dd_real& operator-=(double b)
		//{
		//	double s2;
		//	x[0] = qd::two_sum(x[0], -b, s2);
		//	if (QD_ISFINITE(x[0]))
		//	{
		//		x[1] = qd::two_sum(x[1], s2, s2);
		//		qd::three_sum(x[0], x[1], s2);
		//	}
		//	else
		//	{
		//		x[1] = 0.0;
		//	}
		//	return *this;
		//}
	}

	void qpMathVec::mulQpByD(double * his, double * los, double * f, double * rhis, double * rlos)
	{
		_twoProd->two_prodA(his, f, rhis, _p1);
		vdMul(_len, los, f, rlos);

		_twoSum->three_sum2(rhis, rlos, _p1);

		//dd_real& operator*=(double b)
		//{
		//	double p1;
		//	x[0] = qd::two_prod(x[0], b, p1);
		//	if (QD_ISFINITE(x[0]))
		//	{
		//		x[1] *= b;
		//		qd::three_sum(x[0], x[1], p1);
		//	}
		//	else
		//	{
		//		x[1] = 0.0;
		//	}
		//	return *this;
		//}
	}

	void qpMathVec::mulQpByQp(double * ahis, double * alos, double * bhis, double * blos, double * rhis, double * rlos)
	{
		//p[0] = qd::two_prod(x[0], b.x[0], p[1]);
		_twoProd->two_prodA(ahis, bhis, rhis, rlos);

		//p[2] = qd::two_prod(x[0], b.x[1], p[4]);
		_twoProd->two_prodA(ahis, blos, _p1, _e1);

		//p[3] = qd::two_prod(x[1], b.x[0], p[5]);
		_twoProd->two_prodA(alos, bhis, _p2, _e2);

		//p[6] = x[1] * b.x[1];
		vdMul(_len, alos, blos, _e3);

		//	e powers in p = 0, 1, 2, 3, 2, 2, 2
		//qd::three_sum(p[1], p[2], p[3]);
		_twoSum->three_sum2(rlos, _p1, _p2);

		//	e powers in p = 0, 1, 2, 3, 2, 3, 4
		//p[2] += p[4] + p[5] + p[6];
		vdAdd(_len, _p1, _e1, _p2);
		vdAdd(_len, _p2, _e2, _e1);
		vdAdd(_len, _e1, _e3, _e2);

		//qd::three_sum(p[0], p[1], p[2]);
		_twoSum->three_sum2(rhis, rlos, _e2);

		//dd_real& operator*=(dd_real const& b)
		//{
		//	double p[7];
		//	//	e powers in p = 0, 1, 1, 1, 2, 2, 2
		//	p[0] = qd::two_prod(x[0], b.x[0], p[1]);
		//	if (QD_ISFINITE(p[0]))
		//	{
		//		p[2] = qd::two_prod(x[0], b.x[1], p[4]);
		//		p[3] = qd::two_prod(x[1], b.x[0], p[5]);
		//		p[6] = x[1] * b.x[1];

		//		//	e powers in p = 0, 1, 2, 3, 2, 2, 2
		//		qd::three_sum(p[1], p[2], p[3]);

		//		//	e powers in p = 0, 1, 2, 3, 2, 3, 4
		//		p[2] += p[4] + p[5] + p[6];

		//		qd::three_sum(p[0], p[1], p[2]);

		//		x[0] = p[0];
		//		x[1] = p[1];
		//	}
		//	else
		//	{
		//		x[0] = p[0];
		//		x[1] = 0.0;
		//	}
		//	return *this;
		//}
	}

	void qpMathVec::sqrQp(double * ahis, double * alos, double * rhis, double * rlos)
	{
		_twoProd->two_sqrA(ahis, _p1, _e1);
		vdMul(_len, ahis, alos, _p2);
		vdMul(_len, _p2, _twoProd->_two, _p3);

		vdAdd(_len, _e1, _p3, _p2);

		vdMul(_len, alos, alos, _e1);
		vdAdd(_len, _p2, _e1, _p3);

		_twoSum->quick_two_sumA(_p1, _p3, rhis, rlos);

		//dd_real sqr(dd_real const& a)
		//{
		//	if (std::isnan(a))
		//		return a;

		//	double p2, p1 = qd::two_sqr(a._hi(), p2);
		//	p2 += 2.0 * a._hi() * a._lo();
		//	p2 += a._lo() * a._lo();

		//	double s2, s1 = qd::quick_two_sum(p1, p2, s2);
		//	return dd_real(s1, s2);
		//}
	}

	void qpMathVec::extendSingleQp(qp val, double * his, double * los)
	{
		double hi = val._hi();
		double lo = val._lo();

		for (int i = 0; i < _len; i++) {
			his[i] = hi;
			los[i] = lo;
		}
	}

	void qpMathVec::clearVec(double * his, double * los)
	{
		for (int i = 0; i < _len; i++) {
			his[i] = 0.0;
			los[i] = 0.0;
		}
	}

	void qpMathVec::fillQpVector(double * his, double * los, qp * result)
	{
		for (int i = 0; i < _len; i++) {
			result[i] = qp(his[i], los[i]);
		}
	}

}
