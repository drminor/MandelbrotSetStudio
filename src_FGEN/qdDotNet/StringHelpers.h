//#pragma once
//
//// convert_system_string.cpp
//// compile with: /clr
//#include <string>
////#include <iostream>
//using namespace std;
//
//using namespace System;
//
//
//ref class StringHelpers
//{
//public:
//	StringHelpers();
//
//
//	void MarshalString(String ^ s, string& os) {
//		using namespace Runtime::InteropServices;
//		const char* chars =
//			(const char*)(Marshal::StringToHGlobalAnsi(s)).ToPointer();
//		os = chars;
//		Marshal::FreeHGlobal(IntPtr((void*)chars));
//	}
//
//	void MarshalString(String ^ s, wstring& os) {
//		using namespace Runtime::InteropServices;
//		const wchar_t* chars =
//			(const wchar_t*)(Marshal::StringToHGlobalUni(s)).ToPointer();
//		os = chars;
//		Marshal::FreeHGlobal(IntPtr((void*)chars));
//	}
//
//};





