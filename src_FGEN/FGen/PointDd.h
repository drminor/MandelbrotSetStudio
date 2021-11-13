#pragma once

//#include <qd/dd_real.h>

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

#include "qp.h"

namespace FGen
{
	struct FGEN_API PointDd
	{

	public:
		PointDd();
		PointDd(qp x, qp y);

		inline qp X() const
		{
			return x;
		};

		inline qp Y() const
		{
			return y;
		};

		~PointDd();

	private:
		qp x;
		qp y;
	};
}

