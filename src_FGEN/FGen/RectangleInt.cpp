#include "stdafx.h"
#include "FGen.h"


namespace FGen
{
	RectangleInt::RectangleInt(int x, int y, int w, int h) : point(x, y), size(w, h)
	{
	}

	RectangleInt::RectangleInt(PointInt point, SizeInt size) : point(point), size(size)
	{
	}

	RectangleInt::RectangleInt()
	{
		point = PointInt(0, 0);
		size = SizeInt(0, 0);
	}

	RectangleInt::~RectangleInt()
	{
	}
}