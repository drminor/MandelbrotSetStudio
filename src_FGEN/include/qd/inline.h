/*
 * include/inline.h
 *
 * This work was supported by the Director, Office of Science, Division
 * of Mathematical, Information, and Computational Sciences of the
 * U.S. Department of Energy under contract number DE-AC03-76SF00098.
 *
 * Copyright (c) 2000-2001
 *
 * This file contains the basic functions used both by double-double
 * and quad-double package.  These are declared as inline functions as
 * they are the smallest building blocks of the double-double and 
 * quad-double arithmetic.
 */
#ifndef _QD_INLINE_H
#define _QD_INLINE_H

#include <cmath>
#include <limits>

#include <qd/qd_config.h>

namespace qd
{

	static double const _d_inf = std::numeric_limits<double>::infinity();

	/* Computes fl(a+b) and err(a+b).  Assumes |a| >= |b|. */
	inline double quick_two_sum( double a, double b, double& err )
	{
		double s = a + b;
		err = QD_ISFINITE( s ) ? b - (s - a) : 0.0;
		return s;
	}

	/* Computes fl(a-b) and err(a-b).  Assumes |a| >= |b| */
	inline double quick_two_diff( double a, double b, double& err )
	{
		double s = a - b;
		err = QD_ISFINITE( s ) ? (a - s) - b : 0.0;
		return s;
	}

	/* Computes fl(a+b) and err(a+b).  */
	inline double two_sum( double a, double b, double& err )
	{
		double s = a + b;
		if ( QD_ISFINITE( s ) )
		{
			double bb = s - a;
			err = (a - (s - bb)) + (b - bb);
		}
		else
			err = 0;
		return s;
	}

	/* Computes fl(a-b) and err(a-b).  */
	inline double two_diff( double a, double b, double& err )
	{
		double s = a - b;
		if ( QD_ISFINITE( s ) )
		{
			double bb = s - a;
			err = (a - (s - bb)) - (b + bb);
		}
		else
			err = 0.0;
		return s;
	}

#if !defined( QD_FMS )
	/* Computes high word and lo word of a */
	inline void split( double a, double& hi, double& lo )
	{
		int const QD_BITS = ( std::numeric_limits< double >::digits + 1 ) / 2;
		static double const QD_SPLITTER     = std::ldexp( 1.0, QD_BITS ) + 1.0;
		static double const QD_SPLIT_THRESH = std::ldexp( (std::numeric_limits< double >::max)(), -QD_BITS - 1 );

		double temp;
			
		if ( std::abs( a ) > QD_SPLIT_THRESH )
		{
			a = std::ldexp( a, -QD_BITS - 1 );
			temp = QD_SPLITTER * a;
			hi = temp - (temp - a);
			lo = a - hi;
			hi = std::ldexp( hi, QD_BITS + 1 );
			lo = std::ldexp( lo, QD_BITS + 1 );
		}
		else
		{
			temp = QD_SPLITTER * a;
			hi = temp - (temp - a);
			lo = a - hi;
		}
	}
#endif

	/* Computes fl(a*b) and err(a*b). */
	inline double two_prod( double a, double b, double& err )
	{
		double p = a * b;
		if ( QD_ISFINITE( p ))
		{
#if defined( QD_FMS )
			err = QD_FMS(a, b, p);
#else
			double a_hi, a_lo, b_hi, b_lo;
			split( a, a_hi, a_lo );
			split( b, b_hi, b_lo );
			err = ((a_hi * b_hi - p) + a_hi * b_lo + a_lo * b_hi) + a_lo * b_lo;
#endif
		}
		else
			err = 0.0;
		return p;
	}

/* Computes fl(a*a) and err(a*a).  Faster than the above method. */
	inline double two_sqr( double a, double& err )
	{
		double p = a * a;
		if ( QD_ISFINITE( p ) )
		{
#if defined( QD_FMS )
			err = QD_FMS(a, a, p);
#else
			double hi, lo;
			split( a, hi, lo );
			err = ((hi * hi - p) + 2.0 * hi * lo) + lo * lo;
#endif
		}
		else
			err = 0.0;
		return p;
	}

	inline void renorm( double &c0, double &c1, double &c2, double &c3 )
	{
		double s0, s1, s2 = 0.0, s3 = 0.0;

		if (QD_ISINF(c0)) return;

		s0 = quick_two_sum(c2, c3, c3);
		s0 = quick_two_sum(c1, s0, c2);
		c0 = quick_two_sum(c0, s0, c1);

		s0 = c0;
		s1 = c1;
		if (s1 != 0.0)
		{
			s1 = quick_two_sum(s1, c2, s2);
			if (s2 != 0.0)
				s2 = quick_two_sum(s2, c3, s3);
			else
				s1 = quick_two_sum(s1, c3, s2);
		}
		else
		{
			s0 = quick_two_sum(s0, c2, s1);
			if (s1 != 0.0)
				s1 = quick_two_sum(s1, c3, s2);
			else
				s0 = quick_two_sum(s0, c3, s1);
		}

		c0 = s0;
		c1 = s1;
		c2 = s2;
		c3 = s3;
	}

	inline void renorm( double &c0, double &c1, double &c2, double &c3, double &c4 )
	{
		double s0, s1, s2 = 0.0, s3 = 0.0;

		if (QD_ISINF(c0)) return;

		s0 = quick_two_sum(c3, c4, c4);
		s0 = quick_two_sum(c2, s0, c3);
		s0 = quick_two_sum(c1, s0, c2);
		c0 = quick_two_sum(c0, s0, c1);

		s0 = c0;
		s1 = c1;

		s0 = quick_two_sum(c0, c1, s1);
		if (s1 != 0.0)
		{
			s1 = quick_two_sum(s1, c2, s2);
			if (s2 != 0.0)
			{
				s2 = quick_two_sum(s2, c3, s3);
				if (s3 != 0.0)
					s3 += c4;
				else
					s2 += c4;
			}
			else
			{
				s1 = quick_two_sum(s1, c3, s2);
				if (s2 != 0.0)
					s2 = quick_two_sum(s2, c4, s3);
				else
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

		c0 = s0;
		c1 = s1;
		c2 = s2;
		c3 = s3;
	}

	inline void three_sum( double &a, double &b, double &c )
	{
		double t1, t2, t3;
		
		t1 = two_sum(a, b, t2);
		a  = two_sum(c, t1, t3);
		b  = two_sum(t2, t3, c);
	}

	inline void three_sum2( double &a, double &b, double &c )
	{
		double t1, t2, t3;
		t1 = two_sum(a, b, t2);
		a  = two_sum(c, t1, t3);
		b = t2 + t3;
	}

	/* s = quick_three_accum(a, b, c) adds c to the dd-pair (a, b).
	 * If the result does not fit in two doubles, then the sum is 
	 * output into s and (a,b) contains the remainder.  Otherwise
	 * s is zero and (a,b) contains the sum. */
	inline double quick_three_accum(double &a, double &b, double c)
	{
		double s;
		bool za, zb;

		s = two_sum(b, c, b);
		s = two_sum(a, s, a);
		za = (a != 0.0);
		zb = (b != 0.0);

		if (za && zb)
			return s;

		if (!zb)
		{
			b = a;
			a = s;
		}
		else
		{
			a = s;
		}

		return 0.0;
	}

	/* Computes the nearest integer to d. */
	inline double round( double d )
	{
		if (d == std::floor(d))
			return d;
		return std::floor(d + 0.5);
	}

	/* Computes the truncated integer. */
	inline double trunc( double d )
	{
		return (d >= 0.0) ? std::floor(d) : std::ceil(d);
	}

}

#endif /* _QD_INLINE_H */
