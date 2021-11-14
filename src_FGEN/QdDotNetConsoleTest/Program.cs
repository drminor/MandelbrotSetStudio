using System;
using ClrClassLib;

namespace QdDotNetConsoleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			//var x = new QdTest();

			//var y = x.Test1(out string testString);

			var x = new ClrClassLib.Class1();

			var z = x.Test22();

			string testString = "no yet";
			Console.WriteLine($"Hello World, the test string is {testString}");
		}
	}
}
