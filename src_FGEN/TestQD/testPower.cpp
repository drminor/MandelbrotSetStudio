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

void testSqrt()
{
	std::uniform_real_distribution< dd_real > distribution1( 0.25 , 1.0 );
	std::uniform_real_distribution< dd_real > distribution2( 2.0, 128.0 );
	int mre;

	std::cout << "valid bits of sqrt() in primary range = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution1( generator );
		dd_real x2 = x * x;
		dd_real xtag = std::sqrt( x2 );

		mre = std::min( mre, validBits( xtag, x ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of sqrt() in extended range = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution2( generator );
		dd_real x2 = x * x;
		dd_real xtag = std::sqrt( x2 );

		mre = std::min( mre, validBits( xtag, x ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "sqrt(0.0) = " << std::sqrt( dd_real( "0.0" ) ) << "\n";
	std::cout << "sqrt(-0.0) = " << std::sqrt( dd_real( "-0.0" ) ) << "\n";
	std::cout << "sqrt(" << std::numeric_limits< dd_real >::max() << ") = " << std::sqrt( std::numeric_limits< dd_real >::max() ) << "\n";
	std::cout << "sqrt(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::sqrt( std::numeric_limits< dd_real >::infinity() ) << "\n";
	std::cout << "sqrt(" << std::numeric_limits< dd_real >::min() << ") = " << std::sqrt( std::numeric_limits< dd_real >::min() ) << "\n";
	std::cout << "sqrt(" << -std::numeric_limits< dd_real >::min() << ") = " << std::sqrt( -std::numeric_limits< dd_real >::min() ) << "\n";
	std::cout << "sqrt(-1.0) = " << std::sqrt( dd_real( "-1.0" ) ) << "\n";
	std::cout << "\n";
}

void testCbrt()
{
	std::uniform_real_distribution< dd_real > distribution1( 0.125 , 1.0 );
	std::uniform_real_distribution< dd_real > distribution2( 2.0, 128.0 );
	int mre;

	std::cout << "valid bits of cbrt() in primary range = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution1( generator );
		dd_real x3 = x * x * x;
		dd_real xtag = std::cbrt( x3 );

		mre = std::min( mre, validBits( xtag, x ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of cbrt() in extended range = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution2( generator );
		dd_real x3 = x * x * x;
		dd_real xtag = std::cbrt( x3 );

		mre = std::min( mre, validBits( xtag, x ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "cbrt(0.0) = " << std::cbrt( dd_real( "0.0" ) ) << "\n";
	std::cout << "cbrt(-0.0) = " << std::cbrt( dd_real( "-0.0" ) ) << "\n";
	std::cout << "cbrt(" << std::numeric_limits< dd_real >::max() << ") = " << std::cbrt( std::numeric_limits< dd_real >::max() ) << "\n";
	std::cout << "cbrt(" << std::numeric_limits< dd_real >::infinity() << ") = " << std::cbrt( std::numeric_limits< dd_real >::infinity() ) << "\n";
	std::cout << "cbrt(" << -std::numeric_limits< dd_real >::infinity() << ") = " << std::cbrt( -std::numeric_limits< dd_real >::infinity() ) << "\n";
	std::cout << "cbrt(" << std::numeric_limits< dd_real >::min() << ") = " << std::cbrt( std::numeric_limits< dd_real >::min() ) << "\n";
	std::cout << "cbrt(" << -std::numeric_limits< dd_real >::min() << ") = " << std::cbrt( -std::numeric_limits< dd_real >::min() ) << "\n";
	std::cout << "cbrt(-8.0) = " << std::cbrt( dd_real( "-8.0" ) ) << "\n";
	std::cout << "\n";
}

void testHypot()
{
	std::uniform_real_distribution< dd_real > distribution( 1.0, 1048576.0 );
	int mre;

	std::cout << "valid bits of hypot() = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution( generator );
		dd_real y = distribution( generator );
		dd_real result = std::sqrt( x * x + y * y );
		dd_real xtag = std::hypot( x, y );

		mre = std::min( mre, validBits( xtag, result ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "hypot(0.0, 0.0) = " << std::hypot( dd_real( "0.0" ), dd_real( "0.0" ) ) << "\n";
	std::cout << "hypot(" << std::numeric_limits< dd_real >::max() << ", 1.0) = " << std::hypot( std::numeric_limits< dd_real >::max(), dd_real( "1.0" ) ) << "\n";
	std::cout << "hypot(" << std::numeric_limits< dd_real >::infinity() << ",1.0) = " << std::hypot( std::numeric_limits< dd_real >::infinity(), dd_real( "1.0" ) ) << "\n";
	std::cout << "hypot(" << -std::numeric_limits< dd_real >::infinity() << ", 1.0) = " << std::hypot( -std::numeric_limits< dd_real >::infinity(), dd_real( "1.0" )  ) << "\n";
	std::cout << "hypot(" << std::numeric_limits< dd_real >::min() << ", 1.0) = " << std::hypot( std::numeric_limits< dd_real >::min(), dd_real( "1.0" ) ) << "\n";
	std::cout << "hypot(" << -std::numeric_limits< dd_real >::min() << ", 1.0) = " << std::hypot( -std::numeric_limits< dd_real >::min(), dd_real( "1.0" ) ) << "\n";

	std::cout << "hypot(1.0, " << std::numeric_limits< dd_real >::max() << ") = " << std::hypot( dd_real( "1.0" ), std::numeric_limits< dd_real >::max() ) << "\n";
	std::cout << "hypot(1.0, " << std::numeric_limits< dd_real >::infinity() << ") = " << std::hypot( dd_real( "1.0" ), std::numeric_limits< dd_real >::infinity() ) << "\n";
	std::cout << "hypot(1.0, " << -std::numeric_limits< dd_real >::infinity() << ") = " << std::hypot( dd_real( "1.0" ), -std::numeric_limits< dd_real >::infinity() ) << "\n";
	std::cout << "hypot(1.0, " << std::numeric_limits< dd_real >::min() << ") = " << std::hypot( dd_real( "1.0" ), std::numeric_limits< dd_real >::min() ) << "\n";
	std::cout << "hypot(1.0, " << -std::numeric_limits< dd_real >::min() << ") = " << std::hypot( dd_real( "1.0" ), -std::numeric_limits< dd_real >::min() ) << "\n";
	std::cout << "\n";
}

void testPow()
{
	dd_real half( "0.5" );
	dd_real third( "0.333333333333333333333333333333333333" );
	std::uniform_real_distribution< dd_real > distribution( 1.0, 1048576.0 );
	int mre;

	std::cout << "valid bits of pow(x, 0.5) = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution( generator );
		dd_real result = std::sqrt( x );
		dd_real xtag = std::pow( x, half );

		mre = std::min( mre, validBits( xtag, result ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of pow(x, 0.3333...) = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution( generator );
		dd_real result = std::cbrt( x );
		dd_real xtag = std::pow( x, third );

		mre = std::min( mre, validBits( xtag, result ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of pow(x, 2.0) = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution( generator );
		dd_real result = x * x;
		dd_real xtag = std::pow( x, 2.0 );

		mre = std::min( mre, validBits( xtag, result ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "valid bits of pow(x, 3.0) = ";
	mre = std::numeric_limits<int>::max();
	for ( int i = 0; i < 100000; i++ )
	{
		dd_real x = distribution( generator );
		dd_real result = x * x * x;
		dd_real xtag = std::pow( x, 3.0 );

		mre = std::min( mre, validBits( xtag, result ) );
	}
	if (mre == std::numeric_limits< int >::max())
		std::cout << "EXACT\n";
	else
		std::cout << mre << "\n";

	std::cout << "\n";
}

void testPower()
{
	std::cout << "Tests of power functions\n";
	std::cout << "========================\n";

	testSqrt();
	testCbrt();
	testHypot();
	testPow();
	std::cout << "\n";
}
