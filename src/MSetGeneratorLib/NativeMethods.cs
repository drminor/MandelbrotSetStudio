using System;
using System.Runtime.InteropServices;

namespace MSetGeneratorLib
{
	internal static class NativeMethods
    {
		//[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern void GenerateMapSection(MapSectionRequestStruct requestStruct, IntPtr counts, IntPtr doneFlags, IntPtr zValues);

		////Use this for "PROD" deployment.

		//[DllImport("MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void GenerateMapSection(MapSectionRequestStruct requestStruct, IntPtr counts, IntPtr doneFlags, IntPtr zValues);

		//[DllImport("MSetGenerator.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void GetStringValues(MapSectionRequestStruct requestStruct, 
			[Out][MarshalAs(UnmanagedType.LPStr)] out string px,
			[Out][MarshalAs(UnmanagedType.LPStr)] out string py,
			[Out][MarshalAs(UnmanagedType.LPStr)] out string deltaW,
			[Out][MarshalAs(UnmanagedType.LPStr)] out string deltaH);


		#region Just for Testing

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll")]
        internal static extern void DisplayHelloFromDLL();

        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SendBigIntUsingLongs(long hi, long lo, int exponent);

        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ConvertLongsToDoubles(long hi, long lo, int exponent, double[] buffer);

        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GenerateMapSection1(long hi, long lo, int exponent, ref IntPtr array, int size);

		#endregion
    }
}
