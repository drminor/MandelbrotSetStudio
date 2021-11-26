
#include "pch.h"
#include "PointInt.h"

PointInt::PointInt(int x, int y) : x(x), y(y)
{
}

PointInt::PointInt()
{
	x = 0;
	y = 0;
}

void PointInt::SetXY(int nx, int ny)
{
	x = nx;
	y = ny;
}


PointInt::~PointInt()
{
}

