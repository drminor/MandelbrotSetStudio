#include "stdafx.h"
#include "FGen.h"


namespace FGen
{
	PointDd::PointDd(qp x, qp y) : x(x), y(y)
	{
	}

	PointDd::PointDd()
	{
		x = 0;
		y = 0;
	}

	PointDd::~PointDd()
	{
	}
}
