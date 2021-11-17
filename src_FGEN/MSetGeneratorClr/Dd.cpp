#pragma once

//using namespace System;
//using namespace Runtime::InteropServices;


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




