// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"


bool ThreadAttach( HMODULE /*hModule*/, LPVOID /*lpReserved*/ )
{
	return true;
}

bool ProcessAttach( HMODULE hModule, LPVOID lpReserved )
{
	return ThreadAttach( hModule, lpReserved );
}

void ThreadDetach( HMODULE /*hModule*/, LPVOID /*lpReserved*/ )
{
}

void ProcessDetach( HMODULE hModule, LPVOID lpReserved )
{
	ThreadDetach( hModule, lpReserved );
}


BOOL APIENTRY DllMain(
	HMODULE hModule,
	DWORD   ul_reason_for_call,
	LPVOID  lpReserved
)
{
	switch ( ul_reason_for_call )
	{
		case DLL_PROCESS_ATTACH:
			return ProcessAttach( hModule, lpReserved );

		case DLL_THREAD_ATTACH:
			ThreadAttach( hModule, lpReserved );
			return true;

		case DLL_THREAD_DETACH:
			ThreadDetach( hModule, lpReserved );
			return true;

		case DLL_PROCESS_DETACH:
			ProcessDetach( hModule, lpReserved );
			return true;

		default:
			return false;
	}
}
