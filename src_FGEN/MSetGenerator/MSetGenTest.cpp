#include "pch.h"
#include "MSetGenTest.h"

//#include <string.h>
#include "qp.h"

namespace MSetGenerator
{
	double MSetGenTest::Test22()
	{
		double hi = 1.92;
		double lo = 82.0553889;

		qp test = qp(hi, lo);

		const std::string s = test.to_string();

		return 0.0;
	}
}



