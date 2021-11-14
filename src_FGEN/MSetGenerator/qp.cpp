#include "pch.h"

#include <string>
#include <cctype>

#include "qp.h"
#include "qpParser.h"

namespace MSetGenerator {

	qp::qp(const std::string s)
	{
		qpParser parser = qpParser();
		parser.Read(s, _hip, _lop);

		//std::string result = parser.ToStr(_hip, _lop);
	}

	qp::~qp()
	{
	}

	const std::string qp::to_string()
	{
		qpParser parser = qpParser();

		std::string result = parser.ToStr(_hip, _lop);
		return result;
	}

}