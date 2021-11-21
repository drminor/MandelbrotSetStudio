#pragma once
#include "pch.h"

//using namespace System;
//using namespace Runtime::InteropServices;

#include "qpParser.cpp"

using namespace System;

using namespace MSetGenerator;

class UnmanagedDd {
public:

	const std::string GetStringFromDouble(double a, double b)
	{
		qpParser* qpPa = new qpParser();
		std::string result = qpPa->ToStr(a, b);
		delete qpPa;

		return result;
	}

	void Read(std::string const& s, double& hi, double& lo) const
	{
		qpParser* qpPa = new qpParser();
		qpPa->Read(s, hi, lo);
		delete qpPa;
	}

};

namespace MSetGeneratorClr
{
	public value struct Dd
	{
		double hi;
		double lo;

	public:
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

		property double Hi { double get() { return hi; } }
		property double Lo { double get() { return lo; } }

		String^ GetStringVal()
		{
			UnmanagedDd* unmanagedDd = new UnmanagedDd();
			std::string strResult = unmanagedDd->GetStringFromDouble(Hi, Lo);
			String^ result = gcnew String(strResult.c_str());
			delete unmanagedDd;

			return result;
		}

		Dd(String^ s)
		{
			using namespace Runtime::InteropServices;

			const char* chars = (const char*)(Marshal::StringToHGlobalAnsi(s)).ToPointer();
			std::string st = chars;

			double tHi = this->hi;
			double tLo = this->lo;
			UnmanagedDd* unmanagedDd = new UnmanagedDd();
			unmanagedDd->Read(st, tHi, tLo);
			Marshal::FreeHGlobal(IntPtr((void*)chars));

			delete unmanagedDd;

		}

		//void MarshalString(String^ s, string& os) {
		//	using namespace Runtime::InteropServices;
		//	const char* chars =
		//		(const char*)(Marshal::StringToHGlobalAnsi(s)).ToPointer();
		//	os = chars;
		//	Marshal::FreeHGlobal(IntPtr((void*)chars));
		//}


		//Dd(qp val)
		//{
		//	this->hi = val._hi();
		//	this->lo = val._lo();
		//}

		//Dd(String^ val)
		//{
		//	const char* chars = (const char*)(Marshal::StringToHGlobalAnsi(val)).ToPointer();
		//	std::string strVal = chars;
		//	qp temp = qp(strVal);
		//	this->hi = temp._hi();
		//	this->lo = temp._lo();

		//	Marshal::FreeHGlobal(IntPtr((void*)chars));
		//}

		//qp ToQp()
		//{
		//	qp result = qp(this->hi, this->lo);qp result = qp(this->hi, this->lo);
		//	return result;
		//}

		//String^ GetStringVal()
		//{
		//	qp temp = qp(this->hi, this->lo);
		//	std::string strVal = temp.to_string();

		//	// TODO: Do we need to free up the memory allocated to strVal?
		//	String^ result = gcnew String(strVal.c_str());

		//	return result;
		//}

	};

}




