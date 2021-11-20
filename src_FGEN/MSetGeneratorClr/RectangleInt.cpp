#pragma once

#include "pch.h"

#include "PointInt.cpp"
#include "SizeInt.cpp"

namespace MSetGeneratorClr
{
	public value struct RectangleInt
	{
	public:
		
		RectangleInt(PointInt point, SizeInt size) : point(point), size(size)
		{
		}

		RectangleInt(int x, int y, int width, int height) : point(x, y), size(width, height)
		{
		}

		property PointInt Point { PointInt get() { return point; } }
		property SizeInt Size { SizeInt get() { return size; } }

		property int X { int get() { return point.X; } }
		property int Y { int get() { return point.Y; } }

		property int Width { int get() { return size.Width; } }

		property int Height { int get() { return size.Height; } }

		//FGen::RectangleInt ToRectangleInt()
		//{
		//	FGen::RectangleInt result = FGen::RectangleInt(point.X(), point.Y(), size.W(), size.H());
		//	return result;
		//}

	private:
		SizeInt size;
		PointInt point;

	};
}
