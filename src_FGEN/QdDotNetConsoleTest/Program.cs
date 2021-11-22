using System;
using System.IO;
using System.Runtime.InteropServices;

namespace QdDotNetConsoleTest
{
	class Program
	{
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll")]
		public static extern void DisplayHelloFromDLL();


		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll")]
		public static extern int SendBigIntUsingLongs(long hi, long lo, int exponent);
		
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

			long hi = 3;
			long lo = 4;
			int exponent = 2;
			int rc = SendBigIntUsingLongs(hi, lo, exponent);

			Console.WriteLine($"Got rc: {rc} from SendBig.");

		}
	}
}
