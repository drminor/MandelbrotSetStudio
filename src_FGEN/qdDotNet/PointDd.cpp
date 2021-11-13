#include "stdafx.h"
#include "PointDd.h"

namespace qdDotNet
{
	PointDd::PointDd(Dd x, Dd y) : x(x), y(y)
	{
	}

	PointDd::PointDd(FGen::PointDd point) : x(point.X()), y(point.Y())
	{
	}

}
