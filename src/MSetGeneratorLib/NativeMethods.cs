﻿using System;
using System.Runtime.InteropServices;

namespace MSetGeneratorLib
{
	internal static class NativeMethods
    {
		//[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern void GetStringValues(MapSectionRequestStruct requestStruct, 
		//	[Out][MarshalAs(UnmanagedType.LPStr)] out string px,
		//	[Out][MarshalAs(UnmanagedType.LPStr)] out string py,
		//	[Out][MarshalAs(UnmanagedType.LPStr)] out string deltaW,
		//	[Out][MarshalAs(UnmanagedType.LPStr)] out string deltaH);


		//[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern void GenerateMapSection(MapSectionRequestStruct requestStruct, IntPtr counts, IntPtr doneFlags, IntPtr zValues);

		//C:\Users\david\source\repos\MandelbrotSetStudio\x64\Debug
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]



		internal static extern void GenerateMapSection(MapSectionRequestStruct requestStruct, IntPtr counts);




	}
}
