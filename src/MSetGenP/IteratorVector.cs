using System.Diagnostics;
using System.Numerics;

namespace MSetGenP
{
	public class IteratorVector
	{
		private readonly SmxMathHelper _smxMathHelper;
		private SmxVecMathHelper _smxVecMathHelper;

		public IteratorVector(SmxVecMathHelper smxVecMathHelper)
		{
			_smxMathHelper = new SmxMathHelper(smxVecMathHelper.Precision);
			_smxVecMathHelper = smxVecMathHelper;
		}

		public void Iterate(FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		{
			try
			{
				// z.r + z.i
				var zRZIs = _smxVecMathHelper.Add(zRs, zIs);

				// square(z.r + z.i)
				var zRZiSqrs = _smxVecMathHelper.Square(zRZIs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				zIs = _smxVecMathHelper.Sub(zRZiSqrs, zRSqrs);
				zIs = _smxVecMathHelper.Sub(zIs, zISqrs);
				zIs = _smxVecMathHelper.Add(zIs, cIs);

				// z.r = zrsqr - zisqr + c.r
				zRs = _smxVecMathHelper.Sub(zRSqrs, zISqrs);
				zRs = _smxVecMathHelper.Add(zRs, cRs);

				zRSqrs = _smxVecMathHelper.Square(zRs);
				zISqrs = _smxVecMathHelper.Square(zIs);

				//sumOfSqrs = _smxVecMathHelper.Add(zRSqrs, zISqrs);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}
		
		public Smx[] BuildSamplePoints(Smx startValue, Smx[] samplePointOffsets)
		{
			var result = new Smx[samplePointOffsets.Length];

			for (var i = 0; i < samplePointOffsets.Length; i++)
			{
				result[i] = _smxMathHelper.Add(startValue, samplePointOffsets[i]);
			}

			return result;
		}

		public Smx[] BuildSamplePointOffsets(Smx delta, int sampleCount)
		{
			var result = new Smx[sampleCount];

			for (var i = 0; i < sampleCount; i++)
			{
				result[i] = _smxMathHelper.Multiply(delta, i);
			}

			return result;
		}

		public void Sample()
		{
			var lanes = Vector<ulong>.Count;

			ulong[] a = { 1, 2, 3, 4, 5, 6, 7, 8 };
			ulong[] b = { 1, 1, 1, 1, 1, 1, 1, 1 };
			ulong[] c = new ulong[a.Length];

			for (int i = 0; i < a.Length; i += lanes)
			{
				var a8 = new Vector<ulong>(a, i);
				var b8 = new Vector<ulong>(b, i);
				(a8 + b8).CopyTo(c, i);
			}

			for (int i = 0; i < a.Length; i++)
			{
				Debug.WriteLine($"item: {i}: {c[i]}.");
			}
		}
	}
}
