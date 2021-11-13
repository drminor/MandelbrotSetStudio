#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

//#include <qd/dd_real.h>

#include "CoordsDd.h"
#include "SizeInt.h"
#include "RectangleInt.h"


namespace FGen
{
	class FGEN_API CoordsMath
	{
	public:

		CoordsMath();

		CoordsDd ZoomIn(CoordsDd coords, SizeInt samplePoints, RectangleInt area);
		CoordsDd ZoomOut(CoordsDd coords, double amount);

		CoordsDd ShiftRight(CoordsDd coords, double amount);
		CoordsDd ShiftUp(CoordsDd coords, double amount);

		~CoordsMath();

	private:
		qp GetNewCoord(qp vSt, int avStart, qp extentY, int h);
	};
}

