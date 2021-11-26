#pragma once

class vHelper
{
public:

	vHelper();
	~vHelper();

	inline double * createVec(int n)
	{
		return new double[n];
	}

	void clearVec(int n, double * vec);
	double * createAndInitVec(int n, double val);
};
