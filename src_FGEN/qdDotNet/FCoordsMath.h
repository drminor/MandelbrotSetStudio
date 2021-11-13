#pragma once

#include "../FGen/FGen.h"
#include "Dd.h"
#include "MCoordsDd.h"
#include "RectangleInt.h"
#include "PointDd.h"
#include "SizeInt.h"
#include "PointInt.h"

using namespace System;
namespace qdDotNet
{
	public ref class FCoordsMath
	{

	public:

		FCoordsMath();

		MCoordsDd ZoomIn(MCoordsDd coords, SizeInt samplePoints, RectangleInt area);
		MCoordsDd ZoomOut(MCoordsDd coords, double amount);

		MCoordsDd ShiftRight(MCoordsDd coords, double amount);
		MCoordsDd ShiftUp(MCoordsDd coords, double amount);

		virtual ~FCoordsMath()
		{
		}
		!FCoordsMath()
		{
		}

	private:
		FGen::CoordsMath* m_CoordsMath;


	};
}

