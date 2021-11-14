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

			var x = new MSetGenerator();

			var z = x.Test22();

			string testString = "no yet";
			Console.WriteLine($"Hello World, the test string is {testString}");
		}
	}
}
