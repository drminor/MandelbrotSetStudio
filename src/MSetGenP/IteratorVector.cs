using System.Diagnostics;

namespace MSetGenP
{
	public class IteratorVector
	{
		private SmxVecMathHelper _smxVecMathHelper;

		private FPValues _cRs;
		private FPValues _cIs;
		private FPValues _zRs;
		private FPValues _zIs;
		private FPValues _zRSqrs;
		private FPValues _zISqrs;

		private FPValues _zRZIs;
		private FPValues _zRZiSqrs;

		private FPValues _zRs2;
		private FPValues _zIs2;

		public IteratorVector(SmxVecMathHelper smxVecMathHelper, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		{
			_smxVecMathHelper = smxVecMathHelper;

			_cRs = cRs;
			_cIs = cIs;
			_zRs = zRs;
			_zIs = zIs;
			_zRSqrs = zRSqrs;
			_zISqrs = zISqrs;

			_zRZIs = new FPValues(_cRs.LimbCount, _cRs.Length);
			_zRZiSqrs = new FPValues(_cRs.LimbCount, _cRs.Length);

			_zRs2 = new FPValues(_cRs.LimbCount, _cRs.Length);
			_zIs2 = new FPValues(_cRs.LimbCount, _cRs.Length);
		}

		public void Iterate()
		{
			try
			{
				// z.r + z.i
				_smxVecMathHelper.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				_smxVecMathHelper.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_smxVecMathHelper.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				_smxVecMathHelper.Sub(_zIs, _zISqrs, _zIs2);
				_smxVecMathHelper.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				_smxVecMathHelper.Sub(_zRSqrs, _zISqrs, _zRs2);
				_smxVecMathHelper.Add(_zRs2, _cRs, _zRs);

				_smxVecMathHelper.Square(_zRs, _zRSqrs);
				_smxVecMathHelper.Square(_zIs, _zISqrs);

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
