//
//	testPower.cpp
//
//		Copyright (c) 2015 Daniel M. Pfeffer
//
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

void testExp()
{
	std::uniform_real_distribution< dd_real > distribution1(-dd_ln2(), dd_ln2());
	std::uniform_real_distribution< dd_real > distribution2(-335.0, 354.0);
	int mre;

	std::cout << "valid bits of exp(2x) == exp(x)^2 in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::exp( x ) * std::exp( x );
		dd_real xtag = std::exp(2.0 * x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of exp(2x) == exp(x)^2  in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real x2 = std::exp(x) * std::exp(x);
		dd_real xtag = std::exp(2.0 * x);

		mre = std::min(mre, validBits(xtag, x2));
		if (mre < 100)
			break;
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of exp(+x)*exp(-x) == 1.0 in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real xtag = std::exp(x) * std::exp(-x);

		mre = std::min(mre, validBits(xtag, 1.0));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "exp(0.0) = " << std::exp(dd_real("0.0")) << "\n";
	std::cout << "exp(-0.0) = " << std::exp(dd_real("-0.0")) << "\n";
	std::cout << "exp(" << std::numeric_limits< dd_real >::max() << ") = " << std::exp(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "exp(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::exp(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "exp(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::exp(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "exp(" << std::numeric_limits< dd_real >::min() << ") = " << std::exp(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "exp(" << -std::numeric_limits< dd_real >::min() << ") = " << std::exp(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "exp(1.0) = " << std::exp(dd_real("1.0")) << "\n";
	std::cout << "exp(-1.0) = " << std::exp(dd_real("-1.0")) << "\n";

	std::cout << "\n";
}

void testExp2()
{
	std::uniform_real_distribution< dd_real > distribution1(-1.0, 1.0);
	std::uniform_real_distribution< dd_real > distribution2(-484.0, 512.0);
	int mre;

	std::cout << "valid bits of exp2(2x) == exp2(x)^2 in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::exp2(x) * std::exp2(x);
		dd_real xtag = std::exp2(2.0 * x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of exp2(2x) == exp2(x)^2  in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real x2 = std::exp2(x) * std::exp2(x);
		dd_real xtag = std::exp2(2.0 * x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of exp2(+x)*exp2(-x) == 1.0 in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real xtag = std::exp2(x) * std::exp2(-x);

		mre = std::min(mre, validBits(xtag, 1.0));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "exp2(0.0) = " << std::exp2(dd_real("0.0")) << "\n";
	std::cout << "exp2(-0.0) = " << std::exp2(dd_real("-0.0")) << "\n";
	std::cout << "exp2(" << std::numeric_limits< dd_real >::max() << ") = " << std::exp2(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "exp2(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::exp2(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "exp2(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::exp2(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "exp2(" << std::numeric_limits< dd_real >::min() << ") = " << std::exp2(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "exp2(" << -std::numeric_limits< dd_real >::min() << ") = " << std::exp2(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "exp2(1.0) = " << std::exp2(dd_real("1.0")) << "\n";
	std::cout << "exp2(-1.0) = " << std::exp2(dd_real("-1.0")) << "\n";

	std::cout << "\n";
}

void testExpm1()
{
	std::uniform_real_distribution< dd_real > distribution1(-dd_ln2(), dd_ln2());
	int mre;

	std::cout << "valid bits of exp(x) - expm1(x) == 1 in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::expm1(x);
		dd_real xtag = std::exp(x);

		mre = std::min(mre, validBits(xtag - x2, 1.0));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "expm1(0.0) = " << std::expm1(dd_real("0.0")) << "\n";
	std::cout << "expm1(-0.0) = " << std::expm1(dd_real("-0.0")) << "\n";
	std::cout << "expm1(" << std::numeric_limits< dd_real >::max() << ") = " << std::expm1(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "expm1(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::expm1(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "expm1(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::expm1(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "expm1(" << std::numeric_limits< dd_real >::min() << ") = " << std::expm1(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "expm1(" << -std::numeric_limits< dd_real >::min() << ") = " << std::expm1(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "expm1(1.0) = " << std::expm1(dd_real("1.0")) << "\n";
	std::cout << "expm1(-1.0) = " << std::expm1(dd_real("-1.0")) << "\n";

	std::cout << "\n";
}

void testLog()
{
	std::uniform_real_distribution< dd_real > distribution1(1.0, 1048576.0);
	int mre;

	std::cout << "valid bits of log(x) in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::log( x * x );
		dd_real xtag = 2.0 * std::log( x );

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "log(0.0) = " << std::log(dd_real("0.0")) << "\n";
	std::cout << "log(-0.0) = " << std::log(dd_real("-0.0")) << "\n";
	std::cout << "log(" << std::numeric_limits< dd_real >::max() << ") = " << std::log(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "log(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::log(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::log(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log(" << std::numeric_limits< dd_real >::min() << ") = " << std::log(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log(" << -std::numeric_limits< dd_real >::min() << ") = " << std::log(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log(1.0) = " << std::log(dd_real("1.0")) << "\n";
	std::cout << "log(e) = " << std::log(dd_e()) << "\n";
	mre = validBits(std::log(dd_e()), 1.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "    valid bits = EXACT\n";
	else
		std::cout << "    valid bits = " << mre << "\n";

	std::cout << "\n";
}

void testLog2()
{
	std::uniform_real_distribution< dd_real > distribution1(dd_ln2()*sqrt(0.5), dd_ln2());
	int mre;

	std::cout << "valid bits of log2(x) in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::log2(x * x);
		dd_real xtag = 2.0 * std::log2(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "log2(0.0) = " << std::log2(dd_real("0.0")) << "\n";
	std::cout << "log2(-0.0) = " << std::log2(dd_real("-0.0")) << "\n";
	std::cout << "log2(" << std::numeric_limits< dd_real >::max() << ") = " << std::log2(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "log2(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::log2(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log2(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::log2(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log2(" << std::numeric_limits< dd_real >::min() << ") = " << std::log2(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log2(" << -std::numeric_limits< dd_real >::min() << ") = " << std::log2(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log2(1.0) = " << std::log2(dd_real("1.0")) << "\n";
	std::cout << "log2(2.0) = " << std::log2(dd_real("2.0")) << "\n";
	mre = validBits(std::log2(dd_real("2.0")), 1.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "    valid bits = EXACT\n";
	else
		std::cout << "    valid bits = " << mre << "\n";

	std::cout << "\n";
}

void testLog10()
{
	std::uniform_real_distribution< dd_real > distribution1(dd_ln10()*sqrt(0.5), dd_ln10());
	int mre;

	std::cout << "valid bits of log10(x) in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::log10(x * x);
		dd_real xtag = 2.0 * std::log10(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "log10(0.0) = " << std::log10(dd_real("0.0")) << "\n";
	std::cout << "log10(-0.0) = " << std::log10(dd_real("-0.0")) << "\n";
	std::cout << "log10(" << std::numeric_limits< dd_real >::max() << ") = " << std::log10(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "log10(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::log10(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log10(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::log10(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log10(" << std::numeric_limits< dd_real >::min() << ") = " << std::log10(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log10(" << -std::numeric_limits< dd_real >::min() << ") = " << std::log10(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log10(1.0) = " << std::log10(dd_real("1.0")) << "\n";
	std::cout << "log10(10.0) = " << std::log10(dd_real("10.0")) << "\n";
	mre = validBits(std::log10(dd_real("10.0")), 1.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "    valid bits = EXACT\n";
	else
		std::cout << "    valid bits = " << mre << "\n";
	std::cout << "\n";
}

void testLog1p()
{
	std::uniform_real_distribution< dd_real > distribution1(-0.5, 1.0);
	std::uniform_real_distribution< dd_real > distribution2(1.0, 2.0);
	std::uniform_real_distribution< dd_real > distribution3(-0.999999999999, -0.5);
	std::uniform_real_distribution< dd_real > distribution4(-dd_ln2(), dd_ln2());
	int mre;

	std::cout << "valid bits of log1p(expm1(x)) == x in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution4(generator);
		dd_real x2 = std::expm1(x);
		dd_real xtag = std::log1p(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of expm1(log1p(x)) == x in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::log1p(x);
		dd_real xtag = std::expm1(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of expm1(log1p(x)) == x in high range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real x2 = std::log1p(x);
		dd_real xtag = std::expm1(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of expm1(log1p(x)) == x in low range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real x2 = std::log1p(x);
		dd_real xtag = std::expm1(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "log1p(0.0) = " << std::log1p(dd_real("0.0")) << "\n";
	std::cout << "log1p(-0.0) = " << std::log1p(dd_real("-0.0")) << "\n";
	std::cout << "log1p(" << std::numeric_limits< dd_real >::max() << ") = " << std::log1p(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "log1p(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::log1p(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log1p(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::log1p(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "log1p(" << std::numeric_limits< dd_real >::min() << ") = " << std::log1p(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log1p(" << -std::numeric_limits< dd_real >::min() << ") = " << std::log1p(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "log1p(1.0) = " << std::log1p(dd_real("1.0")) << "\n";
	std::cout << "log1p(-1.0) = " << std::log1p(dd_real("-1.0")) << "\n";
	std::cout << "\n";
}

void testExpLog()
{
	std::cout << "Tests of exponential and logarithmic functions\n";
	std::cout << "==============================================\n";

	testExp();
	testExp2();
	testExpm1();
	testLog();
	testLog2();
	testLog10();
	testLog1p();

	std::cout << "\n";
}
