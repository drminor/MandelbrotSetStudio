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
