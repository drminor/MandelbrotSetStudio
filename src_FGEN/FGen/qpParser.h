#pragma once

#include <string>
#include <iostream>
#include "qp.h"

namespace FGen
{
	class qpMath;

	class qpParser
	{
	public:
		qpParser();
		~qpParser();

		std::string ToStr(double hi, double lo);
		std::string to_string(double hi, double lo, std::streamsize precision, int width, std::ios_base::fmtflags fmt, bool showpos, bool uppercase, char fill) const;

		int Read(std::string const& s, double &hi, double &lo) const;

		std::string GetStr(double x) const;

	private:
		qpMath * _qpCalc;

		void error(std::string const& msg) const;

		void Negate(double &hi, double &lo) const;
		int GetExp(double hi, double lo) const;
		void LdExpInPlace(double &hi, double &lo, int exp) const;
		void CopySign(double ahi, double alo, double bhi, double &rhi, double &rlo) const;
		void MulAdd(double ahi, double alo, double bhi, double blo, double chi, double clo, double &rhi, double &rlo) const;

		void ReciprocalInPlace(double &hi, double &lo) const;
		void MulQpByQpInPlace(double &ahi, double &alo, double bhi, double blo) const;

		void MulQpByDInPlace(double &ahi, double &alo, double b) const;

		void AddDToQpInPlace(double &ahi, double &alo, double b) const;
		void SubDFromQpInPlace(double &ahi, double &alo, double b) const;
		
		void Pown(double hi, int n, double &rHi, double &rLo) const;

		bool geD(double &ahi, double &alo, double b) const;
		bool ltD(double &ahi, double &alo, double b) const;
		bool isinf(double hi) const;

		void to_digits(char* s, int& expn, int precision, double hi, double lo) const;
		void append_expn(std::string& str, int expn) const;
	};
}

