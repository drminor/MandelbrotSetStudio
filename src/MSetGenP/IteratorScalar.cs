using System.Diagnostics;

namespace MSetGenP
{
	internal ref struct IteratorScalar
	{
		private ScalerMath _smxMathHelper;
		private int _targetIterations;

		private ScalarMath2C _fPMathHelper;

		#region Constructor

		public IteratorScalar(ScalerMath smxMathHelper, int targetIterations)
		{
			_smxMathHelper = smxMathHelper;
			_fPMathHelper = new ScalarMath2C(_smxMathHelper.ApFixedPointFormat, _smxMathHelper.Threshold);

			_targetIterations = targetIterations;
		}

		#endregion

		#region Public Properties

		public long NumberOfSplits => _fPMathHelper.NumberOfSplits;
		public long NumberOfGetCarries => _fPMathHelper.NumberOfGetCarries;

		public long NumberOfIsGrtrOpsFP => _fPMathHelper.NumberOfGrtrThanOps;
		public long NumberOfIsGrtrOpsSc => _smxMathHelper.NumberOfGrtrThanOps;

		#endregion

		#region Iterate - Smx

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

		#endregion

		#region Iterate - Smx2C

		public ushort IterateSmx2C(Smx cR, Smx cI)
		{
			var cRn = _fPMathHelper.Convert(cR);
			var cIn = _fPMathHelper.Convert(cI);

			var result = IterateSmxC2(cRn, cIn, cntr: 0, _fPMathHelper.CreateNewZeroSmx2C(cRn.Precision), _fPMathHelper.CreateNewZeroSmx2C(cR.Precision));
			return result;
		}

		public ushort IterateSmxC2(Smx2C cR, Smx2C cI, ushort cntr, Smx2C zR, Smx2C zI)
		{
			var zRSqr = _fPMathHelper.Square(zR);
			var zISqr = _fPMathHelper.Square(zI);
			var sumOfSqrs = _fPMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");

			while (!_fPMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = _fPMathHelper.Add(zR, zI, "adding zR and zI");

					// square(z.r + z.i)
					var zRZiSqr = _fPMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = _fPMathHelper.Sub(zRZiSqr, zRSqr, "zRZiSqr - zRSqr");
					zI = _fPMathHelper.Sub(zI, zISqr, "- zISqr");
					zI = _fPMathHelper.Add(zI, cI, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zR = _fPMathHelper.Sub(zRSqr, zISqr, "zRSqr - zISqr");
					zR = _fPMathHelper.Add(zR, cR, "adding cR");

					zRSqr = _fPMathHelper.Square(zR);
					zISqr = _fPMathHelper.Square(zI);

					sumOfSqrs = _fPMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}

			return cntr;
		}

		#endregion

	}
}
