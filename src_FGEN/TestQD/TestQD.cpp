// TestQD.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include <algorithm>
#include <cmath>
#include <ctime>
#include <iostream>
#include <limits>
#include <random>
#include <sstream>

#include <qd/dd_real.h>

#include "tests.h"
#include "Utils.h"

std::default_random_engine generator;

void testIO()
{
	std::cout << "Tests of I/O\n";
	std::cout << "============\n";

	std::cout << "max = " << std::numeric_limits< dd_real >::max() << "\n";
	std::cout << "min = " << std::numeric_limits< dd_real >::min() << "\n";
	std::cout << "inf = " << std::numeric_limits< dd_real >::infinity() << "\n";
	std::cout << "qnan = " << std::numeric_limits< dd_real >::quiet_NaN() << "\n";
	std::cout << "snan = " << std::numeric_limits< dd_real >::signaling_NaN() << "\n";
	std::cout << "0.0 = " << dd_real( "0.0" ) << "\n";
	std::cout << "-0.0 = " << dd_real("-0.0") << "\n";
	std::cout << "1.0 = " << dd_real("1.0") << "\n";
	std::cout << "-1.0 = " << dd_real("-1.0") << "\n";
	std::cout << "\n";
}

void testMultDiv()
{
	std::cout << "Tests of multiplication and division\n";
	std::cout << "====================================\n";

	std::uniform_real_distribution< dd_real > distribution1(-1048576.0, 1048576.0);
	int mre;

	std::cout << "valid bits of a * reciprocal(a) == 1.0 = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real a = distribution1(generator);
		if (a != 0.0)
		{
			dd_real b = reciprocal(a);
			dd_real c = a * b;

			mre = std::min(mre, validBits(c, 1.0));
		}
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of b * (a/b) == a = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real a = distribution1(generator);
		dd_real b = distribution1(generator);

		if (b != 0)
		{
			dd_real c = a / b;
			dd_real d = b * c;

			mre = std::min(mre, validBits(d, a));
		}
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "\n";
}

void testClassification()
{
	std::cout << "Tests of classification functions\n";
	std::cout << "=================================\n";

	std::cout << "fpclassify(qnan) = " << std::fpclassify(std::numeric_limits<dd_real>::quiet_NaN()) << "\n";
	std::cout << "fpclassify(snan) = " << std::fpclassify(std::numeric_limits<dd_real>::signaling_NaN()) << "\n";
	std::cout << "fpclassify(-inf) = " << std::fpclassify(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "fpclassify(-1.0) = " << std::fpclassify(dd_real(-1.0)) << "\n";
	std::cout << "fpclassify(-0.0) = " << std::fpclassify(dd_real("-0.0")) << "\n";
	std::cout << "fpclassify(0.0) = " << std::fpclassify(dd_real("0.0")) << "\n";
	std::cout << "fpclassify(1.0) = " << std::fpclassify(dd_real(1.0)) << "\n";
	std::cout << "fpclassify(inf) = " << std::fpclassify(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "\n";

	std::cout << "isfinite(qnan) = " << std::isfinite(std::numeric_limits<dd_real>::quiet_NaN()) << "\n";
	std::cout << "isfinite(snan) = " << std::isfinite(std::numeric_limits<dd_real>::signaling_NaN()) << "\n";
	std::cout << "isfinite(-inf) = " << std::isfinite(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "isfinite(-1.0) = " << std::isfinite(dd_real(-1.0)) << "\n";
	std::cout << "isfinite(-0.0) = " << std::isfinite(dd_real("-0.0")) << "\n";
	std::cout << "isfinite(0.0) = " << std::isfinite(dd_real("0.0")) << "\n";
	std::cout << "isfinite(1.0) = " << std::isfinite(dd_real(1.0)) << "\n";
	std::cout << "isfinite(inf) = " << std::isfinite(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "\n";

	std::cout << "isinf(qnan) = " << std::isinf(std::numeric_limits<dd_real>::quiet_NaN()) << "\n";
	std::cout << "isinf(snan) = " << std::isinf(std::numeric_limits<dd_real>::signaling_NaN()) << "\n";
	std::cout << "isinf(-inf) = " << std::isinf(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "isinf(-1.0) = " << std::isinf(dd_real(-1.0)) << "\n";
	std::cout << "isinf(-0.0) = " << std::isinf(dd_real("-0.0")) << "\n";
	std::cout << "isinf(0.0) = " << std::isinf(dd_real("0.0")) << "\n";
	std::cout << "isinf(1.0) = " << std::isinf(dd_real(1.0)) << "\n";
	std::cout << "isinf(inf) = " << std::isinf(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "\n";

	std::cout << "isnan(qnan) = " << std::isnan(std::numeric_limits<dd_real>::quiet_NaN()) << "\n";
	std::cout << "isnan(snan) = " << std::isnan(std::numeric_limits<dd_real>::signaling_NaN()) << "\n";
	std::cout << "isnan(-inf) = " << std::isnan(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "isnan(-1.0) = " << std::isnan(dd_real(-1.0)) << "\n";
	std::cout << "isnan(-0.0) = " << std::isnan(dd_real("-0.0")) << "\n";
	std::cout << "isnan(0.0) = " << std::isnan(dd_real("0.0")) << "\n";
	std::cout << "isnan(1.0) = " << std::isnan(dd_real(1.0)) << "\n";
	std::cout << "isnan(inf) = " << std::isnan(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "\n";

	std::cout << "isnormal(qnan) = " << std::isnormal(std::numeric_limits<dd_real>::quiet_NaN()) << "\n";
	std::cout << "isnormal(snan) = " << std::isnormal(std::numeric_limits<dd_real>::signaling_NaN()) << "\n";
	std::cout << "isnormal(-inf) = " << std::isnormal(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "isnormal(-1.0) = " << std::isnormal(dd_real(-1.0)) << "\n";
	std::cout << "isnormal(-0.0) = " << std::isnormal(dd_real("-0.0")) << "\n";
	std::cout << "isnormal(0.0) = " << std::isnormal(dd_real("0.0")) << "\n";
	std::cout << "isnormal(1.0) = " << std::isnormal(dd_real(1.0)) << "\n";
	std::cout << "isnormal(inf) = " << std::isnormal(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "\n";

	std::cout << "signbit(-inf) = " << std::signbit(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "signbit(-1.0) = " << std::signbit(dd_real(-1.0)) << "\n";
	std::cout << "signbit(-0.0) = " << std::signbit(dd_real("-0.0")) << "\n";
	std::cout << "signbit(0.0) = " << std::signbit(dd_real("0.0")) << "\n";
	std::cout << "signbit(1.0) = " << std::signbit(dd_real(1.0)) << "\n";
	std::cout << "signbit(inf) = " << std::signbit(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "\n";
}

void testFPManipulation()
{
	std::cout << "Tests of FP manipulation functions\n";
	std::cout << "==================================\n";

#if 0
	//
	//	floating-point manipulation functions
	//
	QD_API dd_real copysign( dd_real const& a, dd_real const& b );
	QD_API dd_real frexp( dd_real const& a, int* pexp );
	QD_API int ilogb( dd_real const& a );
	QD_API dd_real ldexp( dd_real const& a, int exp );
	QD_API dd_real logb( dd_real const& a );
	QD_API dd_real modf( dd_real const& a, dd_real* b );
	QD_API dd_real scalbn( dd_real const& a, int exp );
	QD_API dd_real scalbln( dd_real const& a, long exp );
#endif

	std::cout << "\n";
}

void testRoundingRemainder()
{
	std::cout << "Tests of rounding and remainder functions\n";
	std::cout << "=========================================\n";

	std::cout << "ceil(pi) = " << std::ceil(dd_pi()) << "\n";
	std::cout << "ceil(-pi) = " << std::ceil(-dd_pi()) << "\n";
	std::cout << "ceil(+0.0) = " << std::ceil(dd_real("0.0")) << "\n";
	std::cout << "ceil(-0.0) = " << std::ceil(dd_real("-0.0")) << "\n";

	std::cout << "floor(e) = " << std::floor(dd_e()) << "\n";
	std::cout << "floor(-e) = " << std::floor(-dd_e()) << "\n";
	std::cout << "floor(+0.0) = " << std::floor(dd_real("0.0")) << "\n";
	std::cout << "floor(-0.0) = " << std::floor(dd_real("-0.0")) << "\n";

	std::cout << "fmod(12,5) = " << std::fmod(dd_real("12.0"), dd_real("5.0")) << "\n";
	std::cout << "fmod(12,-5) = " << std::fmod(dd_real("12.0"), dd_real("-5.0")) << "\n";
	std::cout << "fmod(-12,-5) = " << std::fmod(dd_real("-12.0"), dd_real("-5.0")) << "\n";
	std::cout << "fmod(-12,5) = " << std::fmod(dd_real("-12.0"), dd_real("5.0")) << "\n";
	std::cout << "fmod(15,2.3) = " << std::fmod(dd_real("15.0"), dd_real("2.3")) << "\n";
	std::cout << "fmod(12,inf) = " << std::fmod(dd_real("12.0"), std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "fmod(12,0) = " << std::fmod(dd_real("12.0"), dd_real("0.0")) << "\n";
	std::cout << "fmod(inf,5) = " << std::fmod(std::numeric_limits<dd_real>::infinity(), dd_real("5.0")) << "\n";

	std::cout << "round(pi) = " << std::round(dd_pi()) << "\n";
	std::cout << "round(-pi) = " << std::round(-dd_pi()) << "\n";
	std::cout << "round(e) = " << std::round(dd_e()) << "\n";
	std::cout << "round(-e) = " << std::round(-dd_e()) << "\n";
	std::cout << "round(2.5) = " << std::round(dd_real("2.5")) << "\n";
	std::cout << "round(-2.5) = " << std::round(-dd_real("-2.5")) << "\n";
	std::cout << "round(1.5) = " << std::round(dd_real("1.5")) << "\n";
	std::cout << "round(-1.5) = " << std::round(-dd_real("-1.5")) << "\n";

	std::cout << "trunc(pi) = " << std::trunc(dd_pi()) << "\n";
	std::cout << "trunc(-pi) = " << std::trunc(-dd_pi()) << "\n";
	std::cout << "trunc(e) = " << std::trunc(dd_e()) << "\n";
	std::cout << "trunc(-e) = " << std::trunc(-dd_e()) << "\n";

	dd_real a = "9.9999999999999992";
	dd_real b = "9.9999999999999991";

	if ( (a.toInt() == 9 ) && ( a.toInt() == b.toInt() ) )
		std::cout << "Conversion to int PASSED\n";
	else
		std::cout << "Conversion to int FAILED\n";

	long long a_i64 = (std::numeric_limits<long long>::max)();
	long long b_i64 = (std::numeric_limits<long long>::min)();
	dd_real a1 = a_i64;
	if (a1.toLongLong() == a_i64)
		std::cout << "LLMAX_INT conversion PASSED\n";
	else
		std::cout << "LLMAX_INT conversion FAILED\n";
	dd_real b1 = b_i64;
	if (b1.toLongLong() == b_i64)
		std::cout << "LLMIN_INT conversion PASSED\n";
	else
		std::cout << "LLMIN_INT conversion FAILED\n";

	std::cout << "\n";
}

void testMinMaxDiff()
{
	std::cout << "Tests of minimum, maximum, and difference functions\n";
	std::cout << "===================================================\n";

#if 0
	//
	//	minimum, maximum, difference functions
	//
	QD_API dd_real fdim( dd_real const& a, dd_real const& b );
	QD_API dd_real fmax( dd_real const& a, dd_real const& b );
	QD_API dd_real fmin( dd_real const& a, dd_real const& b );
#endif

	std::cout << "\n";
}

void testOther()
{
	std::uniform_real_distribution< dd_real > distribution1(-1048576.0, 1048576.0);
	int mre;

	std::cout << "Tests of other functions\n";
	std::cout << "========================\n";

	std::cout << "fabs(pi) = " << std::fabs(dd_pi()) << "\n";
	std::cout << "fabs(-pi) = " << std::fabs(-dd_pi()) << "\n";
	std::cout << "fabs(e) = " << std::fabs(dd_e()) << "\n";
	std::cout << "fabs(-e) = " << std::fabs(-dd_e()) << "\n";

	std::cout << "abs(pi) = " << std::abs(dd_pi()) << "\n";
	std::cout << "abs(-pi) = " << std::abs(-dd_pi()) << "\n";
	std::cout << "abs(e) = " << std::abs(dd_e()) << "\n";
	std::cout << "abs(-e) = " << std::abs(-dd_e()) << "\n";

	std::cout << "valid bits of fma() = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real a = distribution1(generator);
		dd_real b = distribution1(generator);
		dd_real c = -a * b;
		dd_real x = std::Fma(a, b, c);

		double ax[18];
		double aa[4];
		double ab[4];

		qd::split(a._hi(), aa[0], aa[1]);
		qd::split(a._lo(), aa[2], aa[3]);
		qd::split(b._hi(), ab[0], ab[1]);
		qd::split(b._lo(), ab[2], ab[3]);
		ax[ 0] = aa[0] * ab[0];
		ax[ 1] = aa[0] * ab[1];
		ax[ 2] = aa[1] * ab[0];
		ax[ 3] = aa[0] * ab[2];
		ax[ 4] = aa[1] * ab[1];
		ax[ 5] = aa[2] * ab[0];
		ax[ 6] = aa[0] * ab[3];
		ax[ 7] = aa[1] * ab[2];
		ax[ 8] = aa[2] * ab[1];
		ax[ 9] = aa[3] * ab[0];
		ax[10] = aa[1] * ab[3];
		ax[11] = aa[2] * ab[2];
		ax[12] = aa[3] * ab[1];
		ax[13] = aa[2] * ab[3];
		ax[14] = aa[3] * ab[2];
		ax[15] = aa[3] * ab[3];
		ax[16] = c._hi();
		ax[17] = c._lo();
		for (int i = 0; i < 4; i++)
		{
			for (int j = i + 1; j < 18; j++)
				ax[i] = qd::two_sum(ax[i], ax[j], ax[j]);
		}
		qd::three_sum(ax[0], ax[1], ax[2]);
		qd::three_sum(ax[0], ax[1], ax[3]);
		qd::three_sum(ax[1], ax[2], ax[3]);
		dd_real x2(ax[0], ax[1]);

		mre = std::min(mre, validBits(x, x2));
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "\n";
}

int _tmain(int argc, _TCHAR* argv[])
{
	unsigned int cw = _control87(0, 0);
	_control87(0x00010000, 0x00030000);

	unsigned int cw2 = _control87(0, 0);


	std::time_t t = std::time( NULL );
	generator.seed( (unsigned long)t );

	dd_real x = -1.32;
	dd_real y = 2.0;
	dd_real z = std::pow(x, y);

	testIO();
	testMultDiv();
	testClassification();
//	testFPManipulation();
	testRoundingRemainder();
//	testMinMaxDiff();
	testOther();

	testTrigonometric();
	testExpLog();
	testHyperbolic();
	testPower();

	_control87(cw, 0xFFFFFFFF);

	return 0;
}
