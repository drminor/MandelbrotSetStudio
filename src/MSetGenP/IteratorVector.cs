using System.Diagnostics;
using System.Numerics;

namespace MSetGenP
{
	public class IteratorVector
	{
		private SmxVecMathHelper _smxVecMathHelper;

		public IteratorVector(SmxVecMathHelper smxVecMathHelper)
		{
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

	}
}
