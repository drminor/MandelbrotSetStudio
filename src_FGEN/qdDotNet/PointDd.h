#pragma once

#include "../FGen/FGen.h"
#include "Dd.h"
namespace qdDotNet
{
	public value struct PointDd
	{

	public:	
		PointDd(Dd x, Dd y);
		PointDd(FGen::PointDd point);

		inline Dd X()
		{
			return x;
		};

		inline Dd Y()
		{
			return y;
		};

		FGen::PointDd ToPointDd()
		{
			FGen::PointDd result = FGen::PointDd(x.ToQp(), y.ToQp());
			return result;
		}

	private:
		Dd x;
		Dd y;

	};
}


