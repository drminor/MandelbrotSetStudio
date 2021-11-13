using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using qdDotNet;

namespace qdDotNetTests
{
	[TestClass]
	public class UnitTest1
	{

		[TestMethod]
		public void TestGetFillXCount()
		{
			FGenJob fjob = CreateJob();
			FGenerator fGenerator = new FGenerator(fjob);

			int size = FGenerator.BLOCK_HEIGHT * FGenerator.BLOCK_WIDTH;

			uint[] counts = new uint[size];
			bool[] doneFlags = new bool[size];
			double[] zValues = new double[size * 4];

			PointInt position = new PointInt(0, 0);

			fGenerator.FillCounts(position, ref counts, ref doneFlags, ref zValues);
		}

		//[TestMethod]
		//public void TestMethod1()
		//{
		//	DdFractalFunctions ddFractalFunctions = new DdFractalFunctions();

		//	UInt32 mre = ddFractalFunctions.testMulDiv22();
		//	System.Diagnostics.Debug.WriteLine($"Mre = {mre}.");

		//	Dd ddRealVal = ddFractalFunctions.add(1.1, 2.2);
		//	double hi = ddRealVal.hi;

		//	Dd a = ddFractalFunctions.parse("1.01234567890123456789012345674337250E-5");

		//	string digits = ddFractalFunctions.getDigits(a);
		//	System.Diagnostics.Debug.WriteLine($"A = {digits}; Hi = {a.hi}; Lo = {a.lo}.");

		//	Dd[] samplePoints = ddFractalFunctions.getSamplePoints(ddRealVal, ddRealVal, 100);
		//}

		[TestMethod]
		public void TestCreateJob()
		{
			FGenJob fjob = CreateJob();
		}

		[TestMethod]
		public void SimpleTest()
		{
			int a = 1;
			int b = 2;
			int c = a + b;
		}

		[TestMethod]
		public void TestCreateGenerator()
		{
			FGenJob fjob = CreateJob();
			FGenerator fGenerator = new FGenerator(fjob);
		}


		[TestMethod]
		public void TestGetCounts()
		{
			FGenJob fjob = CreateJob();
			FGenerator fGenerator = new FGenerator(fjob);
			//UInt32[] counts = fGenerator.GetCounts();

			//int ptr = 0;

			//for (int j = 0; j < 10; j++)
			//{
			//	System.Diagnostics.Debug.WriteLine($"y: {j}:");

			//	for (int i = 0; i < 10; i++)
			//	{
			//		System.Diagnostics.Debug.Write($"{counts[ptr++]}   ");
			//	}
			//}
		}

		[TestMethod]
		public void TestGetXCounts()
		{
			FGenJob fjob = CreateJob();
			FGenerator fGenerator = new FGenerator(fjob);

			//for (int j = 0; j < 100; j++)
			//{
			//	System.Diagnostics.Debug.WriteLine($"y: {j}:");

			//	UInt32[] counts = fGenerator.GetXCounts(j);
			//}
		}

		private FGenJob CreateJob()
		{
			PointDd start = new PointDd(new Dd(-2), new Dd(-1));
			PointDd end = new PointDd(new Dd(1), new Dd(1));

			SizeInt samplePoints = new SizeInt(FGenerator.BLOCK_WIDTH, FGenerator.BLOCK_HEIGHT);
			uint maxIterations = 300;

			RectangleInt area = new RectangleInt(new PointInt(0, 0), samplePoints);

			FGenJob fGenJob = new FGenJob(42, start, end, samplePoints, maxIterations, area);

			return fGenJob;

		}
	}
}
