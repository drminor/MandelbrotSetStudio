#include "pch.h"
// wrap_native_class_for_mgd_consumption.cpp
// compile with: /clr /LD
//#include <windows.h>
//#include <vcclr.h>
//#using <System.dll>

#include "qp.cpp"

using namespace System;

using namespace MSetGenerator;

class UnmanagedClass2 {
public:

    const char* GetStringFromDouble(double a, double b)
    {
        MSetGenerator::qp temp = MSetGenerator::qp(a, b);
        std::string strVal = temp.to_string();

        //std::string strVal = "hi";

        const char* result = strVal.c_str();

        return result;
    }

};

namespace MSetGeneratorClr 
{
    public ref class ManagedClass2 {
    public:
        // Allocate the native object on the C++ Heap via a constructor
        ManagedClass2() : m_Impl(new UnmanagedClass2) {}

        // Deallocate the native object on a destructor
        ~ManagedClass2() {
            delete m_Impl;
        }

    protected:
        // Deallocate the native object on the finalizer just in case no destructor is called
        !ManagedClass2() {
            delete m_Impl;
        }

    public:

        String^ GetStringFromDouble(double d) {
			const char* strResult = m_Impl->GetStringFromDouble(d, d);
			String^ result = gcnew String(strResult);

			return result;
        }

    private:
        UnmanagedClass2* m_Impl;
    };

}