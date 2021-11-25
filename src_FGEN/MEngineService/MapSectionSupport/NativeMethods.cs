using System;
using System.Runtime.InteropServices;

namespace MEngineService
{
	internal static class NativeMethods
    {
        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GenerateMapSection(MapSectionRequestStruct requestStruct, ref IntPtr array, int size);

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
