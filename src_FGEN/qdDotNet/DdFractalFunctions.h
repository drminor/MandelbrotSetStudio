#pragma once

#include <qd/dd_real.h>
#include "Dd.h"

using namespace System;

namespace qdDotNet {

	public ref class DdFractalFunctions
	{

	public:
		// constructor
		DdFractalFunctions();

		// wrapper methods
		Dd add(double a, double b);

		Dd parse(String^ val);

		String^ getDigits(Dd val);

		cli::array<Dd>^ getSamplePoints(Dd start, Dd end, int extent);

		UInt32 testMulDiv22();


	private:
		dd_real *myDdReal; // an instance of class in C++

	};




}

