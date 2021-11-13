//
//	Utils.cpp
//
//		Copyright (c) 2015 Daniel M. Pfeffer
//
//
#include "stdafx.h"

#include <cmath>

#include <qd/dd_real.h>


int validBits( dd_real const& computed, dd_real const& expected )
{
	static const double LOG2E = 1.44269504088896340736;

	dd_real delta = computed - expected;
	if ( delta == 0.0 )
	{
		return std::numeric_limits< int >::max();
	}
	else
	{
		if ( expected == 0.0 )
		{
			return static_cast< int >( -std::log( std::fabs( computed.toDouble() ) ) * LOG2E );
		}
		else
		{
			delta /= expected;
			double d = delta.toDouble();

			double ld = std::log(std::fabs(d)) * LOG2E;
			return static_cast<int>(-ld);
		}
	}
}
