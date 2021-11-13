#pragma once

#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

#include "qp.h"
#include "PointDd.h"

namespace FGen
{
	struct FGEN_API CoordsDd
	{
	public:

		CoordsDd();
		CoordsDd(PointDd start, PointDd end);

		inline PointDd Start() const
		{
			return start;
		};

		inline PointDd End() const
		{
			return end;
		};

		inline qp SX() const
		{
			return start.X();
		};

		inline qp EX() const
		{
			return end.X();
		};

		inline qp SY() const
		{
			return start.Y();
		};

		inline qp EY() const
		{
			return end.Y();
		};

		~CoordsDd();

	private:
		PointDd start;
		PointDd end;

	};

}

