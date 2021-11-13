#include "stdafx.h"
#include "RectangleInt.h"


namespace qdDotNet
{

	RectangleInt::RectangleInt(int x, int y, int w, int h) : point(x, y), size(w, h)
	{
	}

	RectangleInt::RectangleInt(PointInt point, SizeInt size) : point(point), size(size)
	{
	}
}
