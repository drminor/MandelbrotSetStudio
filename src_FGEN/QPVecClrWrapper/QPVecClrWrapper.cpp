#include "stdafx.h"

#include "../QPVec/qpvec.h"

#include "QPVecClrWrapper.h"

//#include "../Dll1/SC.h"

using namespace qpvec;

namespace QPVecClrWrapper
{

	Class1::Class1()
	{
	}

	int Class1::Test1(int a, int b)
	{
		return a + b;
	}

	//double Class1::VAdd()
	//{
	//	SC * c = new SC();
	//	double r = c->VAdd();

	//	return r;
	//}

	double Class1::VAdd()
	{
		twoSum * t = new twoSum(128);

		double * a = new double[128];
		double * b = new double[128];
		double * c = new double[128];
		double * d = new double[128];

		for (int i = 0; i < 128; i++) {
			a[i] = 1.2;
			b[i] = 3.4;

			c[i] = 0.0;
			c[i] = 0.0;
		}


		t->two_sumA(a, b, c, d);

		double result = c[0];

		delete[] a, b, c, d;

		return result;
	}

	void Class1::TestGetDiff()
	{
		int _len = 1;
		twoSum * _twoSum = new twoSum(1);


		double aHi = 4.3;
		double aLo = 0.0;

		double bHi = 2.1;
		double bLo = 0.0;

		double s1 = 0.0;
		double s2 = 0.0;

		_twoSum->two_diffA(&aHi, &bHi, &s1, &s2);

		double t1 = 0.0;
		double t2 = 0.0;
		double t3 = 0.0;

		_twoSum->two_diffA(&aLo, &bLo, &t1, &t2);
		_twoSum->two_sumA(&s2, &t1, &aLo, &t3);

		t3 += t2;
		_twoSum->three_sum(&s1, &aLo, &t3);

		//return qp(s1, aLo);

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

}
