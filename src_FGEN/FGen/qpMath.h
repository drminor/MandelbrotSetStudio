#pragma once


#ifdef FGEN_EXPORTS
#define FGEN_API __declspec(dllexport)
#else
#define FGEN_API __declspec(dllimport)
#endif

#include "qp.h"

namespace FGen
{
	class qpMath
	{

	public:
		qpMath();

		qp sub(qp a, qp b);
		qp add(qp a, qp b);

		qp mulD(qp a, double b);

		void addQpAndQp(double ahis, double alos, double bhis, double blos, double &rhis, double &rlos);
		void addDToQp(double ahi, double alo, double b, double &rhi, double &rlo);

		void mulQpByD(double hi, double lo, double f, double &rhi, double &rlo);
		void mulQpByQp(double ahis, double alos, double bhis, double blos, double &rhis, double &rlos);

		void sqrQp(double ahi, double alo, double &rhi, double &rlo);

		void mulQpByQpROp(double ahi, double alo, double bhi, double blo, double * r);
		void addOpAndQp(double const* a, double bhi, double blo, double * s);
		void renorm(double &c0, double &c1, double &c2, double &c3, double &c4);

		double two_sum(double a, double b, double &err);
		double quick_two_sum(double a, double b, double & err);

		void three_sum(double &a, double &b, double &c);
		void three_sum2(double &a, double &b, double c);

		double two_prod(double a, double b, double &err);
		double two_sqr(double a, double &err);

		void split(double a, double &hi, double &lo);

		~qpMath();
	};
}

