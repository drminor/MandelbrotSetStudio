#pragma once

#ifdef MSETGEN_EXPORTS
#define MSETGEN_API __declspec(dllexport)
#else
#define MSETGEN_API __declspec(dllimport)
#endif

#include <string>

namespace MSetGenerator
{
	class MSETGEN_API qp
	{

	public:

		double _hi() const { return _hip; }
		double _lo() const { return _lop; }

		void resetToZero() {
			_hip = 0;
			_lop = 0;
		}

		double toDouble() const
		{
			return _hip + _lop;
		}

		const std::string to_string();

		qp()
		{
			_hip = 0.0;
			_lop = 0.0;
		}

		qp(double h)
		{
			_hip = h;
			_lop = 0.0;
		}

		qp(double hi, double lo)
		{
			_hip = hi;
			_lop = lo;
		}

		//qp(std::string const& s);
		qp(const std::string);

		//qp(const qp &v)
		//{
		//	x = new double[2];
		//	x[0] = v._hi();
		//	x[1] = v._lo();
		//}

		//qp & operator=(const qp &L)
		//{
		//	// check for "self assignment" and do nothing in that case
		//	if (this == &L) return *this;
		//	else {
		//		delete[] x;                // free the storage pointed to by Items
		//		x = new double[2];
		//		x[0] = L._hi();
		//		x[1] = L._lo();

		//		return *this;                   // return this IntList
		//	}
		//}

		~qp();

	private:
		double _hip;
		double _lop;

	};

}


