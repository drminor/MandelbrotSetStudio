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

		public ushort Iterate(Smx cR, Smx cI, uint threshold)
		{
			var result = Iterate(cR, cI, cntr: 0, _smxMathHelper.CreateNewZeroSmx(cR.Precision), _smxMathHelper.CreateNewZeroSmx(cI.Precision), threshold);
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI, uint threshold)
		{
			var zRSqr = _smxMathHelper.Square(zR);
			var zISqr = _smxMathHelper.Square(zI);

			//var zRSqrSa = _smxMathHelper.Convert(zRSqr);
			//var zISqrSa = _smxMathHelper.Convert(zISqr);
			var sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");

			//var sumOfSqrs = _smxMathHelper.Convert(sumOfSqrsSa);

			while (!_smxMathHelper.IsGreaterOrEqThan(sumOfSqrs, threshold) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					//var zRSa = _smxMathHelper.Convert(zR);
					//var zISa = _smxMathHelper.Convert(zI);
					var zRZi = _smxMathHelper.Add(zR, zI, "adding zR and zI");

					// square(z.r + z.i)
					//var zRZi = _smxMathHelper.Convert(zRZiSa);
					var zRZiSqr = _smxMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					//var zRZiSqrSa = _smxMathHelper.Convert(zRZiSqr);
					zI = _smxMathHelper.Sub(zRZiSqr, zRSqr, "zRZiSqr - zRSqr");
					zI = _smxMathHelper.Sub(zI, zISqr, "- zISqr");

					//var cISa = _smxMathHelper.Convert(cI);
					zI = _smxMathHelper.Add(zI, cI, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zR = _smxMathHelper.Sub(zRSqr, zISqr, "zRSqr - zISqr");

					//var cRSa = _smxMathHelper.Convert(cR);
					zR = _smxMathHelper.Add(zR, cR, "adding cR");

					//zR = _smxMathHelper.Convert(zRSa);
					//zI = _smxMathHelper.Convert(zISa);
					zRSqr = _smxMathHelper.Square(zR);
					zISqr = _smxMathHelper.Square(zI);

					//zRSqrSa = _smxMathHelper.Convert(zRSqr);
					//zISqrSa = _smxMathHelper.Convert(zISqr);
					sumOfSqrs = _smxMathHelper.Add(zRSqr, zISqr, "SumOfSqrs");
					//sumOfSqrs = _smxMathHelper.Convert(sumOfSqrsSa);

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
