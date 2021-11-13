using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using qdDotNet;

namespace QpVecClrClientConsole
{
	class Program
	{
		static void Main(string[] args)
		{

			//TestGetDiff();

			TestGetXCounts();

			//QPVecClrWrapper.Class1 c = new QPVecClrWrapper.Class1();

			//int result = c.Test1(1, 2);

			//System.Diagnostics.Debug.WriteLine($"The result is {result}.");

			//double r = c.VAdd();

			//System.Diagnostics.Debug.WriteLine($"The VAdd result is {r}.");

		}

		private static void TestGetDiff()
		{
			QPVecClrWrapper.Class1 c = new QPVecClrWrapper.Class1();
			c.TestGetDiff();

		}

		private static void TestGetXCounts()
		{
			FGenJob fjob = CreateJob();
			FGenerator fGenerator = new FGenerator(fjob);

			int size = 10000;
			uint[] counts = new uint[size];
			double[] zVals = new double[size * 4];
			bool[] doneFlags = new bool[size];

			for (int i = 0; i < counts.Length; i++)
			{
				counts[i] = 0;
			}

			for (int i = 0; i < doneFlags.Length; i++)
			{
				doneFlags[i] = false;
			}

			for (int i = 0; i < zVals.Length; i++)
			{
				zVals[i] = 0;
			}

			PointInt pos = new PointInt(0, 0);

			fGenerator.FillCounts(pos, ref counts, ref doneFlags, ref zVals);

			//for (int j = 0; j < 100; j++)
			//{
			//	System.Diagnostics.Debug.WriteLine($"y: {j}:");

			//	UInt32[] counts = fGenerator.GetXCounts(j);
			//}
		}


		private static FGenJob CreateJob()
		{
			PointDd start = new PointDd(new Dd(-2), new Dd(-1));
			PointDd end = new PointDd(new Dd(1), new Dd(1));

			SizeInt samplePoints = new SizeInt(3 * FGenerator.BLOCK_WIDTH, 2 * FGenerator.BLOCK_HEIGHT);
			uint maxIterations = 300;

			SizeInt areaSize = new SizeInt(3, 2);
			RectangleInt area = new RectangleInt(new PointInt(0, 0), areaSize);

			FGenJob fGenJob = new FGenJob(42, start, end, samplePoints, maxIterations, area);

			return fGenJob;

		}

	}
}
