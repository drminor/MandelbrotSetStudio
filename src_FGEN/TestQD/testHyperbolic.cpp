//
//	testHyperbolic.cpp
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

void testSinhCosh()
{
	std::uniform_real_distribution< dd_real > distribution1(-335.0, 354.0);
	std::uniform_real_distribution< dd_real > distribution2(-0.5, 0.5);
	int mre;

	std::cout << "valid bits of sinh(x) == -sinh(-x) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x1 = std::sinh(x);
		dd_real x2 = -std::sinh(-x);

		mre = std::min(mre, validBits(x1, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of cosh(x) == cosh(-x) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x1 = std::cosh(x);
		dd_real x2 = std::cosh(-x);

		mre = std::min(mre, validBits(x1, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of sinh(2x) == 2sinh(x)cosh(x) in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real x2 = std::sinh(2.0 * x);
		dd_real xtag = 2.0 * std::sinh(x) * std::cosh(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of sinh(2x) == 2sinh(x)cosh(x) in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::sinh(2.0 * x);
		dd_real xtag = 2.0 * std::sinh(x) * std::cosh(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of cosh(2x) == cosh(x)^2 + sinh(x)^2 = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::cosh(2.0 * x);
		dd_real xtag = std::cosh(x) * std::cosh(x) + std::sinh(x) * std::sinh(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "sinh(0.0) = " << std::sinh(dd_real("0.0")) << "\n";
	std::cout << "sinh(-0.0) = " << std::sinh(dd_real("-0.0")) << "\n";
	std::cout << "sinh(" << std::numeric_limits< dd_real >::max() << ") = " << std::sinh(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "sinh(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::sinh(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "sinh(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::sinh(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "sinh(" << std::numeric_limits< dd_real >::min() << ") = " << std::sinh(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "sinh(" << -std::numeric_limits< dd_real >::min() << ") = " << std::sinh(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "sinh(1.0) = " << std::sinh(dd_real("1.0")) << "\n";
	std::cout << "\n";

	std::cout << "cosh(0.0) = " << std::cosh(dd_real("0.0")) << "\n";
	std::cout << "cosh(-0.0) = " << std::cosh(dd_real("-0.0")) << "\n";
	std::cout << "cosh(" << std::numeric_limits< dd_real >::max() << ") = " << std::cosh(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "cosh(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::cosh(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "cosh(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::cosh(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "cosh(" << std::numeric_limits< dd_real >::min() << ") = " << std::cosh(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "cosh(" << -std::numeric_limits< dd_real >::min() << ") = " << std::cosh(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "cosh(1.0) = " << std::cosh(dd_real("1.0")) << "\n";
	std::cout << "\n";
}

void testTanh()
{
	std::uniform_real_distribution< dd_real > distribution1(-360.0, 360.0);
	int mre;

	std::cout << "valid bits of tanh(x) == -tanh(-x) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x1 = std::tanh(x);
		dd_real x2 = -std::tanh(-x);

		mre = std::min(mre, validBits(x1, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of tanh(2x) == 2tanh(x)/(1+tanh(x)^2) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::tanh(2.0 * x);
		dd_real xtag = 2.0 * std::tanh(x) / (1.0 + std::tanh(x) * std::tanh(x));

		mre = std::min(mre, validBits(xtag, x2));
		if (mre < 10)
			break;
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "tanh(0.0) = " << std::tanh(dd_real("0.0")) << "\n";
	std::cout << "tanh(-0.0) = " << std::tanh(dd_real("-0.0")) << "\n";
	std::cout << "tanh(" << std::numeric_limits< dd_real >::max() << ") = " << std::tanh(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "tanh(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::tanh(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "tanh(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::tanh(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "tanh(" << std::numeric_limits< dd_real >::min() << ") = " << std::tanh(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "tanh(" << -std::numeric_limits< dd_real >::min() << ") = " << std::tanh(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "tanh(1.0) = " << std::tanh(dd_real("1.0")) << "\n";
	std::cout << "\n";
}

void testACosh()
{
	std::uniform_real_distribution< dd_real > distribution1(1.0, 709.0);
	int mre;

	std::cout << "valid bits of cosh(acosh((x)) == x = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::acosh(x);
		dd_real xtag = std::cosh(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
}

void testASinh()
{
	std::uniform_real_distribution< dd_real > distribution1(-dd_ln2(), dd_ln2());
	std::uniform_real_distribution< dd_real > distribution2(-670.0, 709.0);
	int mre;

	int count[120];
	dd_real data[120];
	for (int i = 0; i < 120; i++)
	{
		count[i] = 0;
		data[i] = 0.0;
	}
	std::cout << "valid bits of sinh(asinh((x)) == x in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::asinh(x);
		dd_real xtag = std::sinh(x2);

		auto valid = validBits(xtag, x);
		++count[valid];
		data[valid] += std::abs(x);
		mre = std::min(mre, valid);
	}
	for (int i = 0; i < 120; i++)
	{
		if ( count[i] != 0)
			data[i] /= count[i];
	}

	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of sinh(asinh((x)) == x in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::asinh(x);
		dd_real xtag = std::sinh(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
}

void testATanh()
{
	std::uniform_real_distribution< dd_real > distribution1(-0.999999999999, 0.999999999999);
	int mre;

	std::cout << "valid bits of tanh(atanh((x)) == x in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::atanh(x);
		dd_real xtag = std::tanh(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "atanh(0.0) = " << std::atanh(dd_real("0.0")) << "\n";
	std::cout << "atanh(-0.0) = " << std::atanh(dd_real("-0.0")) << "\n";
	std::cout << "atanh(1.0) = " << std::atanh(dd_real("1.0")) << "\n";
	std::cout << "atanh(-1.0) = " << std::atanh(dd_real("-1.0")) << "\n";
}

void testHyperbolic()
{
	std::cout << "Tests of hyperbolic functions\n";
	std::cout << "=============================\n";

	testSinhCosh();
	testTanh();
	testACosh();
	testASinh();
	testATanh();

	std::cout << "\n";
}
