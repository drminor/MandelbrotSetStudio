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

		public ushort Iterate(Smx cR, Smx cI)
		{
			var result = Iterate(cR, cI, cntr: 0, Smx.Zero, Smx.Zero);
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI)
		{
			var zRSqr = SmxMathHelper.Square(zR);
			var zISqr = SmxMathHelper.Square(zI);

			// z.r + z.i
			var zRZi = SmxMathHelper.Add(zR, zI);

			while (SmxMathHelper.SumAndCompare(zRSqr, zISqr, 4) < 1 && cntr++ < _targetIterations)
			{
				// square(z.r + z.i)
				var zRZiSqr = SmxMathHelper.Square(zRZi);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				zI = SmxMathHelper.Sub(zRZiSqr, zRSqr);
				zI = SmxMathHelper.Sub(zI, zISqr);
				zI = SmxMathHelper.Add(zI, cI);

				// z.r = zrsqr - zisqr + c.r
				zR = SmxMathHelper.Sub(zRSqr, zISqr);
				zR = SmxMathHelper.Add(zR, cR);

				// z.r + z.i
				zRZi = SmxMathHelper.Add(zR, zI);

				zRSqr = SmxMathHelper.Square(zR);
				zISqr = SmxMathHelper.Square(zI);
			}

			if (cntr < _targetIterations)
			{
				var sacResult = SmxMathHelper.SumAndCompare(zRSqr, zISqr, 4);

				var sumOfZrSqrAndZiSqr = SmxMathHelper.Add(zRSqr, zISqr);

				var rValDiag = sumOfZrSqrAndZiSqr.GetStringValue();

				Debug.WriteLine($"Balied out: The value is {rValDiag}. SumAndCompare returned: {sacResult}.");
			}

			return cntr;
		}

	}
}
