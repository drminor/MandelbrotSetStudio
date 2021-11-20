#pragma once

#include "pch.h"

namespace MSetGeneratorClr
{
	public value struct SizeInt
	{

	public:
		SizeInt(int w, int h)
		{
			width = w;
			height = h;
		}

		property int Width { int get() { return width; } }
		property int Height { int get() { return height; } }


		//FGen::SizeInt ToSizeInt()
		//{
		//	FGen::SizeInt result = FGen::SizeInt(w, h);
		//	return result;
		//}

	private:
		int width;
		int height;
	};
}