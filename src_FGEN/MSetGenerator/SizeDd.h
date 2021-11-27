#pragma once

#include "qp.h"

struct SizeDd
{

public:
	SizeDd();
	SizeDd(qp width, qp height);

	inline qp Width() const
	{
		return width;
	};

	inline qp Height() const
	{
		return height;
	};

	~SizeDd();

private:
	qp width;
	qp height;
};

