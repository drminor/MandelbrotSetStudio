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

		public ushort Iterate(Smx cR, Smx cI, uint threshold)
		{
			var result = Iterate(cR, cI, cntr: 0, _smxMathHelper.CreateNewZeroSmx(cR.Precision), _smxMathHelper.CreateNewZeroSmx(cI.Precision), threshold);
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI, uint threshold)
		{
			var zRSqr = _smxMathHelper.Square(zR);
			var zISqr = _smxMathHelper.Square(zI);
			var sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");

			while (!_smxMathHelper.IsGreaterOrEqThan(sumOfSqrs, threshold) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = _smxMathHelper.Add(zR, zI, "adding zR and zI");

					// square(z.r + z.i)
					var zRZiSqr = _smxMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = _smxMathHelper.Sub(zRZiSqr, zRSqr, "zRZiSqr - zRSqr");
					zI = _smxMathHelper.Sub(zI, zISqr, "- zISqr");
					zI = _smxMathHelper.Add(zI, cI, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zR = _smxMathHelper.Sub(zRSqr, zISqr, "zRSqr - zISqr");
					zR = _smxMathHelper.Add(zR, cR, "adding cR");

					zRSqr = _smxMathHelper.Square(zR);
					zISqr = _smxMathHelper.Square(zI);

					sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}

			//if (cntr < _targetIterations)
			//{
			//	var sacResult = _smxMathHelper.IsGreaterOrEqThan(sumOfSqrs, threshold);
			//	var rValDiag = sumOfSqrs.GetStringValue();
			//	Debug.WriteLine($"Bailed out: The value is {rValDiag}. Compare returned: {sacResult}.");
			//}
			//else
			//{
			//	var sacResult = _smxMathHelper.IsGreaterOrEqThan(sumOfSqrs, threshold);
			//	var rValDiag = sumOfSqrs.GetStringValue();
			//	Debug.WriteLine($"Target reached: The value is {rValDiag}. Compare returned: {sacResult}.");
			//}

			return cntr;
		}

	}
}
