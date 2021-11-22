// MSetGenerator.h : main header file for the MSetGenerator DLL
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'pch.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CMSetGeneratorApp
// See MSetGenerator.cpp for the implementation of this class
//

class CMSetGeneratorApp : public CWinApp
{
public:
	CMSetGeneratorApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
};
