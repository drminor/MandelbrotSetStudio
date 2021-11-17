#include "pch.h"
#include "dd.h"

#include "../MSetGenerator/ddBridge.h"

using namespace System;
using namespace System::Runtime::InteropServices;

#include <stdio.h>

namespace MSetGeneratorClr
{
	using namespace MSetGenerator;


	public ref class MSetGenClrTest
	{

	public:

		double MSetGenClrTest::Test22()
		{
			String^ pStr = "Hello World!";
			char* pChars = (char*)Marshal::StringToHGlobalAnsi(pStr).ToPointer();
			puts(pChars);

			Marshal::FreeHGlobal((IntPtr)pChars);

			double hi = 0.9;
			Dd f = Dd(hi);

			double a = 1.4;
			double b = 2.5;

			ddBridge g = ddBridge();

			const char* rr = g.test(a, b);

			return 22.1;
		}
	};


}




