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
			var result = Iterate(cR, cI, cntr: 0, _smxMathHelper.CreateNewZeroSmx(), _smxMathHelper.CreateNewZeroSmx(), threshold);
			return result;
		}

		public ushort Iterate(Smx cR, Smx cI, ushort cntr, Smx zR, Smx zI, uint threshold)
		{
			var zRSqr = _smxMathHelper.Square(zR);
			var zISqr = _smxMathHelper.Square(zI);

			var zRSqrSa = _smxMathHelper.Convert(zRSqr);
			var zISqrSa = _smxMathHelper.Convert(zISqr);
			var sumOfSqrsSa = _smxMathHelper.Add(zRSqrSa, zISqrSa, "SumOfSqrs");

			var sumOfSqrs = _smxMathHelper.Convert(sumOfSqrsSa);

			while (!_smxMathHelper.IsGreaterOrEqThan(sumOfSqrs, threshold) && cntr++ < _targetIterations)
			{
				try
				{
					// z.r + z.i
					var zRSa = _smxMathHelper.Convert(zR);
					var zISa = _smxMathHelper.Convert(zI);
					var zRZiSa = _smxMathHelper.Add(zRSa, zISa, "adding zR and zI");

					// square(z.r + z.i)
					var zRZi = _smxMathHelper.Convert(zRZiSa);
					var zRZiSqr = _smxMathHelper.Square(zRZi);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
					var zRZiSqrSa = _smxMathHelper.Convert(zRZiSqr);
					zISa = _smxMathHelper.Sub(zRZiSqrSa, zRSqrSa, "zRZiSqr - zRSqr");
					zISa = _smxMathHelper.Sub(zISa, zISqrSa, "- zISqr");

					var cISa = _smxMathHelper.Convert(cI);
					zISa = _smxMathHelper.Add(zISa, cISa, "adding cI");

					// z.r = zrsqr - zisqr + c.r
					zRSa = _smxMathHelper.Sub(zRSqrSa, zISqrSa, "zRSqr - zISqr");

					var cRSa = _smxMathHelper.Convert(cR);
					zRSa = _smxMathHelper.Add(zRSa, cRSa, "adding cR");

					zR = _smxMathHelper.Convert(zRSa);
					zI = _smxMathHelper.Convert(zISa);
					zRSqr = _smxMathHelper.Square(zR);
					zISqr = _smxMathHelper.Square(zI);

					zRSqrSa = _smxMathHelper.Convert(zRSqr);
					zISqrSa = _smxMathHelper.Convert(zISqr);
					sumOfSqrsSa = _smxMathHelper.Add(zRSqrSa, zISqrSa, "SumOfSqrs");
					sumOfSqrs = _smxMathHelper.Convert(sumOfSqrsSa);

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
