#pragma once

#include "../FGen/FGen.h"

#include "PointInt.h"
#include "SizeInt.h"

namespace qdDotNet
{
	public value struct RectangleInt
	{
	public:
		RectangleInt(PointInt point, SizeInt size);
		RectangleInt(int x, int y, int w, int h);

		inline PointInt Point() 
		{
			return point;
		};

		inline SizeInt Size() 
		{
			return size;
		};

		inline int X() 
		{
			return point.X();
		};

		inline int Y() 
		{
			return point.Y();
		};

		inline int W() 
		{
			return size.W();
		};

		inline int H() 
		{
			return size.H();
		};

		FGen::RectangleInt ToRectangleInt()
		{
			FGen::RectangleInt result = FGen::RectangleInt(point.X(), point.Y(), size.W(), size.H());
			return result;
		}

	private:
		SizeInt size;
		PointInt point;

	};
}



