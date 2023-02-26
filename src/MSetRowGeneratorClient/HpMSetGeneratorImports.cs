using System;
using System.Runtime.InteropServices;

namespace MSetRowGeneratorClient
{
	internal static class HpMSetGeneratorImports
    {
		//	..\\.\\source\repos\MandelbrotSetStudio\x64\Debug
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int GenerateMapSectionRow(MSetRowRequestStruct requestStruct, IntPtr crsForARow, IntPtr ciVec, IntPtr countsForARow);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest(MSetRowRequestStruct requestStruct, IntPtr counts);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest2(MSetRowRequestStruct requestStruct, IntPtr crsForARow, IntPtr ciVec, IntPtr countsForARow);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest3(MSetRowRequestStruct requestStruct, IntPtr crsForARow, IntPtr ciVec, IntPtr countsForARow);


		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\HpMSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int BaseSimdTest4(MSetRowRequestStruct requestStruct, IntPtr crsForARow, IntPtr ciVec, IntPtr countsForARow);

		/* How to declare a extern that returns a bool.
			[DllImport("Whisper.dll", EntryPoint = "Exist", CallingConvention = CallingConvention.Cdecl)]
			[return: MarshalAs(UnmanagedType.I1)]
			public static extern bool Exist([MarshalAs(UnmanagedType.LPStr)] string name);
		*/
	}
}
