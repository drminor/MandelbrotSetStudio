using MSS.Common;
using System.Diagnostics;

namespace MSetGenP
{
	internal class IteratorScalar
	{
		private SmxMathHelper _smxMathHelper;
		private int _targetIterations;

		public IteratorScalar(SmxMathHelper smxMathHelper, int targetIterations)
		{
			_smxMathHelper = smxMathHelper;
			_targetIterations = targetIterations;
		}

		//public ushort Iterate(FPValues cRs, int rIndex, FPValues cIs, int iIndex)
		//{
		//	var cR = new Smx(cRs.Signs[rIndex], GetMantissa(cRs, rIndex), cRs.Exponents[rIndex], 55);
		//	var cI = new Smx(cIs.Signs[iIndex], GetMantissa(cIs, iIndex), cIs.Exponents[iIndex], 55);

		//	var result = Iterate(cR, cI);

		//	return result;
		//}

		//private ulong[] GetMantissa(FPValues fPValues, int index)
		//{
		//	var numberOfLimbs = fPValues.Mantissas.Length;
		//	var result = new ulong[numberOfLimbs];

		//	for (var i = 0; i < numberOfLimbs; i++)
		//	{
		//		result[i] = fPValues.Mantissas[i][index];
		//	}

		//	return result;
		//}

		public ushort Iterate(Smx cR, Smx cI)
		{
			var result = Iterate(cR, cI, cntr: 0, Smx.Zero, Smx.Zero);
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI)
		{
			var zRSqr = _smxMathHelper.Square(zR);
			var zISqr = _smxMathHelper.Square(zI);

			var sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr);

			while (!_smxMathHelper.IsGreaterOrEqThan(sumOfSqrs, 4) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = _smxMathHelper.Add(zR, zI);

					// square(z.r + z.i)
					var zRZiSqr = _smxMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = _smxMathHelper.Sub(zRZiSqr, zRSqr);
					zI = _smxMathHelper.Sub(zI, zISqr);
					//zI = _smxMathHelper.Add(zI, cI, out cI);
					zI = _smxMathHelper.Add(zI, cI);

					// z.r = zrsqr - zisqr + c.r
					zR = _smxMathHelper.Sub(zRSqr, zISqr);
					//zR = _smxMathHelper.Add(zR, cR, out cR);
					zR = _smxMathHelper.Add(zR, cR);

					zRSqr = _smxMathHelper.Square(zR);
					zISqr = _smxMathHelper.Square(zI);

					sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}

			//if (cntr < _targetIterations)
			//{
			//	var sacResult = _smxMathHelper.SumAndCompare(zRSqr, zISqr, 4);
			//	var sumOfZrSqrAndZiSqr = _smxMathHelper.Add(zRSqr, zISqr);
			//	var rValDiag = sumOfZrSqrAndZiSqr.GetStringValue();
			//	Debug.WriteLine($"Balied out: The value is {rValDiag}. SumAndCompare returned: {sacResult}.");
			//}

			return cntr;
		}

	}
}
