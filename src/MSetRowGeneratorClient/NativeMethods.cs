using System;
using System.Runtime.InteropServices;

namespace MSetRowGeneratorClient
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
		internal static extern int GenerateMapSectionRow(MSetRowRequestStruct requestStruct, IntPtr counts);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest(MSetRowRequestStruct requestStruct, IntPtr counts);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest2(MSetRowRequestStruct requestStruct, IntPtr crsForARow, IntPtr ciVec, IntPtr countsForARow);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest3(MSetRowRequestStruct requestStruct, IntPtr crsForARow, IntPtr ciVec, IntPtr countsForARow);



		//[DllImport("Whisper.dll", EntryPoint = "Exist", CallingConvention = CallingConvention.Cdecl)]
		//[return: MarshalAs(UnmanagedType.I1)]
		//public static extern bool Exist([MarshalAs(UnmanagedType.LPStr)] string name);




	}
}
