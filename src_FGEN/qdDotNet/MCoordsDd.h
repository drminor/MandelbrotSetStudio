#pragma once
#include "../FGen/FGen.h"

#include "Dd.h"
#include "PointDd.h"

namespace qdDotNet
{
	public value struct MCoordsDd
	{

	public:
		MCoordsDd(PointDd start, PointDd end);
		MCoordsDd(FGen::CoordsDd coords);

		inline PointDd Start()
		{
			return start;
		};

		inline PointDd End()
		{
			return end;
		};

		FGen::CoordsDd ToCoordsDd() {

			FGen::CoordsDd result = FGen::CoordsDd(start.ToPointDd(), end.ToPointDd());
			return result;
		}


	private:
		PointDd start;
		PointDd end;

	};
}