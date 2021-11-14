#include "pch.h"
#include "RectangleInt.h"

namespace MSetGeneratorClr
{

	RectangleInt::RectangleInt(int x, int y, int w, int h) : point(x, y), size(w, h)
	{
	}

	RectangleInt::RectangleInt(PointInt point, SizeInt size) : point(point), size(size)
	{
	}
}
