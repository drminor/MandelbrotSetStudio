#pragma once
#include "../FGen/FGen.h"

namespace qdDotNet
{
	public value struct SizeInt
	{

	public:
		SizeInt(int w, int h);

		inline int W() 
		{
			return w;
		};

		inline int H() 
		{
			return h;
		};

		FGen::SizeInt ToSizeInt()
		{
			FGen::SizeInt result = FGen::SizeInt(w, h);
			return result;
		}

	private:
		int w;
		int h;
	};
}

