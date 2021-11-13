#pragma once

#include "../FGen/FGen.h"

using namespace System;
using namespace FGen;

namespace qdDotNet {

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

		Dd(qp val)
		{
			this->hi = val._hi();
			this->lo = val._lo();
		}

		Dd(String^ val);

		qp ToQp()
		{
			qp result = qp(this->hi, this->lo);
			return result;
		}

		String^ GetStringVal()
		{
			qp temp = ToQp();
			std::string strVal = temp.to_string();

			String^ result = gcnew String(strVal.c_str());

			return result;
		}

	};

	inline void MarshalString(String ^ s, std::string& os) {
		using namespace Runtime::InteropServices;
		const char* chars = (const char*)(Marshal::StringToHGlobalAnsi(s)).ToPointer();
		os = chars;
		Marshal::FreeHGlobal(IntPtr((void*)chars));
	}

}
