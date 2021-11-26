#pragma once

#include <cmath>
#include <stdexcept>

class qp
{

private:
	double _hip;
	double _lop;

public:

	static void initializeStaticMembers();

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

	qp(LONGLONG h)
	{
		_hip = GetDouble(h);
		_lop = 0.0;
	}

	qp(double hi, double lo)
	{
		_hip = hi;
		_lop = lo;
	}

	qp(LONGLONG hi, LONGLONG lo)
	{
		_hip = GetDouble(hi);
		_lop = GetDouble(lo);
	}

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

	~qp()
	{
	}

private:
	double GetDouble(LONGLONG l);

};



