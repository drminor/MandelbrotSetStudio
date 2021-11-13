#include "stdafx.h"
#include "MCoordsDd.h"

namespace qdDotNet
{
	MCoordsDd::MCoordsDd(PointDd start, PointDd end) : start(start), end(end)
	{
	}

	MCoordsDd::MCoordsDd(FGen::CoordsDd coords) : start(coords.Start()), end(coords.End())
	{
	}

}