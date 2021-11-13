//
//	testPower.cpp
//
//		Copyright (c) 2015 Daniel M. Pfeffer
//
//

#if !defined( QDTEST_TEST_POWER_H__ )
#define QDTEST_TEST_POWER_H__

#include <limits>
#include <random>

#include <qd/dd_real.h>

extern std::default_random_engine generator;

namespace std
{

	template <>
	class uniform_real_distribution< dd_real >
	{
	public:
		uniform_real_distribution( dd_real low, dd_real high )
			: _mDist( low._hi(), high._hi() )
		{
		}

		template < typename engine >
		dd_real operator()( engine& eng )
		{
			return dd_real( _mDist( eng ), 0.5*std::numeric_limits< double >::epsilon() * _mDist( eng ) );
		}

	private:
		uniform_real_distribution< double > _mDist;
	};
	
}

void testTrigonometric();
void testExpLog();
void testHyperbolic();
void testPower();

#endif