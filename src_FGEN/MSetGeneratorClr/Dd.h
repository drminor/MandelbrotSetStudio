#pragma once


using namespace System;

namespace MSetGeneratorClr
{
	public value struct Dd
	{
		double hi;
		double lo;

		Dd(double hi, double lo)
		{
			this->hi = hi;
			this->lo = lo;
		}

		Dd(double hi)
		{
			this->hi = hi;
			this->lo = 0;
		}

		//Dd(qp val)
		//{
		//	this->hi = val._hi();
		//	this->lo = val._lo();
		//}

		//Dd(String^ val);

		//qp ToQp()
		//{
		//	qp result = qp(this->hi, this->lo);qp result = qp(this->hi, this->lo);
		//	return result;
		//}

		//String^ GetStringVal();

	};

}
