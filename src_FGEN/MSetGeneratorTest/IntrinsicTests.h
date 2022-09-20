#pragma once

#include <cxxtest/TestSuite.h>

class IntrinsicTest : public CxxTest::TestSuite
{
public:
	void testMultiplication(void)
	{
		TS_ASSERT_EQUALS(2 * 2, 4);
	}

	void testAddition(void)
	{
		TS_ASSERT(1 + 1 > 1);
		TS_ASSERT_EQUALS(1 + 1, 2);
	}

};
