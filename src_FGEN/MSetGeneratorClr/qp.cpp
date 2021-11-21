#pragma once
#include "pch.h"

namespace MSetGenerator
{
	public class qp
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

		//const std::string to_string()
		//{
		//	qpParser* parser = new qpParser();
		//	std::string result = parser->ToStr(_hip, _lop);
		//	delete parser;

		//	return result;
		//}

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

		//qp(const std::string s)
		//{
		//	//qpParser parser = qpParser();
		//	//parser.Read(s, _hip, _lop);

		//	_hip = 123;
		//	_lop = 567;

		//	//std::string result = parser.ToStr(_hip, _lop);
		//}

	private:
		double _hip;
		double _lop;

	};

}

