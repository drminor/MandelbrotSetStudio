#pragma once

#include <cxxtest/TestSuite.h>

class MyTestSuite : public CxxTest::TestSuite
{
public:
	void testMultiplication(void)
	{
		TS_ASSERT_EQUALS(2 * 2, 5);
	}

	void testAddition(void)
	{
		TS_ASSERT(1 + 1 > 1);
		TS_ASSERT_EQUALS(1 + 1, 2);
	}

};
