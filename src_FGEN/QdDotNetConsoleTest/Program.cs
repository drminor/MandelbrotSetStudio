using System;
using MSetGeneratorClr;

namespace QdDotNetConsoleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			//var x = new QdTest();

			//var y = x.Test1(out string testString);

			//var mSetGenClrTest = new MSetGenClrTest();

			//var test22Result = mSetGenClrTest.Test22();

			//Console.WriteLine($"Hello World. Test22 returned: {test22Result}.");

			ManagedClass a = new ManagedClass();
			string b = a.get_PropertyA;
			a.MethodB(b);

			Console.WriteLine($"Hello World. String b is equal to {b}.");

			RectangleInt ri = new RectangleInt(1, 2, 3, 4);

			Console.WriteLine($"Created a RectangleInt with Width: {ri.Width}");

		}
	}
}
