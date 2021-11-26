#pragma once

#include "qp.h"

struct PointDd
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


