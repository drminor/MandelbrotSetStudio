using MSS.Common;
using System.Diagnostics;

namespace MSetGenP
{
	internal class IteratorScalar
	{
		private int _targetIterations;

		public IteratorScalar(int targetIterations)
		{
			_targetIterations = targetIterations;
		}

		public ushort Iterate(FPValues cRs, int rIndex, FPValues cIs, int iIndex)
		{
			var cR = new Smx(cRs.Signs[rIndex], GetMantissa(cRs, rIndex), cRs.Exponents[rIndex], 55);
			var cI = new Smx(cIs.Signs[iIndex], GetMantissa(cIs, iIndex), cIs.Exponents[iIndex], 55);

			var result = Iterate(cR, cI);

			return result;
		}

		private ulong[] GetMantissa(FPValues fPValues, int index)
		{
			var numberOfLimbs = fPValues.Mantissas.Length;
			var result = new ulong[numberOfLimbs];

			for (var i = 0; i < numberOfLimbs; i++)
			{
				result[i] = fPValues.Mantissas[i][index];
			}

			return result;
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

	}
}
