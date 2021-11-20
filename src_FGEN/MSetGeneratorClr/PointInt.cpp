#pragma once
#include "pch.h"
namespace MSetGeneratorClr
{
	public value struct PointInt
	{

	public:
		PointInt(int x, int y) : x(x), y(y)
		{
		}

		property int X { int get() { return x; } }
		property int Y { int get() { return y; } }

		inline PointInt Translate(PointInt amount)
		{
			return PointInt(x + amount.x, y + amount.y);
		}

	private:
		int x;
		int y;
	};
}
