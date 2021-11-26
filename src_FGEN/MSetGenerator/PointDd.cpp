#include "pch.h"
#include "PointDd.h"

PointDd::PointDd(qp x, qp y) : x(x), y(y)
{
}

PointDd::PointDd()
{
	x = qp(0.0);
	y = qp(0.0);
}

PointDd::~PointDd()
{
}
