#pragma once

#include "qpvec.h"
#include <math.h>

namespace qpvec
{
	class QPVEC_API twoProd
	{
	public:

		void two_prodA(double *a, double *b, double *p, double *err);
		void two_sqrA(double *a, double *p, double *err);

		//void split(double a, double & hi, double & lo);
		void splitA(double * a, double * hi, double * lo);
		void splitSingle(double * a, double * hi, double * lo);

		twoProd(int len);
		~twoProd();

		double * _two;

	private:
		int _len;
		vHelper * _vh;

		// For Split
		double * _splitter;
		double * _splitTemp;
		double * _temp_minus_a;

		// For TwoProd
		double * _a_hi;
		double * _a_lo;
		double * _b_hi;
		double * _b_lo;

		double * _ah_m_bh;
		double * _ah_m_bl;
		double * _al_m_bh;
		double *_al_m_bl;

		double * _ah_m_bh_minus_p;
		double * _s1;
		double * _s2;

		// For TwoSqr
		double * _2ah_m_bl;

		void two_diffA(double *a, double *b, double *s, double *err);

		double * _bb;
		double * _b_plus_bb;
		double * _s_minus_bb;
		double * _a_minus_s_minus_bb;

	};
}
