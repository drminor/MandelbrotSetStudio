#include "pch.h"
#include "ddBridge.h"

//#include <string>
#include "qp.h"


namespace MSetGenerator
{
	const char* ddBridge::test(double a, double b)
	{
		qp temp = qp(a, b);
		std::string strVal = temp.to_string();

		//std::string strVal = "hi";

		const char* result = strVal.c_str();

		return result;
	}

}
