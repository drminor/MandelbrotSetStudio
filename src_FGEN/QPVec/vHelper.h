#pragma once

#ifdef QPVEC_EXPORTS
#define QPVEC_API __declspec(dllexport)
#else
#define QPVEC_API __declspec(dllimport)
#endif

namespace qpvec
{
	class QPVEC_API vHelper
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

}
