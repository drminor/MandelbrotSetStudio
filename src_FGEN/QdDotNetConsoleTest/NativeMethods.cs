using System;
using System.Runtime.InteropServices;

namespace QdDotNetConsoleTest
{
	internal static class NativeMethods
    {
        //// Declares a managed prototype for unmanaged function.
        //[DllImport("..\\LIB\\PinvokeLib.dll", CallingConvention = CallingConvention.Cdecl)]
        //internal static extern int TestStructInStruct(ref MyPerson2 person2);

        //[DllImport("..\\LIB\\PinvokeLib.dll", CallingConvention = CallingConvention.Cdecl)]
        //internal static extern int TestStructInStruct3(MyPerson3 person3);

        //[DllImport("..\\LIB\\PinvokeLib.dll", CallingConvention = CallingConvention.Cdecl)]
        //internal static extern int TestArrayInStruct(ref MyArrayStruct myStruct);

        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll")]
        internal static extern void DisplayHelloFromDLL();


        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SendBigIntUsingLongs(long hi, long lo, int exponent);

        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ConvertLongsToDoubles(long hi, long lo, int exponent, double[] buffer);

        [DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GenerateMapSection(long hi, long lo, int exponent, ref IntPtr array, int size);

    }


}
