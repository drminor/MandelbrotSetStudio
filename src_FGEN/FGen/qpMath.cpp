#pragma once

#include "stdafx.h"
#include "qpMath.h"

namespace FGen
{
	qpMath::qpMath()
	{
	}

	qpMath::~qpMath()
	{
	}

	qp qpMath::sub(qp a, qp b)
	{
		double rHi;
		double rLo;
		addQpAndQp(a._hi(), a._lo(), -b._hi(), -b._lo(), rHi, rLo);
		return qp(rHi, rLo);
	}

	qp qpMath::add(qp a, qp b)
	{
		double rHi;
		double rLo;
		addQpAndQp(a._hi(), a._lo(), b._hi(), b._lo(), rHi, rLo);
		return qp(rHi, rLo);
	}

	void qpMath::addQpAndQp(double aHi, double aLo, double bHi, double bLo, double &rHi, double &rLo)
	{
		double s2;
		rHi = two_sum(aHi, bHi, s2);
		if (isfinite(rHi))
		{
			double t2;
			double t1 = two_sum(aLo, bLo, t2);

			double t4;
			rLo = two_sum(s2, t1, t4);

			t4 += t2;
			three_sum2(rHi, rLo, t4);
		}
		else
		{
			rLo = 0.0;
		}

		//dd_real& operator+=(dd_real const& b)
		//{
		//	double s2;
		//	x[0] = qd::two_sum(x[0], b.x[0], s2);
		//	if (QD_ISFINITE(x[0]))
		//	{
		//		double t2, t1 = qd::two_sum(x[1], b.x[1], t2);
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

	void qpMath::addDToQp(double ahi, double alo, double b, double &rhi, double &rlo)
	{
		//_twoSum->two_sumA(ahis, b, rhis, _e1);
		double e1;
		rhi = two_sum(ahi, b, e1);

		//_twoSum->two_sumA(alos, _e1, rlos, _e2);
		double e2;
		rlo = two_sum(alo, e1, e2);

		//_twoSum->three_sum2(rhis, rlos, _e2);
		three_sum2(rhi, rlo, e2);

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

	void qpMath::sqrQp(double ahi, double alo, double &rhi, double &rlo)
	{
		//_twoProd->two_sqrA(ahis, _p1, _e1);
		double e1;
		double p1 = two_sqr(ahi, e1);

		//vdMul(_len, ahis, alos, _p2);
		//vdMul(_len, _p2, _twoProd->_two, _p3);
		double p2 = 2 * ahi * alo;

		//vdAdd(_len, _e1, _p3, _p2);
		e1 += p2;

		//vdMul(_len, alos, alos, _e1);
		p2 = alo * alo;
		e1 += p2;

		//vdAdd(_len, _p2, _e1, _p3);

		//_twoSum->quick_two_sumA(_p1, _p3, rhis, rlos);
		rhi = two_sum(p1, e1, rlo);

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

	void qpMath::mulQpByD(double hi, double lo, double f, double &rhi, double &rlo)
	{
		double e1;
		rhi = two_prod(hi, f, e1);

		rlo = lo * f;
		three_sum2(rhi, rlo, e1);

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

	qp qpMath::mulD(qp a, double b)
	{
		double rHi;
		double rLo;
		mulQpByD(a._hi(), a._lo(), b, rHi, rLo);
		return qp(rHi, rLo);
	}

	void qpMath::mulQpByQp(double ahis, double alos, double bhis, double blos, double &rhis, double &rlos)
	{
		//p[0] = qd::two_prod(x[0], b.x[0], p[1]);
		//_twoProd->two_prodA(ahis, bhis, rhis, rlos);
		rhis = two_prod(ahis, bhis, rlos);

		//p[2] = qd::two_prod(x[0], b.x[1], p[4]);
		//_twoProd->two_prodA(ahis, blos, _p1, _e1);
		double e1;
		double p1 = two_prod(ahis, blos, e1);

		//p[3] = qd::two_prod(x[1], b.x[0], p[5]);
		//_twoProd->two_prodA(alos, bhis, _p2, _e2);
		double e2;
		double p2 = two_prod(alos, bhis, e2);

		//p[6] = x[1] * b.x[1];
		//vdMul(_len, alos, blos, _e3);
		double e3 = alos * blos;

		//	e powers in p = 0, 1, 2, 3, 2, 2, 2
		//qd::three_sum(p[1], p[2], p[3]);
		//_twoSum->three_sum2(rlos, _p1, _p2);
		three_sum2(rlos, p1, p2);

		//	e powers in p = 0, 1, 2, 3, 2, 3, 4
		//p[2] += p[4] + p[5] + p[6];
		//vdAdd(_len, _p1, _e1, _p2);
		//vdAdd(_len, _p2, _e2, _e1);
		//vdAdd(_len, _e1, _e3, _e2);
		p1 += e1 + e2 + e3;

		//qd::three_sum(p[0], p[1], p[2]);
		//_twoSum->three_sum2(rhis, rlos, _e2);
		three_sum2(rhis, rlos, p1);

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

	// quad-double = double-double * double-double
	void qpMath::mulQpByQpROp(double ahi, double alo, double bhi, double blo, double * r)
	{
		double p4, p5, p6, p7;// , t1, t2;

		//	powers of e - 0, 1, 1, 1, 2, 2, 2, 3
		//	p[0] = qd::two_prod(a._hi(), b._hi(), p[1]);
		//_twoProd->two_prodA(&ahi, &bhi, &r[0], &r[1]);
		r[0] = two_prod(ahi, bhi, r[1]);

		//	if (QD_ISFINITE(p[0]))
		//	{
		if(_finite(r[0]) != 0)
		{
			//	p[2] = qd::two_prod(a._hi(), b._lo(), p4);
			//_twoProd->two_prodA(&ahi, &blo, &r[2], &p4);
			r[2] = two_prod(ahi, blo, p4);

			//	p[3] = qd::two_prod(a._lo(), b._hi(), p5);
			//_twoProd->two_prodA(&alo, &bhi, &r[3], &p5);
			r[3] = two_prod(alo, bhi, p5);

			//	p6 = qd::two_prod(a._lo(), b._lo(), p7);
			//_twoProd->two_prodA(&alo, &blo, &p6, &p7);
			p6 = two_prod(alo, blo, p7);

			//	powers of e - 0, 1, 2, 3, 2, 2, 2, 3
			//	qd::three_sum(p[1], p[2], p[3]);
			//_twoSum->three_sum(&r[1], &r[2], &r[3]);
			three_sum(r[1], r[2], r[3]);

			//	powers of e - 0, 1, 2, 3, 2, 3, 4, 3
			//	qd::three_sum(p4, p5, p6);
			//_twoSum->three_sum(&p4, &p5, &p6);
			three_sum(p4, p5, p6);

			//	powers of e - 0, 1, 2, 3, 3, 3, 4, 3
			//	p[2] = qd::two_sum(p[2], p4, p4);
			//_twoSum->quick_two_sumA(&r[2], &p4, &t1, &t2);
			//r[2] = t1;
			//p4 = t2;

			r[2] = quick_two_sum(r[2], p4, p4);

			//	powers of e - 0, 1, 2, 3, 4, 5, 4, 3
			//	qd::three_sum(p[3], p4, p5);
			//_twoSum->three_sum(&r[3], &p4, &p5);
			three_sum(r[3], p4, p5);

			//	powers of e - 0, 1, 2, 3, 4, 5, 4, 4
			//	p[3] = qd::two_sum(p[3], p7, p7);
			//_twoSum->quick_two_sumA(&r[3], &p7, &t1, &t2);
			//r[3] = t1;
			//p7 = t2;

			r[3] = quick_two_sum(r[3], p7, p7);

			p4 += (p6 + p7);

			renorm(r[0], r[1], r[2], r[3], p4);
		}
		else
		{
			r[1] = r[2] = r[3] = 0.0;
		}
	}

	// quad-double + double-double
	void qpMath::addOpAndQp(double const* a, double bhi, double blo, double * s)
	{
		double * f = new double[4];
		for (int i = 0; i < 4; i++) {
			f[i] = a[i];
		}

		//	double t[5];
		//double t0, t1, t2, t3;
		double t0, t1;

		//	s[0] = qd::two_sum(a[0], b._hi(), t[0]);		//	s0 - O( 1 ); t0 - O( e )
		//_twoSum->quick_two_sumA(&f[0], &bhi, &s[0], &t0);
		s[0] = quick_two_sum(f[0], bhi, t0);

		//	s[1] = qd::two_sum(a[1], b._lo(), t[1]);		//	s1 - O( e ); t1 - O( e^2 )
		//_twoSum->quick_two_sumA(&f[1], &blo, &s[1], &t1);
		s[1] = quick_two_sum(f[1], blo, t1);

		//	s[1] = qd::two_sum(s[1], t[0], t[0]);			//	s1 - O( e ); t0 - O( e^2 )
		//_twoSum->quick_two_sumA(&s[1], &t0, &t2, &t3);
		//s[1] = t2;
		//t0 = t3;
		s[1] = quick_two_sum(s[1], t0, t0);


		//	s[2] = a[2];									//	s2 - O( e^2 )
		s[2] = a[2];

		//	qd::three_sum(s[2], t[0], t[1]);				//	s2 - O( e^2 ); t0 - O( e^3 ); t1 = O( e^4 )
		//_twoSum->three_sum(&s[2], &t0, &t1);
		three_sum(s[2], t0, t1);

		//	s[3] = qd::two_sum(a[3], t[0], t[0]);			//	s3 - O( e^3 ); t0 - O( e^4 )
		//_twoSum->quick_two_sumA(&f[3], &t0, &s[3], &t3);
		s[3] = quick_two_sum(f[3], t0, t0);

		//	t[0] += t[1];									//	fl( t0 + t1 ) - accuracy less important
		//t3 += t1;
		t0 += t1;

		//renorm(s[0], s[1], s[2], s[3], t3);
		renorm(s[0], s[1], s[2], s[3], t0);

		delete[] f;
	}

	void qpMath::renorm(double &c0, double &c1, double &c2, double &c3, double &c4)
	{
		//if (QD_ISINF(c0)) return;
		if (_finite(c0) != 0) return;

		double s0, s1, s2 = 0.0, s3 = 0.0;
		//double t, e4;

		//s0 = quick_two_sum(c3, c4, c4);
		//_twoSum->quick_two_sumA(&c3, &c4, &t, &e4);
		//c4 = e4;
		s0 = quick_two_sum(c3, c4, c4);

		//s0 = quick_two_sum(c2, s0, c3);
		//_twoSum->quick_two_sumA(&c2, &t, &s0, &c3);
		s0 = quick_two_sum(c2, s0, c3);

		//s0 = quick_two_sum(c1, s0, c2);
		//_twoSum->quick_two_sumA(&c1, &s0, &t, &c2);
		s0 = quick_two_sum(c1, s0, c2);

		//c0 = quick_two_sum(c0, s0, c1);
		//_twoSum->quick_two_sumA(&c0, &t, &s0, &c1);
		//c0 = s0;
		c0 = quick_two_sum(c0, s0, c1);

		//s0 = c0;
		//s1 = c1;

		//s0 = quick_two_sum(c0, c1, s1);
		//_twoSum->quick_two_sumA(&c0, &c1, &s0, &s1);
		s0 = quick_two_sum(c0, c1, s1);

		if (s1 != 0.0)
		{
			//	s1 = quick_two_sum(s1, c2, s2);
			//_twoSum->quick_two_sumA(&s1, &c2, &t, &s2);
			//s1 = t;
			s1 = quick_two_sum(s1, c2, s2);
			if (s2 != 0.0)
			{
				//	s2 = quick_two_sum(s2, c3, s3);
				//_twoSum->quick_two_sumA(&s2, &c3, &t, &s3);
				//s2 = t;
				s2 = quick_two_sum(s2, c3, s3);
				if (s3 != 0.0)
					s3 += c4;
				else
					s2 += c4;
			}
			else
			{
				//	s1 = quick_two_sum(s1, c3, s2);
				//_twoSum->quick_two_sumA(&s1, &c3, &t, &s2);
				//s1 = t;
				s1 = quick_two_sum(s1, c3, s2);
				if (s2 != 0.0)
				{
					//	s2 = quick_two_sum(s2, c4, s3);
					//_twoSum->quick_two_sumA(&s2, &c4, &t, &s3);
					//s2 = t;
					s2 = quick_two_sum(s2, c4, s3);
				}
				else
				{
					//	s1 = quick_two_sum(s1, c4, s2);
					//_twoSum->quick_two_sumA(&s1, &c4, &t, &s2);
					//s1 = t;
					s1 = quick_two_sum(s1, c4, s2);
				}
			}
		}
		else
		{
			//	s0 = quick_two_sum(s0, c2, s1);
			//_twoSum->quick_two_sumA(&s0, &c2, &t, &s1);
			//s0 = t;
			s0 = quick_two_sum(s0, c2, s1);
			if (s1 != 0.0)
			{
				//	s1 = quick_two_sum(s1, c3, s2);
				s1 = quick_two_sum(s1, c3, s2);
				if (s2 != 0.0)
				{
					//	s2 = quick_two_sum(s2, c4, s3);
					//_twoSum->quick_two_sumA(&s2, &c4, &t, &s3);
					//s2 = t;
					s2 = quick_two_sum(s2, c4, s3);
				}
				else
				{
					//	s1 = quick_two_sum(s1, c4, s2);
					//_twoSum->quick_two_sumA(&s1, &c4, &t, &s2);
					//s1 = t;
					s1 = quick_two_sum(s1, c4, s2);
				}
			}
			else
			{
				s0 = quick_two_sum(s0, c2, s1);
				if (s1 != 0.0)
				{
					s1 = quick_two_sum(s1, c3, s2);
					if (s2 != 0.0)
						s2 = quick_two_sum(s2, c4, s3);
					else
						s1 = quick_two_sum(s1, c4, s2);
				}
				else
				{
					s0 = quick_two_sum(s0, c3, s1);
					if (s1 != 0.0)
						s1 = quick_two_sum(s1, c4, s2);
					else
						s0 = quick_two_sum(s0, c4, s1);
				}
			}
		}

		c0 = s0;
		c1 = s1;
		c2 = s2;
		c3 = s3;
	}

	double qpMath::two_sum(double a, double b, double &err)
	{
		double s = a + b;

		//err = (a - (s - bb)) + (b - bb);

		double bb = s - a;
		double bbb = b - bb;
		double sbb = s - bb;
		double asbb = a - sbb;

		err = asbb + bbb;
		return s;
	}

	double qpMath::quick_two_sum(double a, double b, double &err)
	{
		double s = a + b;
		double bb = s - a;
		err = b - bb;

		return s;
	}

	void qpMath::three_sum2(double &a, double &b, double c)
	{
		//_twoSum->three_sum2(&a, &b, &c);

		//t1 = two_sum(a, b, t2);
		//two_sumA(a, b, _t1, _t2);
		double e1;
		double t1 = two_sum(a, b, e1);

		//a = two_sum(c, t1, t3);
		//two_sumA(c, _t1, a, _t3);
		double e2;
		a = two_sum(c, t1, e2);

		//b = t2 + t3;
		b = e1 + e2;
	}

	void qpMath::three_sum(double &a, double &b, double &c)
	{
		//_twoSum->three_sum2(&a, &b, &c);

		//t1 = two_sum(a, b, t2);
		//two_sumA(a, b, _t1, _t2);
		double e1;
		double t1 = two_sum(a, b, e1);

		//a = two_sum(c, t1, t3);
		//two_sumA(c, _t1, a, _t3);
		double e2;
		a = two_sum(c, t1, e2);

		//b = two_sum(t2, t3, c);
		b = two_sum(e1, e2, c);
	}

	double qpMath::two_prod(double a, double b, double &err)
	{
		double p = a * b;

		double a_hi, a_lo, b_hi, b_lo;

		split(a, a_hi, a_lo);
		split(b, b_hi, b_lo);

		//err = ((a_hi * b_hi - p) + a_hi * b_lo + a_lo * b_hi) + a_lo * b_lo;

		double ah_m_bh = a_hi * b_hi;
		double ah_m_bh_minus_p = ah_m_bh - p;

		double e1;
		double diff = two_sum(ah_m_bh, -p, e1);

		double ah_m_bl = a_hi * b_lo;
		double s1 = ah_m_bh_minus_p + ah_m_bl;

		double al_m_bh = a_lo * b_hi;

		double s2 = s1 + al_m_bh;

		double al_m_bl = a_lo * b_lo;
		err = s2 + al_m_bl;

		three_sum2(p, err, e1);

		return p;
	}

	double qpMath::two_sqr(double a, double &err)
	{
		double p = a * a;

		double a_hi, a_lo;

		split(a, a_hi, a_lo);

		//double hi, lo;
		//split(a, hi, lo);
		//err = ((hi * hi - p) + 2.0 * hi * lo) + lo * lo;

		//vdMul(_len, _a_hi, _a_hi, _ah_m_bh);
		double ah_m_ah = a_hi * a_hi;

		//vdSub(_len, _ah_m_bh, p, _ah_m_bh_minus_p);
		double ah_m_ah_minus_p = ah_m_ah - p;

		//vdMul(_len, _a_hi, _a_lo, _ah_m_bl);
		double twice_ah_m_bl = 2 * a_hi * a_lo;

		//vdMul(_len, _ah_m_bl, _two, _2ah_m_bl);

		//vdAdd(_len, _ah_m_bh_minus_p, _2ah_m_bl, _s1);
		double s1 = ah_m_ah_minus_p + twice_ah_m_bl;

		//vdMul(_len, _a_lo, _a_lo, _al_m_bl);
		double al_m_bl = a_lo * a_lo;

		//vdAdd(_len, _s1, _al_m_bl, err);
		err = s1 + al_m_bl;

		return p;
	}

	void qpMath::split(double a, double &hi, double &lo)
	{
		double splitMult = pow(2, 27) + 1.0;

		double temp = a * splitMult;

		//hi = temp - (temp - a);
		double temp_minus_a = temp - a;
		hi = temp - temp_minus_a;
		lo = a - hi;
	}
	
}
