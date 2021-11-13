
#include "stdafx.h"
#include "../FGen/FGen.h"
#include "FCoordsMath.h"


namespace qdDotNet
{
	FCoordsMath::FCoordsMath()
	{
		m_CoordsMath = new FGen::CoordsMath();
	}

	MCoordsDd FCoordsMath::ZoomIn(MCoordsDd mCoords, SizeInt mSamplePoints, RectangleInt mArea)
	{
		FGen::CoordsDd coords = mCoords.ToCoordsDd();
		FGen::SizeInt samplePoints = mSamplePoints.ToSizeInt();
		FGen::RectangleInt area = mArea.ToRectangleInt();

		FGen::CoordsDd result = m_CoordsMath->ZoomIn(coords, samplePoints, area);

		MCoordsDd mResult = MCoordsDd(result);
		return mResult;
	}

	MCoordsDd FCoordsMath::ZoomOut(MCoordsDd mCoords, double amount)
	{
		FGen::CoordsDd coords = mCoords.ToCoordsDd();
		FGen::CoordsDd result = m_CoordsMath->ZoomOut(coords, amount);
		MCoordsDd mResult = MCoordsDd(result);
		return mResult;
	}

	MCoordsDd FCoordsMath::ShiftRight(MCoordsDd mCoords, double amount)
	{
		FGen::CoordsDd coords = mCoords.ToCoordsDd();
		FGen::CoordsDd result = m_CoordsMath->ShiftRight(coords, amount);
		MCoordsDd mResult = MCoordsDd(result);
		return mResult;
	}

	MCoordsDd FCoordsMath::ShiftUp(MCoordsDd mCoords, double amount)
	{
		FGen::CoordsDd coords = mCoords.ToCoordsDd();
		FGen::CoordsDd result = m_CoordsMath->ShiftUp(coords, amount);
		MCoordsDd mResult = MCoordsDd(result);
		return mResult;
	}
}

