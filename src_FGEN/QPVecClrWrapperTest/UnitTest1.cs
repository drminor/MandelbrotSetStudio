using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using QPVecClrWrapper;

namespace QPVecClrWrapperTest
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			QPVecClrWrapper.Class1 class1 = new Class1();

			double r = class1.VAdd();
		}
	}
}
