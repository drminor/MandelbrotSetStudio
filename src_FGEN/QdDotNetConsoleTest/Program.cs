using System;
using System.Runtime.InteropServices;
using System.Numerics;

using static MSS.Types.BigIntegerExtensions;

namespace QdDotNetConsoleTest
{
	class Program
	{
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll")]
		public static extern void DisplayHelloFromDLL();


		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SendBigIntUsingLongs(long hi, long lo, int exponent);

		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void ConvertLongsToDoubles(long hi, long lo, int exponent, double[] buffer);


		static void Main(string[] args)
		{
			//var x = new QdTest();

			//var y = x.Test1(out string testString);

			//var mSetGenClrTest = new MSetGenClrTest();

			//var test22Result = mSetGenClrTest.Test22();

			//Console.WriteLine($"Hello World. Test22 returned: {test22Result}.");

			//ManagedClass a = new ManagedClass();
			//string b = a.get_PropertyA;
			//a.MethodB(b);

			//ManagedClass2 a = new ManagedClass2();
			//string b = a.GetStringFromDouble(12.7639);

			//Console.WriteLine($"Hello World. String b is equal to {b}.");

			//RectangleInt ri = new RectangleInt(1, 2, 3, 4);

			//Console.WriteLine($"Created a RectangleInt with Width: {ri.Width}");

			//Dd dd = new Dd(23.15d);
			//string strDd = dd.GetStringVal();

			//Dd dd = new Dd("123.45");

			//Console.WriteLine($"Created a Dd: {dd.hi}, {dd.lo}");

			Console.WriteLine("This is C# program");
			DisplayHelloFromDLL();

			long hi = 0;
			long lo = 9;
			int exponent = -2;
			int rc = SendBigIntUsingLongs(hi, lo, exponent);

			Console.WriteLine($"Got rc: {rc} from SendBig.");

			BigInteger a = new BigInteger(Math.ScaleB(5, 54));
			a += 245;

			long[] tmp = a.ToLongs();

			double[] buf = new double[2];
			ConvertLongsToDoubles(tmp[0], tmp[1], 0, buf);

			BigInteger b = new BigInteger(buf[0]);
			b += (BigInteger)buf[1];

			Console.WriteLine($"The BigInteger before: {a} and after {b}.");


		}
	}
}
