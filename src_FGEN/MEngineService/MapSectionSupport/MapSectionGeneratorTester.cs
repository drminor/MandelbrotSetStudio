using System;
using System.Runtime.InteropServices;
using System.Numerics;

using static MSS.Types.BigIntegerExtensions;
using System.Linq;
using MEngineDataContracts;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;

namespace MEngineService
{
	public class MapSectionGeneratorTester
	{
		public static void TestTheGenerator()
		{
			MapSectionRequest request = new MapSectionRequest();
			request.SubdivisionId = "TestA";
			request.BlockPosition = new PointInt(0, 0);
			request.Position = new RPointDto(new BigInteger[] { 4, 5 }, 1);
			request.BlockSize = new SizeInt(128, 128);
			request.SamplePointsDelta = new RSizeDto(new BigInteger[] { 1, 1 }, -8);
			request.MapCalcSettings = new MapCalcSettings(maxIterations: 400, threshold: 4, iterationsPerStep: 100);

			var response = new MapSectionGenerator().GenerateMapSection(request);

			Console.WriteLine($"The response has {response.Counts.Length} count values.");
		}

		public static void BasicTest()
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
			NativeMethods.DisplayHelloFromDLL();

			// ---------
			long hi = 0;
			long lo = 9;
			int exponent = -2;
			int rc = NativeMethods.SendBigIntUsingLongs(hi, lo, exponent);

			Console.WriteLine($"Got rc: {rc} from SendBig.");

			// ----------
			BigInteger a = new BigInteger(Math.ScaleB(5, 54));
			a += 245;

			long[] tmp = a.ToLongs();

			double[] buf = new double[2];
			NativeMethods.ConvertLongsToDoubles(tmp[0], tmp[1], 0, buf);

			BigInteger b = new BigInteger(buf[0]);
			b += (BigInteger)buf[1];

			Console.WriteLine($"The BigInteger before: {a} and after {b}.");

			// -------
			int size = 10;
			IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * size);
			NativeMethods.GenerateMapSection1(tmp[0], tmp[1], 0, ref buffer, size);

			int[] arrayRes = new int[size];
			Marshal.Copy(buffer, arrayRes, 0, size);
			Marshal.FreeCoTaskMem(buffer);

			// TODO: Update C++ code to use integers
			uint[] res = arrayRes.Cast<uint>().ToArray();

			for (int i = 0; i < size; i++)
			{
				Console.WriteLine($"The results[{i}] is {res[i]}.");
			}

			//------

			MapSectionRequest request = new MapSectionRequest();
			request.SubdivisionId = "TestA";
			request.BlockPosition = new PointInt(0, 0);
			request.Position = new RPointDto(new BigInteger[] { 4, 5 }, 1);
			request.BlockSize = new SizeInt(128, 128);
			request.SamplePointsDelta = new RSizeDto(new BigInteger[] { 1, 1 }, -8);
			request.MapCalcSettings = new MapCalcSettings(maxIterations: 400, threshold: 4, iterationsPerStep: 100);

			MapSectionRequestStruct requestStruct = new MapSectionReqHelper().GetRequestStruct(request);

			int length = 10;
			IntPtr rawCnts = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * length);
			NativeMethods.GenerateMapSection(requestStruct, ref rawCnts, length);

			int[] tmpCnts = new int[length];
			Marshal.Copy(rawCnts, tmpCnts, 0, length);
			Marshal.FreeCoTaskMem(rawCnts);

			// TODO: Update C++ code to use integers
			uint[] cnts = tmpCnts.Cast<uint>().ToArray();

			for (int i = 0; i < length; i++)
			{
				Console.WriteLine($"The results[{i}] is {cnts[i]}.");
			}

		}
	}
}
