#include "pch.h"

#include "qp.h"

static LONGLONG MAX_LONG_FOR_DOUBLE;

void qp::initializeStaticMembers()
{
	MAX_LONG_FOR_DOUBLE = static_cast<LONGLONG>(pow(2, 53));
}

double qp::GetDouble(LONGLONG l) {
	if (l <= MAX_LONG_FOR_DOUBLE) {
		return static_cast<double>(l);
	}
	else {
		throw std::invalid_argument("Cannot create a qp from a LONGLONG having a value > 2^53.");
	}
}
