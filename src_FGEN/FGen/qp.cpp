#include "stdafx.h"
#include "FGen.h"

#include <string>
#include <cctype>

#include "qpParser.h"

namespace FGen {

	qp::qp(std::string const& s)
	{
		qpParser parser = qpParser();
		parser.Read(s, _hip, _lop);

		//std::string result = parser.ToStr(_hip, _lop);
	}

	qp::~qp()
	{
	}

	std::string qp::to_string()
	{
		qpParser parser = qpParser();

		std::string result = parser.ToStr(_hip, _lop);
		return result;
	}

}