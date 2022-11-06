using MSS.Common;
using System.Diagnostics;
using System.Numerics;

namespace MSetGenP
{
	internal class IteratorVector
	{
		private SmxVecMathHelper _smxVecMathHelper;
		private int _targetIterations;

		public IteratorVector(SmxVecMathHelper smxVecMathHelper, int targetIterations)
		{
			_smxVecMathHelper = smxVecMathHelper;
			_targetIterations = targetIterations;
		}

		public ushort Iterate(Smx cR, Smx cI)
		{
			var result = Iterate(cR, cI, cntr: 0, Smx.Zero, Smx.Zero);
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI)
		{
			var zRSqr = SmxMathHelper.Square(zR);
			var zISqr = SmxMathHelper.Square(zI);

			var sumOfSqrs = SmxMathHelper.Add(zRSqr, zISqr);

			while (!SmxMathHelper.IsGreaterOrEqThan(sumOfSqrs, 4) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = SmxMathHelper.Add(zR, zI);

					// square(z.r + z.i)
					var zRZiSqr = SmxMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = SmxMathHelper.Sub(zRZiSqr, zRSqr);
					zI = SmxMathHelper.Sub(zI, zISqr);
					//zI = SmxMathHelper.Add(zI, cI, out cI);
					zI = SmxMathHelper.Add(zI, cI);

					// z.r = zrsqr - zisqr + c.r
					zR = SmxMathHelper.Sub(zRSqr, zISqr);
					//zR = SmxMathHelper.Add(zR, cR, out cR);
					zR = SmxMathHelper.Add(zR, cR);

					zRSqr = SmxMathHelper.Square(zR);
					zISqr = SmxMathHelper.Square(zI);

					sumOfSqrs = SmxMathHelper.Add(zRSqr, zISqr);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}

			//if (cntr < _targetIterations)
			//{
			//	var sacResult = SmxMathHelper.SumAndCompare(zRSqr, zISqr, 4);
			//	var sumOfZrSqrAndZiSqr = SmxMathHelper.Add(zRSqr, zISqr);
			//	var rValDiag = sumOfZrSqrAndZiSqr.GetStringValue();
			//	Debug.WriteLine($"Balied out: The value is {rValDiag}. SumAndCompare returned: {sacResult}.");
			//}

			return cntr;
		}

		public Smx[] BuildSamplePoints(Smx startValue, Smx[] samplePoints)
		{
			var result = new Smx[samplePoints.Length];

			for (var i = 0; i < samplePoints.Length; i++)
			{
				result[i] = SmxMathHelper.Add(startValue, samplePoints[i]);
			}

			return result;
		}

		public Smx[] BuildSamplePointOffsets(Smx delta, int sampleCount)
		{
			var result = new Smx[sampleCount];

			for (var i = 0; i < sampleCount; i++)
			{
				result[i] = SmxMathHelper.Multiply(delta, i);
			}

			return result;
		}

		public void Sample()
		{
			var lanes = Vector<int>.Count;

			int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			int[] b = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
			int[] c = new int[a.Length];

			for (int i = 0; i < a.Length; i += lanes)
			{
				var a8 = new Vector<int>(a, i);
				var b8 = new Vector<int>(b, i);
				(a8 + b8).CopyTo(c, i);
			}
		}

			/*
			1 using System.Numerics;
			2 namespace test { class P { static void Main(string[] args)
			3 {
			4 var lanes = Vector<int>.Count;
			5 int[] a = { 1, 2, 3, 4, 5, 6, 7, 8 };
			6 int[] b = { 1, 1, 1, 1, 1, 1, 1, 1 };
			7 int[] c = new int[a.Length];
			8 for( int i = 0; i < a.Length; i += lanes )
			9 {
			10 var a8 = new Vector<int>(a, i);
			11 var b8 = new Vector<int>(b, i);
			12 (a8 + b8).CopyTo(c, i);
			13 }}}}
			*/

		}
}
