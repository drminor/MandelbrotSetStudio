// wrap_native_class_for_mgd_consumption.cpp
// compile with: /clr /LD
#include <windows.h>
#include <vcclr.h>
#using <System.dll>

using namespace System;

class UnmanagedClass {
public:
        LPCWSTR GetPropertyA()
        {
            LPCWSTR s = L"hi";
            return s;
        }

    void MethodB(LPCWSTR) {}
};

#include <string>

namespace MSetGenerator
{
	class qp
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

		const std::string to_string()
		{
			//qpParser parser = qpParser();

			std::string result = "to-string-test";
			return result;
		}


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

		qp(const std::string s)
		{
			//qpParser parser = qpParser();
			//parser.Read(s, _hip, _lop);

			_hip = 123;
			_lop = 567;

			//std::string result = parser.ToStr(_hip, _lop);
		}

	private:
		double _hip;
		double _lop;

	};

}
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

    public ref class ManagedClass {
    public:
        // Allocate the native object on the C++ Heap via a constructor
        ManagedClass() : m_Impl(new UnmanagedClass) {}

        // Deallocate the native object on a destructor
        ~ManagedClass() {
            delete m_Impl;
        }

    protected:
        // Deallocate the native object on the finalizer just in case no destructor is called
        !ManagedClass() {
            delete m_Impl;
        }

    public:
        property String^ get_PropertyA {
            String^ get() {
                return gcnew String(m_Impl->GetPropertyA());
            }
        }

        void MethodB(String^ theString) {
            pin_ptr<const WCHAR> str = PtrToStringChars(theString);
            m_Impl->MethodB(str);
        }

    private:
        UnmanagedClass* m_Impl;
    };

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