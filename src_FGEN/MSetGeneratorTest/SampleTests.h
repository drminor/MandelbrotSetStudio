#pragma once

#include <cxxtest/TestSuite.h>

class SampleTest : public CxxTest::TestSuite
{

public:

	void testMultiplication2(void)
	{
		TS_ASSERT_EQUALS(2 * 2, 4);
	}

	void testAddition2(void)
	{
		TS_ASSERT(1 + 1 > 1);
		TS_ASSERT_EQUALS(1 + 1, 2);
	}

};
