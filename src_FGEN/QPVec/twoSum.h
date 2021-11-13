#pragma once

#include "qpvec.h"

namespace qpvec
{
	class QPVEC_API twoSum
	{

	public:

		void two_sumA(double * a, double * b, double * s, double * err);
		void quick_two_sumA(double *a, double *b, double *s, double* err);

		void two_diffA(double * a, double * b, double * s, double * err);

		void three_sum(double * a, double * b, double * c);
		void three_sum2(double * a, double * b, double * c);

		twoSum(int len);
		~twoSum();

	private:
		int _len;
		vHelper * _vh;

		double * _bb;
		double * _b_minus_bb;
		double * _b_plus_bb;
		double * _s_minus_bb;
		double * _a_minus_s_minus_bb;

		double * _t1;
		double * _t2;
		double * _t3;
	};
}
