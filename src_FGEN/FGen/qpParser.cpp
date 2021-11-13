

#include "stdafx.h"
#include <algorithm>
#include <cctype>
#include <stdio.h>
#include <iostream>
#include <iomanip>
#include <sstream>
#include <limits>
#include <cmath>

#include "qpParser.h"
#include "qpMath.h"

namespace FGen
{
	qpParser::qpParser()
	{
		_qpCalc = new qpMath();
	}

	qpParser::~qpParser()
	{
		delete _qpCalc;
		_qpCalc = 0;
	}

	std::string qpParser::ToStr(double hi, double lo)
	{
		std::string result = to_string(hi, lo, 32, 1, 0, false, false, 'u');
		return result;
	}

	/* This routine is called whenever a fatal error occurs. */
	void qpParser::error(std::string const& msg) const
	{
		std::cerr << "ERROR " << msg << std::endl;
	}

	void qpParser::Negate(double &hi, double &lo) const
	{
		hi = -hi;
		lo = -lo;
	}

	int qpParser::GetExp(double hi, double lo) const
	{
		int pexp = 0;
		std::frexp(hi, &pexp);
		return pexp;
	}

	//dd_real frexp(dd_real const& a, int* pexp)
	//{
	//	double hi = std::frexp(a._hi(), pexp);
	//	double lo = std::ldexp(a._lo(), -*pexp);
	//	return dd_real(hi, lo);
	//}

	// InPlace
	void qpParser::LdExpInPlace(double &hi, double &lo, int exp) const
	{
		double rHi = std::ldexp(hi, exp);
		double rLo = std::ldexp(lo, exp);

		hi = rHi;
		lo = rLo;
	}

	//dd_real ldexp(dd_real const& a, int exp)
	//{
	//	static_assert(numeric_limits< dd_real >::radix == 2, "CONFIGURATION: dd_real radix must be 2!");
	//	static_assert(numeric_limits< double >::radix == 2, "CONFIGURATION: double radix must be 2!");
	//
	//	return dd_real(std::ldexp(a._hi(), exp), std::ldexp(a._lo(), exp));
	//}

	void qpParser::CopySign(double ahi, double alo, double bhi, double &rhi, double &rlo) const
	{
		rhi = ahi;
		rlo = alo;

		double signA = _copysign(1.0, ahi);
		double signB = _copysign(1.0, bhi);

		//return signA != signB ? -a : a;

		if (signA != signB) {
			Negate(rhi, rlo);
		}
	}

	bool isinf(double a)
	{
		return (::_fpclass(a) & (_FPCLASS_NINF | _FPCLASS_PINF)) != 0;
	}

	void qpParser::MulAdd(double ahi, double alo, double bhi, double blo, double chi, double clo, double &rhi, double &rlo) const
	{
		double p[4];

		//	qd_mul(a, b, p);
		_qpCalc->mulQpByQpROp(ahi, alo, bhi, blo, p);

		//	qd_add(p, c, p);
		_qpCalc->addOpAndQp(p, chi, clo, p);

		//	p[0] = qd::two_sum(p[0], p[1] + p[2] + p[3], p[1]);
		rhi = _qpCalc->two_sum(p[0], p[1] + p[2] + p[3], rlo);

		//	return dd_real(p[0], p[1]);
	}

	void qpParser::ReciprocalInPlace(double &ahi, double &alo) const
	{
		double hi;
		double lo;
		//double hi2;
		//double lo2;

		//	if (iszero(a))
		//		return std::copysign(std::numeric_limits< dd_real >::infinity(), a);

		if (ahi == 0) {
			hi = std::numeric_limits<double>::infinity();
			lo = 0;
			CopySign(hi, lo, ahi, ahi, alo);
			return;
		}

		//	if (std::isinf(a))
		//		return std::copysign(_zero, a);

		if (isinf(ahi)) {
			hi = 0;
			lo = 0;
			CopySign(hi, lo, ahi, ahi, alo);
			return;
		}

		//	double q1 = 1.0 / a._hi();  /* approximate quotient */
		double q1 = 1.0 / ahi;

		//	if (QD_ISFINITE(q1))
		//	{

		if(_finite(q1) != 0)
		{
			//	dd_real r = std::Fma(-q1, a, 1.0);
			double rhi = 0.0;
			double rlo = 0.0;
			MulAdd(-q1, 0, ahi, alo, 1.0, 0.0, rhi, rlo);

			//	double q2 = r._hi() / a._hi();
			double q2 = rhi / ahi;

			//	r = std::Fma(-q2, a, r);
			MulAdd(-q2, 0, ahi, alo, rhi, rlo, rhi, rlo);

			//	double q3 = r._hi() / a._hi();
			double q3 = rhi / ahi;

			//	qd::three_sum(q1, q2, q3);
			_qpCalc->three_sum2(q1, q2, q3);

			//	return dd_real(q1, q2);
			ahi = q1;
			alo = q2;
		}
		else
		{
			//	return dd_real(q1, 0.0);
			ahi = q1;
			alo = 0.0;
		}
	}

	void qpParser::Pown(double hi, int n, double &rHi, double &rLo) const
	{
		//	if (std::isnan(a))
		//		return a;
		if (isnan(hi)) {
			rHi = hi;
			rLo = 0;
			return;
		}

		int N = std::abs(n);
		double sHi;
		double sLo;
		double sHi2;
		double sLo2;

		switch (N)
		{
		case 0:
			if (hi == 0.0)
			{
				error("(dd_real::pown): Invalid argument.");
				//errno = EDOM;


				//return std::numeric_limits< dd_real >::quiet_NaN();
				rHi = std::numeric_limits<double>::quiet_NaN();
				rLo = 0.0;
				return;
			}
			rHi = 1.0;
			rLo = 0.0;
			return;

		case 1:
			//s = a;
			sHi = hi;
			sLo = 0;
			break;

		case 2:
			//s = sqr(a);

			sHi2 = hi;
			sLo2 = 0;
			_qpCalc->sqrQp(sHi2, sLo2, sHi, sLo);
			break;

		default:							/* Use binary exponentiation */
		{
			//dd_real r = a;
			double tHi = hi;
			double tLo = 0;
			double tHi2;
			double tLo2;

			//s = 1.0;
			sHi = 1.0;
			sLo = 0.0;

			while (N > 0)
			{
				if (N % 2 == 1)
				{
					//s *= r;
					_qpCalc->mulQpByQp(sHi, sLo, tHi, tLo, sHi2, sLo2);
					sHi = sHi2;
					sLo = sLo2;
				}
				N /= 2;
				if (N > 0)
				{
					//r = sqr(r);
					_qpCalc->sqrQp(tHi, tLo, tHi2, tLo2);
					tHi = tHi2;
					tLo = tLo2;
				}
			}
		}

		break;
		}

		/* Compute the reciprocal if n is negative. */
		//return n < 0 ? reciprocal(s) : s;
		if (n < 0) {
			ReciprocalInPlace(sHi, sLo);
		}

		rHi = sHi;
		rLo = sLo;
	}

	void qpParser::MulQpByQpInPlace(double &ahi, double &alo, double bhi, double blo) const
	{
		double rhi;
		double rlo;

		_qpCalc->mulQpByQp(ahi, alo, bhi, blo, rhi, rlo);

		ahi = rhi;
		alo = rlo;
	}

	void qpParser::MulQpByDInPlace(double &ahi, double &alo, double b) const
	{
		double rhi;
		double rlo;

		_qpCalc->mulQpByD(ahi, alo, b, rhi, rlo);

		ahi = rhi;
		alo = rlo;
	}

	void qpParser::AddDToQpInPlace(double &ahi, double &alo, double b) const
	{
		double rhi;
		double rlo;
		_qpCalc->addDToQp(ahi, alo, b, rhi, rlo);

		ahi = rhi;
		alo = rlo;
	}

	void qpParser::SubDFromQpInPlace(double &ahi, double &alo, double b) const
	{
		double rhi;
		double rlo;

		_qpCalc->addDToQp(ahi, alo, -b, rhi, rlo);

		ahi = rhi;
		alo = rlo;
	}

	bool qpParser::geD(double &ahi, double &alo, double b) const
	{
		if (isnan(ahi) || isnan(b)) return false;
		return ahi > b || (ahi == b && alo >= 0.0);
	}

	bool qpParser::ltD(double &ahi, double &alo, double b) const
	{
		if (isnan(ahi) || isnan(b)) return false;
		return ahi < b || (ahi == b && alo < 0.0);
	}

	bool qpParser::isinf(double hi) const
	{
		int y = ::_fpclass(hi);
		int mask = (_FPCLASS_NINF | _FPCLASS_PINF);
		y &= mask;

		return y != 0;
	}

	void qpParser::to_digits(char* s, int& expn, int precision, double hi, double lo) const
	{
		int D = precision + 1;  /* number of digits to compute */

		double rHi = hi;
		double rLo = lo;

		//std::string * shis = new std::string[50];
		//std::string * slos = new std::string[50];

		//std::string * shis2 = new std::string[50];
		//std::string * slos2 = new std::string[50];

		//dd_real r = std::abs(*this);
		if (hi < 0) {
			Negate(rHi, rLo);
		}

		if (hi == 0.0)
		{
			/* this == 0.0 */
			expn = 0;
			for (int i = 0; i < precision; i++)
				s[i] = '0';
			return;
		}

		///* First determine the (approximate) exponent. */
		//int e;  /* exponent */
		//std::frexp(*this, &e);	//	e is appropriate for 0.5 <= x < 1
		//std::ldexp(r, 1);			//	adjust e, r
		//e = (_log2 * (double)e).toInt();

		int e = GetExp(hi, lo);
		--e;
		e = static_cast<int>(log10(2.0) * (double)e);

		if (e < 0)
		{
			if (e < -300)
			{
				//	r = std::ldexp(r, 53);
				LdExpInPlace(rHi, rLo, 53);

				//	r *= pown(_ten, -e);
				double mx = pow(10, -e);
				MulQpByDInPlace(rHi, rLo, mx);

				//	r = std::ldexp(r, -53);
				LdExpInPlace(rHi, rLo, -53);
			}
			else
			{
				//	r *= pown(_ten, -e);
				double mx = pow(10, -e);
				MulQpByDInPlace(rHi, rLo, mx);
			}
		}
		else
			if (e > 0)
			{
				if (e > 300)
				{
					//	r = std::ldexp(r, -53);
					LdExpInPlace(rHi, rLo, -53);

					//	r /= pown(_ten, e);
					double mx = pow(10, e);
					MulQpByDInPlace(rHi, rLo, mx);

					//	r = std::ldexp(r, +53);
					LdExpInPlace(rHi, rLo, 53);
				}
				else
				{
					//	r /= pown(_ten, e);
					double mx = pow(10, e);
					MulQpByDInPlace(rHi, rLo, mx);
				}
			}

		///* Fix exponent if we are off by one */
		//if (r >= _ten)
		//{
		//	r /= _ten;
		//	++e;
		//}
		//else
		//	if (r < 1.0)
		//	{
		//		r *= _ten;
		//		--e;
		//	}

		if (geD(rHi, rLo, 10.0))
		{
			MulQpByDInPlace(rHi, rLo, 0.1);
			++e;
		}
		else 
			if (ltD(rHi, rLo, 1.0))
			{
				MulQpByDInPlace(rHi, rLo, 10.0);
				--e;
			}

		//if ((r >= _ten) || (r < _one))
		//{
		//	error("(dd_real::to_digits): can't compute exponent.");
		//	return;
		//}

		if (geD(rHi, rLo, 10.0) || ltD(rHi, rLo, 1.0))
		{
			error("(qpParser::to_digits): can't compute exponent.");
			return;
		}

		///* Extract the digits */
		for (int i = 0; i < D; i++)
		{
			//d = static_cast<int>(r.x[0]);
			int d = static_cast<int>(rHi);

			//r -= d;
			AddDToQpInPlace(rHi, rLo, -d);
			//SubDFromQpInPlace(rHi, rLo, d);
			//shis[i] = GetStr(rHi);
			//slos[i] = GetStr(rLo);

			//r *= 10.0;
			MulQpByDInPlace(rHi, rLo, 10.0);
			//shis2[i] = GetStr(rHi);
			//slos2[i] = GetStr(rLo);
		
			s[i] = static_cast<char>(d + '0');
		}

		///* Fix out of range digits. */
		for (int i = D - 1; i > 0; i--)
		{
			if (s[i] < '0')
			{
				s[i - 1]--;
				s[i] += 10;
			}
			else
				if (s[i] > '9')
				{
					s[i - 1]++;
					s[i] -= 10;
				}
		}

		if (s[0] <= '0')
		{
			error("(qdParser::to_digits): non-positive leading digit.");
			return;
		}

		///* Round, handle carry */
		if (s[D - 1] >= '5')
		{
			s[D - 2]++;

			int i = D - 2;
			while (i > 0 && s[i] > '9')
			{
				s[i] -= 10;
				s[--i]++;
			}
		}

		///* If first digit is 10, shift everything. */
		if (s[0] > '9')
		{
			++e;
			for (int i = precision; i >= 2; i--)
				s[i] = s[i - 1];
			s[0] = '1';
			s[1] = '0';
		}

		s[precision] = 0;
		expn = e;
	}

	std::string qpParser::to_string(double hi, double lo, std::streamsize precision, int width, std::ios_base::fmtflags fmt, bool showpos, bool uppercase, char fill) const
	{
		std::string s;
		bool fixed = (fmt & std::ios_base::fixed) != 0;
		bool sgn = true;
		int i, e = 0;

		if (isnan(hi)) {
			s = uppercase ? "NAN" : "nan";
			sgn = false;
		}
		else {
			//		if (std::signbit(*this))
			//			s += '-';
			//		else if (showpos)
			//			s += '+';
			//		else
			//			sgn = false;

			if (hi < 0.0)
				s += '-';
			else if (showpos)
				s += '+';
			else
				sgn = false;

			//		if (isinf()) {
			//			s += uppercase ? "INF" : "inf";
			//		}

			if (isinf(hi)) {
				s += uppercase ? "INF" : "inf";
			}

			//		else if (*this == 0.0) {
			//			/* Zero case */
			//			s += '0';
			//			if (precision > 0) {
			//				s += '.';
			//				s.append(static_cast<unsigned int>(precision), '0');
			//			}
			//		}

			else if (hi == 0.0) {
				/* Zero case */
				s += '0';
				if (precision > 0) {
					s += '.';
					s.append(static_cast<unsigned int>(precision), '0');
				}
			}

			//	else {
			//		/* Non-zero case */
			//		int off = (fixed ? (1 + std::floor(std::log10(std::abs(*this)))).toInt() : 1);
			//		int d = static_cast<int>(precision) + off;
			//
			//		int d_with_extra = d;

			else {
				/* Non-zero case */
				int off;
				if (fixed) {
					// TODO: Fix GetOffset when format is Fixed.
					off = 1; //(1 + std::floor(std::log10(std::abs(*this)))).toInt();
				}
				else {
					off = 1;
				}

				int d = static_cast<int>(precision) + off;

				int d_with_extra = d;

				//	if (fixed)
				//	d_with_extra = std::max(60, d); // longer than the max accuracy for DD

				if (fixed)
					d_with_extra = 60 > d ? 60 : d; // longer than the max accuracy for DD

				// highly special case - fixed mode, precision is zero, abs(*this) < 1.0
				// without this trap a number like 0.9 printed fixed with 0 precision prints as 0
				// should be rounded to 1.

					//			if (fixed && (precision == 0) && (std::abs(*this) < 1.0)) {
					//				if (std::abs(*this) >= 0.5)
					//					s += '1';
					//				else
					//					s += '0';
					//
					//				return s;
					//			}

				//if (fixed && (precision == 0) && (std::abs(*this) < 1.0)) {
				//	if (std::abs(*this) >= 0.5)
				//		s += '1';
				//	else
				//		s += '0';
				//	
				//	return s;
				//}
				//
					//
					//			// handle near zero to working precision (but not exactly zero)
					//			if (fixed && d <= 0) {
					//				s += '0';
					//				if (precision > 0) {
					//					s += '.';
					//					s.append(static_cast<unsigned int>(precision), '0');
					//				}
					//			}
					//			else { // default
					//
					//				char *t; //  = new char[d+1];
					//				int j;
					//
					//				if (fixed) {
					//					t = new char[d_with_extra + 1];
					//					to_digits(t, e, d_with_extra);
					//				}
					//				else {
					//					t = new char[d + 1];
					//					to_digits(t, e, d);
					//				}
					//
					//				if (fixed) {
					//					// fix the string if it's been computed incorrectly
					//					// round here in the decimal string if required
					//					round_string(t, d + 1, &off);
					//
					//					if (off > 0) {
					//						for (i = 0; i < off; i++) s += t[i];
					//						if (precision > 0) {
					//							s += '.';
					//							for (j = 0; j < precision; j++, i++) s += t[i];
					//						}
					//					}
					//					else {
					//						s += "0.";
					//						if (off < 0) s.append(-off, '0');
					//						for (i = 0; i < d; i++) s += t[i];
					//					}
					//				}
					//				else {
					//					s += t[0];
					//					if (precision > 0) s += '.';
					//
					//					for (i = 1; i <= precision; i++)
					//						s += t[i];
					//
					//				}
					//				delete[] t;
					//			}

				// default
				char *t = new char[d + 1];
				to_digits(t, e, d, hi, lo);
				s += t[0];
				if (precision > 0) s += '.';

				for (i = 1; i <= precision; i++)
					s += t[i];

				delete[] t;

				//		// trap for improper offset with large values
				//		// without this trap, output of values of the for 10^j - 1 fail for j > 28
				//		// and are output with the point in the wrong place, leading to a dramatically off value
				//		if (fixed && (precision > 0)) {
				//			// make sure that the value isn't dramatically larger
				//			double from_string = atof(s.c_str());
				//
				//			// if this ratio is large, then we've got problems
				//			if (fabs(from_string / this->x[0]) > 3.0) {
				//
				//				// loop on the string, find the point, move it up one
				//				// don't act on the first character
				//				for (std::string::size_type i = 1; i < s.length(); i++)
				//				{
				//					if (s[i] == '.') {
				//						s[i] = s[i - 1];
				//						s[i - 1] = '.';
				//						break;
				//					}
				//				}
				//
				//				from_string = atof(s.c_str());
				//				// if this ratio is large, then the string has not been fixed
				//				if (fabs(from_string / this->x[0]) > 3.0) {
				//					error("Re-rounding unsuccessful in large number fixed point trap.");
				//				}
				//			}
				//		}
				//
				//

				if (!fixed && !isinf(hi)) {
					/* Fill in exponent part */
					s += uppercase ? 'E' : 'e';
					append_expn(s, e);
				}
			}
		}

		/* Fill in the blanks */
		int len = static_cast<int>(s.length());
		if (len < width)
		{
			int delta = static_cast<int>(width) - len;
			if (fmt & std::ios_base::internal)
			{
				if (sgn)
					s.insert(static_cast<std::string::size_type>(1), delta, fill);
				else
					s.insert(static_cast<std::string::size_type>(0), delta, fill);
			}
			else if (fmt & std::ios_base::left)
			{
				s.append(delta, fill);
			}
			else
			{
				s.insert(static_cast<std::string::size_type>(0), delta, fill);
			}
		}

		return s;
	}

	void qpParser::append_expn(std::string& str, int expn) const
	{
		int k;

		str += (expn < 0 ? '-' : '+');
		expn = std::abs(expn);

		if (expn >= 100)
		{
			k = (expn / 100);
			str += static_cast<char>('0' + k);
			expn -= 100 * k;
		}

		k = (expn / 10);
		str += static_cast<char>('0' + k);
		expn -= 10 * k;

		str += static_cast<char>('0' + expn);
	}

	//std::string dd_real::to_string(std::streamsize precision, std::streamsize width, std::ios_base::fmtflags fmt, bool showpos, bool uppercase, char fill) const
	//{
	//	std::string s;
	//	bool fixed = (fmt & std::ios_base::fixed) != 0;
	//	bool sgn = true;
	//	int i, e = 0;
	//
	//	if (isnan()) {
	//		s = uppercase ? "NAN" : "nan";
	//		sgn = false;
	//	}
	//	else {
	//		if (std::signbit(*this))
	//			s += '-';
	//		else if (showpos)
	//			s += '+';
	//		else
	//			sgn = false;
	//
	//		if (isinf()) {
	//			s += uppercase ? "INF" : "inf";
	//		}
	//		else if (*this == 0.0) {
	//			/* Zero case */
	//			s += '0';
	//			if (precision > 0) {
	//				s += '.';
	//				s.append(static_cast<unsigned int>(precision), '0');
	//			}
	//		}
	//		else {
	//			/* Non-zero case */
	//			int off = (fixed ? (1 + std::floor(std::log10(std::abs(*this)))).toInt() : 1);
	//			int d = static_cast<int>(precision) + off;
	//
	//			int d_with_extra = d;
	//			if (fixed)
	//				d_with_extra = std::max(60, d); // longer than the max accuracy for DD
	//
	//			// highly special case - fixed mode, precision is zero, abs(*this) < 1.0
	//			// without this trap a number like 0.9 printed fixed with 0 precision prints as 0
	//			// should be rounded to 1.
	//			if (fixed && (precision == 0) && (std::abs(*this) < 1.0)) {
	//				if (std::abs(*this) >= 0.5)
	//					s += '1';
	//				else
	//					s += '0';
	//
	//				return s;
	//			}
	//
	//			// handle near zero to working precision (but not exactly zero)
	//			if (fixed && d <= 0) {
	//				s += '0';
	//				if (precision > 0) {
	//					s += '.';
	//					s.append(static_cast<unsigned int>(precision), '0');
	//				}
	//			}
	//			else { // default
	//
	//				char *t; //  = new char[d+1];
	//				int j;
	//
	//				if (fixed) {
	//					t = new char[d_with_extra + 1];
	//					to_digits(t, e, d_with_extra);
	//				}
	//				else {
	//					t = new char[d + 1];
	//					to_digits(t, e, d);
	//				}
	//
	//				if (fixed) {
	//					// fix the string if it's been computed incorrectly
	//					// round here in the decimal string if required
	//					round_string(t, d + 1, &off);
	//
	//					if (off > 0) {
	//						for (i = 0; i < off; i++) s += t[i];
	//						if (precision > 0) {
	//							s += '.';
	//							for (j = 0; j < precision; j++, i++) s += t[i];
	//						}
	//					}
	//					else {
	//						s += "0.";
	//						if (off < 0) s.append(-off, '0');
	//						for (i = 0; i < d; i++) s += t[i];
	//					}
	//				}
	//				else {
	//					s += t[0];
	//					if (precision > 0) s += '.';
	//
	//					for (i = 1; i <= precision; i++)
	//						s += t[i];
	//
	//				}
	//				delete[] t;
	//			}
	//		}
	//
	//		// trap for improper offset with large values
	//		// without this trap, output of values of the for 10^j - 1 fail for j > 28
	//		// and are output with the point in the wrong place, leading to a dramatically off value
	//		if (fixed && (precision > 0)) {
	//			// make sure that the value isn't dramatically larger
	//			double from_string = atof(s.c_str());
	//
	//			// if this ratio is large, then we've got problems
	//			if (fabs(from_string / this->x[0]) > 3.0) {
	//
	//				// loop on the string, find the point, move it up one
	//				// don't act on the first character
	//				for (std::string::size_type i = 1; i < s.length(); i++)
	//				{
	//					if (s[i] == '.') {
	//						s[i] = s[i - 1];
	//						s[i - 1] = '.';
	//						break;
	//					}
	//				}
	//
	//				from_string = atof(s.c_str());
	//				// if this ratio is large, then the string has not been fixed
	//				if (fabs(from_string / this->x[0]) > 3.0) {
	//					error("Re-rounding unsuccessful in large number fixed point trap.");
	//				}
	//			}
	//		}
	//
	//
	//		if (!fixed && !isinf()) {
	//			/* Fill in exponent part */
	//			s += uppercase ? 'E' : 'e';
	//			append_expn(s, e);
	//		}
	//	}
	//
	//	/* Fill in the blanks */
	//	int len = s.length();
	//
	//	if (len < width)
	//	{
	//		int delta = static_cast<int>(width) - len;
	//		if (fmt & std::ios_base::internal)
	//		{
	//			if (sgn)
	//				s.insert(static_cast<std::string::size_type>(1), delta, fill);
	//			else
	//				s.insert(static_cast<std::string::size_type>(0), delta, fill);
	//		}
	//		else if (fmt & std::ios_base::left)
	//		{
	//			s.append(delta, fill);
	//		}
	//		else
	//		{
	//			s.insert(static_cast<std::string::size_type>(0), delta, fill);
	//		}
	//	}
	//
	//	return s;
	//}

	//int qp::read(std::string const& s, qp& a)
	//{
	//	char const* p = s.c_str();
	//	char ch;
	//	int sign = 0;
	//	int point = -1;
	//	int nd = 0;
	//	int e = 0;
	//	bool done = false;
	//	qp r = 0.0;
	//	int nread;
	//
	//	/* Skip any leading spaces */
	//	while (std::isspace(*p))
	//		++p;
	//
	//	while (!done && (ch = *p) != '\0')
	//	{
	//		if (std::isdigit(ch))
	//		{
	//			int d = ch - '0';
	//			r *= 10.0;
	//			r += static_cast<double>(d);
	//			nd++;
	//		}
	//		else
	//		{
	//			switch (ch)
	//			{
	//			case '.':
	//				if (point >= 0)
	//					return -1;
	//				point = nd;
	//				break;
	//
	//			case '-':
	//			case '+':
	//				if (sign != 0 || nd > 0)
	//					return -1;
	//				sign = (ch == '-') ? -1 : 1;
	//				break;
	//
	//			case 'E':
	//			case 'e':
	//				nread = std::sscanf(p + 1, "%d", &e);
	//				done = true;
	//				if (nread != 1)
	//					return -1;
	//				break;
	//
	//			default:
	//				return -1;
	//			}
	//		}
	//
	//		++p;
	//	}
	//
	//	if (point >= 0)
	//	{
	//		e -= (nd - point);
	//	}
	//
	//	if (e > 0)
	//	{
	//		r *= pown(_ten, e);
	//	}
	//	else
	//		if (e < 0)
	//			r /= pown(_ten, -e);
	//
	//	a = (sign == -1) ? -r : r;
	//	return 0;
	//}

	int qpParser::Read(std::string const& s, double &hi, double &lo) const
	{

		double * his = new double[50];
		double * los = new double[50];

		double * his2 = new double[50];
		double * los2 = new double[50];

		std::string * shis = new std::string[50];
		std::string * slos = new std::string[50];

		std::string * shis2 = new std::string[50];
		std::string * slos2 = new std::string[50];


		char const* p = s.c_str();
		char ch;
		int sign = 0;
		int point = -1;
		int nd = 0;
		int e = 0;
		bool done = false;

		//qp r = 0.0;
		hi = 0;
		lo = 0;

		int nread;
	
		/* Skip any leading spaces */
		while (std::isspace(*p))
			++p;
	
		while (!done && (ch = *p) != '\0')
		{
			if (std::isdigit(ch))
			{
				if (nd > 15)
				{
					int aa = 0;
				}
				int d = ch - '0';
				//r *= 10.0;
				MulQpByDInPlace(hi, lo, 10.0);
				his[nd] = hi;
				los[nd] = lo;
				shis[nd] = GetStr(hi);
				slos[nd] = GetStr(lo);

				//r += static_cast<double>(d);
				AddDToQpInPlace(hi, lo, d);

				his2[nd] = hi;
				los2[nd] = lo;
				shis2[nd] = GetStr(hi);
				slos2[nd] = GetStr(lo);


				nd++;
			}
			else
			{
				switch (ch)
				{
				case '.':
					if (point >= 0)
						return -1;
					point = nd;
					break;
	
				case '-':
				case '+':
					if (sign != 0 || nd > 0)
						return -1;
					sign = (ch == '-') ? -1 : 1;
					break;
	
				case 'E':
				case 'e':
					nread = sscanf_s(p + 1, "%d", &e);

					done = true;
					if (nread != 1)
						return -1;
					break;
	
				default:
					return -1;
				}
			}
	
			++p;
		}
	
		if (point >= 0)
		{
			e -= (nd - point);
		}

		std::string shiF = GetStr(hi);
		std::string sloF = GetStr(lo);
	
		if (e > 0)
		{
			//r *= pown(_ten, e);
			//double mx = pow(10, e);
			double mxHi = 0;
			double mxLo = 0;
			Pown(10, e, mxHi, mxLo);
			MulQpByQpInPlace(hi, lo, mxHi, mxLo);
		}
		else
			if (e < 0)
			{
				//r /= pown(_ten, -e);
				//double mx = pow(10, e);
				//MulQpByDInPlace(hi, lo, mx);
				double mxHi = 0;
				double mxLo = 0;
				Pown(10, e, mxHi, mxLo);
				MulQpByQpInPlace(hi, lo, mxHi, mxLo);
			}
	
		//a = (sign == -1) ? -r : r;
		if (sign == -1) {
			hi = -hi;
			lo = -lo;
		}

		std::string shiF2 = GetStr(hi);
		std::string sloF2 = GetStr(lo);

		return 0;
	}

	std::string qpParser::GetStr(double x) const
	{
		std::ostringstream strout;
		strout << std::fixed << std::setprecision(22) << x;
		std::string str = strout.str();
		return str;
	}

}
