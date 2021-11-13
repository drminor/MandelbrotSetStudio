#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

#include "PointDd.h"
#include "SizeInt.h"
#include "PointInt.h"

namespace FGen
{
	struct FGEN_API RectangleInt
	{

	public:
		RectangleInt();
		RectangleInt(PointInt point, SizeInt size);
		RectangleInt(int x, int y, int w, int h);

		inline PointInt Point() const
		{
			return point;
		};

		inline SizeInt Size() const
		{
			return size;
		};

		inline int SX() const
		{
			return point.X();
		};

		inline int SY() const
		{
			return point.Y();
		};

		inline int EX() const
		{
			return point.X() + size.W();
		};

		inline int EY() const
		{
			return point.Y() + size.H();
		};

		inline int W() const
		{
			return size.W();
		};

		inline int H() const
		{
			return size.H();
		};

		~RectangleInt();

	private:
		SizeInt size;
		PointInt point;
	};

}


