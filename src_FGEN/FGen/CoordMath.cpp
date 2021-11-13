#include "stdafx.h"
#include "FGen.h"
#include "qpMath.h"

namespace FGen
{
	CoordsMath::CoordsMath()
	{
	}

	CoordsDd CoordsMath::ZoomIn(CoordsDd coords, SizeInt samplePoints, RectangleInt area)
	{
		qpMath * qpCalc = new qpMath();

		//qp extentX = coords.EX() - coords.SX();
		qp extentX = qpCalc->sub(coords.EX(), coords.SX());

		qp hSt = coords.SX();
		int w = samplePoints.W();

		int ahStart = area.SX();
		qp nsx = CoordsMath::GetNewCoord(hSt, ahStart, extentX, w);

		int ahEnd = area.EX();
		qp nex = CoordsMath::GetNewCoord(hSt, ahEnd, extentX, w);

		//qp extentY = coords.EY() - coords.SY();
		qp extentY = qpCalc->sub(coords.EY(), coords.SY());

		qp vSt = coords.SY();
		int h = samplePoints.H();

		int avStart = area.SY();
		qp nsy = CoordsMath::GetNewCoord(vSt, avStart, extentY, h);

		int avEnd = area.EY();
		qp ney = CoordsMath::GetNewCoord(vSt, avEnd, extentY, h);

		PointDd start = PointDd(nsx, nsy);
		PointDd end = PointDd(nex, ney);
		CoordsDd result = CoordsDd(start, end);

		delete qpCalc;
		return result;
	}

	qp CoordsMath::GetNewCoord(qp mSt, int pt, qp mExtent, int aExtent)
	{
		qpMath * qpCalc = new qpMath();

		double aRat = pt / (double)aExtent;

		//qp mOff = mExtent * aRat;
		qp mOff = qpCalc->mulD(mExtent, aRat);

		//qp nMPt = mSt + mOff;
		qp nMPt = qpCalc->add(mSt, mOff);

		delete qpCalc;
		return nMPt;
	}

	CoordsDd CoordsMath::ZoomOut(CoordsDd coords, double amount)
	{
		qpMath * qpCalc = new qpMath();

		//qp deltaX = coords.Width() * amount / 2;
		qp width = qpCalc->sub(coords.EX(), coords.SX());
		qp deltaX = qpCalc->mulD(width, amount / 2);


		//qp deltaY = coords.Height() * amount / 2;
		qp height = qpCalc->sub(coords.EY(), coords.SY());
		qp deltaY = qpCalc->mulD(height, amount / 2);

		//PointDd start = PointDd(coords.SX() - deltaX, coords.SY() - deltaY);
		PointDd start = PointDd(qpCalc->sub(coords.SX(), deltaX), qpCalc->sub(coords.SY(), deltaY));

		//PointDd end = PointDd(coords.EX() + deltaX, coords.EY() + deltaY);
		PointDd end = PointDd(qpCalc->add(coords.EX(), deltaX), qpCalc->add(coords.EY(), deltaY));

		CoordsDd result = CoordsDd(start, end);

		delete qpCalc;
		return result;
	}

	CoordsDd CoordsMath::ShiftRight(CoordsDd coords, double amount)
	{
		qpMath * qpCalc = new qpMath();

		//qp delta = coords.Width() * amount;
		qp width = qpCalc->sub(coords.EX(), coords.SX());
		qp delta = qpCalc->mulD(width, amount);

		//PointDd start = PointDd(coords.SX() + delta, coords.SY());
		PointDd start = PointDd(qpCalc->add(coords.SX(), delta), coords.SY());

		//PointDd end = PointDd(coords.EX() + delta, coords.EY());
		PointDd end = PointDd(qpCalc->add(coords.EX(), delta), coords.EY());

		CoordsDd result = CoordsDd(start, end);

		delete qpCalc;
		return result;
	}

	CoordsDd CoordsMath::ShiftUp(CoordsDd coords, double amount)
	{
		qpMath * qpCalc = new qpMath();

		//qp delta = coords.Height() * amount;
		qp height = qpCalc->sub(coords.EY(), coords.SY());
		qp delta = qpCalc->mulD(height, amount);

		//PointDd start = PointDd(coords.SX(), coords.SY() + delta);
		PointDd start = PointDd(coords.SX(), qpCalc->add(coords.SY(), delta));

		//PointDd end = PointDd(coords.EX(), coords.EY() + delta);
		PointDd end = PointDd(coords.EX(), qpCalc->add(coords.EY(), delta));

		CoordsDd result = CoordsDd(start, end);

		delete qpCalc;
		return result;
	}

	CoordsMath::~CoordsMath()
	{
	}
}
