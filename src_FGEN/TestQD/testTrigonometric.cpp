//
//	testTrigonometric.cpp
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

static dd_real const _inv_sqrt2 = dd_real("0.707106781186547524400844362104849039");

void testSinCos()
{
	std::uniform_real_distribution< dd_real > distribution1(50.0*dd_pi(), 50.0*dd_pi());
	std::uniform_real_distribution< dd_real > distribution2(-0.125*dd_pi(), 0.125*dd_pi());

	int mre;
	
	std::cout << "valid bits of sin(x) == -sin(-x) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x1 = std::sin(x);
		dd_real x2 = -std::sin(-x);

		mre = std::min(mre, validBits(x1, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of cos(x) == cos(-x) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x1 = std::cos(x);
		dd_real x2 = std::cos(-x);

		mre = std::min(mre, validBits(x1, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of sin(2x) == 2sin(x)cos(x) in primary range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution2(generator);
		dd_real x2 = std::sin(2.0 * x);
		dd_real xtag = 2.0 * std::sin(x) * std::cos(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of sin(2x) == 2sin(x)cos(x) in extended range = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::sin(2.0 * x);
		dd_real xtag = 2.0 * std::sin(x) * std::cos(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of cos(2x) == cos(x)^2 - sin(x)^2  = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::cos(2.0 * x);
		dd_real xtag = std::cos(x) * std::cos(x) - std::sin(x) * std::sin(x);

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "sin(0.0) = " << std::sin(dd_real("0.0")) << "\n";
	std::cout << "sin(-0.0) = " << std::sin(dd_real("-0.0")) << "\n";
	std::cout << "sin(" << std::numeric_limits< dd_real >::max() << ") = " << std::sin(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "sin(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::sin(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "sin(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::sin(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "sin(" << std::numeric_limits< dd_real >::min() << ") = " << std::sin(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "sin(" << -std::numeric_limits< dd_real >::min() << ") = " << std::sin(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "sin(pi) = " << std::sin(dd_pi()) << "\n";
	std::cout << "valid bits of sin(pi) == 0.0 = ";
	mre = validBits(std::sin(dd_pi()), 0.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of sin(pi/2) == 1.0 = ";
	mre = validBits(std::sin(0.5*dd_pi()), 1.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of sin(pi/4) == sqrt(0.5) = ";
	mre = validBits(std::sin(0.25*dd_pi()), _inv_sqrt2);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of sin(pi/6) == 0.5 = ";
	mre = validBits(std::sin(dd_pi() / 6.0), 0.5);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "\n";

	std::cout << "cos(0.0) = " << std::cos(dd_real("0.0")) << "\n";
	std::cout << "cos(-0.0) = " << std::cos(dd_real("-0.0")) << "\n";
	std::cout << "cos(" << std::numeric_limits< dd_real >::max() << ") = " << std::cos(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "cos(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::cos(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "cos(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::cos(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "cos(" << std::numeric_limits< dd_real >::min() << ") = " << std::cos(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "cos(" << -std::numeric_limits< dd_real >::min() << ") = " << std::cos(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "valid bits of cos(pi) == -1.0 = ";
	mre = validBits(std::cos(dd_pi()), -1.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of cos(pi/2) == 0.0 = ";
	mre = validBits(std::cos(0.5*dd_pi()), 0.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of cos(pi/4) == sqrt(0.5) = ";
	mre = validBits(std::cos(0.25*dd_pi()), _inv_sqrt2);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of cos(pi/3) == 0.5 = ";
	mre = validBits(std::cos(dd_pi() / 3.0), 0.5);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "\n";
}

void testTan()
{
	std::uniform_real_distribution< dd_real > distribution1(50.0*dd_pi(), 50.0*dd_pi());
	int mre;

	std::cout << "valid bits of tan(x) == -tan(-x) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x1 = std::tan(x);
		dd_real x2 = -std::tan(-x);

		mre = std::min(mre, validBits(x1, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of tan(2x) == 2tan(x)/(1-tan(x)^2) = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::tan(2.0 * x);
		dd_real xtag = 2.0 * std::tan(x) / (1.0 - std::tan(x) * std::tan(x));

		mre = std::min(mre, validBits(xtag, x2));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "tan(0.0) = " << std::tan(dd_real("0.0")) << "\n";
	std::cout << "tan(-0.0) = " << std::tan(dd_real("-0.0")) << "\n";
	std::cout << "tan(" << std::numeric_limits< dd_real >::max() << ") = " << std::tan(std::numeric_limits< dd_real >::max()) << "\n";
	std::cout << "tan(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::tan(std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "tan(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::tan(-std::numeric_limits< dd_real >::infinity()) << "\n";
	std::cout << "tan(" << std::numeric_limits< dd_real >::min() << ") = " << std::tan(std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "tan(" << -std::numeric_limits< dd_real >::min() << ") = " << std::tan(-std::numeric_limits< dd_real >::min()) << "\n";
	std::cout << "valid bits of tan(pi/4) == 1.0 = ";
	mre = validBits(std::tan(0.25*dd_pi()), 1.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "valid bits of tan(pi) == 0.0 = ";
	mre = validBits(std::tan(dd_pi()), 0.0);
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "\n";
}

void testACos()
{
	std::uniform_real_distribution< dd_real > distribution1(-0.999999999999, 0.999999999999);
	int mre;

	std::cout << "valid bits of cos(acos((x)) == x = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i <= 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::acos(x);
		dd_real xtag = std::cos(x2);

		int valid = validBits(xtag, x);
		mre = std::min(mre, valid);
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
}

void testASin()
{
	std::uniform_real_distribution< dd_real > distribution1(-0.999999999999, 0.999999999999);
	int mre;

	std::cout << "valid bits of sin(asin((x)) == x = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::asin(x);
		dd_real xtag = std::sin(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
}

void testATan()
{
	std::uniform_real_distribution< dd_real > distribution1(-1024.0, 1024.0);
	int mre;

	std::cout << "valid bits of tan(atan((x)) == x = ";
	mre = std::numeric_limits<int>::max();
	for (int i = 0; i < 100000; i++)
	{
		dd_real x = distribution1(generator);
		dd_real x2 = std::atan(x);
		dd_real xtag = std::tan(x2);

		mre = std::min(mre, validBits(xtag, x));
	}
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "atan(0.0) = " << std::atan(dd_real("0.0")) << "\n";
	std::cout << "atan(-0.0) = " << std::atan(dd_real("-0.0")) << "\n";
	std::cout << "atan(inf) = " << std::atan(std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "atan(-inf) = " << std::atan(-std::numeric_limits<dd_real>::infinity()) << "\n";
	std::cout << "valid bits of atan(1.0) == pi/4 = ";
	mre = validBits(std::atan(dd_real("1.0")), 0.25*dd_pi());
	if (mre == std::numeric_limits<int>::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";
	std::cout << "\n";
}

void testATan2()
{
	std::cout << "atan2(0.0, 0.0) = " << std::atan2(dd_real("0.0"), dd_real("0.0")) << "\n";
	std::cout << "atan2(0.0, 1.0) = " << std::atan2(dd_real("0.0"), dd_real("1.0")) << "\n";
	std::cout << "atan2(0.0, -1.0) = " << std::atan2(dd_real("0.0"), dd_real("-1.0")) << "\n";
	std::cout << "atan2(1.0, 0.0) = " << std::atan2(dd_real("1.0"), dd_real("0.0")) << "\n";
	std::cout << "atan2(1.0, 1.0) = " << std::atan2(dd_real("1.0"), dd_real("1.0")) << "\n";
	std::cout << "atan2(1.0, -1.0) = " << std::atan2(dd_real("1.0"), dd_real("-1.0")) << "\n";
	std::cout << "atan2(-1.0, 0.0) = " << std::atan2(dd_real("-1.0"), dd_real("0.0")) << "\n";
	std::cout << "atan2(-1.0, 1.0) = " << std::atan2(dd_real("-1.0"), dd_real("1.0")) << "\n";
	std::cout << "atan2(-1.0, -1.0) = " << std::atan2(dd_real("-1.0"), dd_real("-1.0")) << "\n";
	std::cout << "\n";
}

void testTrigonometric()
{
	std::cout << "Tests of trigonometric functions\n";
	std::cout << "================================\n";

	testSinCos();
	testTan();
	testACos();
	testASin();
	testATan();
	testATan2();

	std::cout << "\n";
}
