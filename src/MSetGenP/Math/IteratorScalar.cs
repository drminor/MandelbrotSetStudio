using MSS.Common.APValues;
using System;
using System.Diagnostics;

namespace MSetGenP
{
	internal ref struct IteratorScalar
	{
		private ScalarMath _scalarMath;
		private ScalarMath2C _scalarMath2C;

		private int _targetIterations;

		#region Constructors

		public IteratorScalar(ScalarMath scalarMath, int targetIterations)
		{
			_scalarMath = scalarMath;
			_scalarMath2C = new ScalarMath2C(_scalarMath.ApFixedPointFormat, _scalarMath.Threshold);

			_targetIterations = targetIterations;
		}

		#endregion

		#region Public Properties

		public long NumberOfSplits => _scalarMath2C.NumberOfSplits;
		public long NumberOfGetCarries => _scalarMath2C.NumberOfGetCarries;

		public long NumberOfIsGrtrOpsFP => _scalarMath2C.NumberOfGrtrThanOps;
		public long NumberOfIsGrtrOpsSc => _scalarMath.NumberOfGrtrThanOps;

		#endregion

		#region Iterate - Smx

		public ushort Iterate(Smx cR, Smx cI)
		{
			var result = Iterate(cR, cI, cntr: 0, _scalarMath.CreateNewZeroSmx(cR.Precision), _scalarMath.CreateNewZeroSmx(cI.Precision));
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI)
		{
			var zRSqr = _scalarMath.Square(zR);
			var zISqr = _scalarMath.Square(zI);
			var sumOfSqrs = _scalarMath.Add(zRSqr, zISqr, "SumOfSqrs");

			while (!_scalarMath.IsGreaterOrEqThanThreshold(sumOfSqrs) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = _scalarMath.Add(zR, zI, "adding zR and zI");

					// square(z.r + z.i)
					var zRZiSqr = _scalarMath.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = _scalarMath.Sub(zRZiSqr, zRSqr, "zRZiSqr - zRSqr");
					zI = _scalarMath.Sub(zI, zISqr, "- zISqr");
					zI = _scalarMath.Add(zI, cI, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zR = _scalarMath.Sub(zRSqr, zISqr, "zRSqr - zISqr");
					zR = _scalarMath.Add(zR, cR, "adding cR");

					zRSqr = _scalarMath.Square(zR);
					zISqr = _scalarMath.Square(zI);

					sumOfSqrs = _scalarMath.Add(zRSqr, zISqr, "SumOfSqrs");
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
			var cRn = _scalarMath2C.Convert(cR);
			var cIn = _scalarMath2C.Convert(cI);

			var zR = _scalarMath2C.CreateNewZeroSmx2C(cRn.Precision);
			var zI = _scalarMath2C.CreateNewZeroSmx2C(cIn.Precision);


			var result = IterateSmxC2(cRn, cIn, cntr: 0, zR, zI);
			return result;
		}

		public ushort IterateSmxC2(Smx2C cR, Smx2C cI, ushort cntr, Smx2C zR, Smx2C zI)
		{
			var zRSqr = _scalarMath2C.Square(zR);
			var zISqr = _scalarMath2C.Square(zI);
			var sumOfSqrs = _scalarMath2C.Add(zRSqr, zISqr, "SumOfSqrs");

			while (!_scalarMath2C.IsGreaterOrEqThanThreshold(sumOfSqrs) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRZi = _scalarMath2C.Add(zR, zI, "adding zR and zI");

					// square(z.r + z.i)
					var zRZiSqr = _scalarMath2C.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					zI = _scalarMath2C.Sub(zRZiSqr, zRSqr, "zRZiSqr - zRSqr");
					zI = _scalarMath2C.Sub(zI, zISqr, "- zISqr");
					zI = _scalarMath2C.Add(zI, cI, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zR = _scalarMath2C.Sub(zRSqr, zISqr, "zRSqr - zISqr");
					zR = _scalarMath2C.Add(zR, cR, "adding cR");

					zRSqr = _scalarMath2C.Square(zR);
					zISqr = _scalarMath2C.Square(zI);

					sumOfSqrs = _scalarMath2C.Add(zRSqr, zISqr, "SumOfSqrs");
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
