using System.Diagnostics;

namespace MSetGenP
{
	internal ref struct IteratorScalar
	{
		private SmxMathHelper _smxMathHelper;
		private int _targetIterations;

		public IteratorScalar(SmxMathHelper smxMathHelper, int targetIterations)
		{
			_smxMathHelper = smxMathHelper;
			_targetIterations = targetIterations;
		}

		public ushort Iterate(Smx cR, Smx cI)
		{
			var result = Iterate(cR, cI, cntr: 0, _smxMathHelper.CreateNewZeroSmx(cR.Precision), _smxMathHelper.CreateNewZeroSmx(cI.Precision));
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI)
		{
			var zRSqr = _smxMathHelper.Square(zR);
			var zISqr = _smxMathHelper.Square(zI);
			var sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");

			while (!_smxMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs) && cntr++ < _targetIterations)
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


		public ushort IterateSmxC2(Smx cR, Smx cI)
		{
			var fPMathHelper = new FPMathHelper(_smxMathHelper.ApFixedPointFormat, _smxMathHelper.Threshold);

			var cRn = fPMathHelper.Convert(cR);
			var cIn = fPMathHelper.Convert(cI);

			var result = IterateSmxC2(fPMathHelper, cRn, cIn, cntr: 0, fPMathHelper.CreateNewZeroSmx2C(cRn.Precision), fPMathHelper.CreateNewZeroSmx2C(cR.Precision));
			return result;
		}

		public ushort IterateSmxC2(FPMathHelper fPMathHelper, Smx2C cR, Smx2C cI, ushort cntr, Smx2C zR, Smx2C zI)
		{
			var zRSqr = fPMathHelper.Square(zR);
			var zISqr = fPMathHelper.Square(zI);
			var sumOfSqrs = fPMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");

			while (!fPMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = fPMathHelper.Add(zR, zI, "adding zR and zI");

					// square(z.r + z.i)
					var zRZiSqr = fPMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = fPMathHelper.Sub(zRZiSqr, zRSqr, "zRZiSqr - zRSqr");
					zI = fPMathHelper.Sub(zI, zISqr, "- zISqr");
					zI = fPMathHelper.Add(zI, cI, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zR = fPMathHelper.Sub(zRSqr, zISqr, "zRSqr - zISqr");
					zR = fPMathHelper.Add(zR, cR, "adding cR");

					zRSqr = fPMathHelper.Square(zR);
					zISqr = fPMathHelper.Square(zI);

					sumOfSqrs = fPMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}

			return cntr;
		}


	}
}
