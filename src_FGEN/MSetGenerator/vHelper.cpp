#pragma once

#include "pch.h"
#include "vHelper.h"

vHelper::vHelper()
{
}

vHelper::~vHelper()
{
}

void vHelper::clearVec(int n, double * vec)
{
	for (int i = 0; i < n; i++)
	{
		vec[i] = 0.0;
	}
}

double * vHelper::createAndInitVec(int n, double val)
{
	double * result = new double[n];

	for (int i = 0; i < n; i++)
	{
		result[i] = val;
	}

	return result;
}
