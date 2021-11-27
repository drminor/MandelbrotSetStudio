#include "pch.h"
#include "SizeDd.h"

SizeDd::SizeDd()
{
	width = qp();
	height = qp();
}

SizeDd::SizeDd(qp width, qp height) : width(width), height(height)
{
}

SizeDd::~SizeDd()
{
}
