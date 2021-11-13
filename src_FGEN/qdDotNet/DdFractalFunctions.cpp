#include "stdafx.h"
#include "DdFractalFunctions.h"

#include <qd/dd_real.h>

namespace qdDotNet
{
	// Constructor implementaion
	DdFractalFunctions::DdFractalFunctions()
	{
		myDdReal = new dd_real();
	}

	Dd qdDotNet::DdFractalFunctions::add(double a, double b)
	{
		dd_real temp = myDdReal->add(a, b);
		Dd result = Dd(temp._hi(), temp._lo());
		return result;
	}

	Dd DdFractalFunctions::parse(String^ val)
	{

		//std::string strVal;
		//MarshalString(val, strVal);

		//dd_real temp = dd_real(strVal);

		Dd result = Dd(val);

		return result;
	}

	String^ DdFractalFunctions::getDigits(Dd val)
	{
		dd_real temp = val.ToDdReal();

		std::string strVal = temp.to_string();

		String^ result = gcnew String(strVal.c_str());

		return result;
	}


	cli::array<Dd>^ DdFractalFunctions::getSamplePoints(Dd start, Dd end, int extent)
	{
		cli::array<Dd>^ result = gcnew cli::array<Dd>(extent);

		for (int i = 0; i < extent; i++) {
			result[i] = qdDotNet::Dd(0, 0);
		}

		return result;
	}

	UInt32 DdFractalFunctions::testMulDiv22() {

		//dd_real temp = dd_real(0,0);

		//temp.add(1, 1);

		UInt32 mre = testMultDiv2();
		return mre;
	}



}

//// Declares and initializes an array of user-defined value types.
//array< MyStruct >^ MyStruct1 = gcnew array< MyStruct >(ARRAY_SIZE);
//for (i = 0; i < ARRAY_SIZE; i++) {
//	MyStruct1[i] = MyStruct();
//	MyStruct1[i].m_i = i + 40;
//}
//
//for (i = 0; i < ARRAY_SIZE; i++)
//	Console::WriteLine("MyStruct1[{0}] = {1}", i, MyStruct1[i].m_i);