using System.Diagnostics;

namespace MSetGenP
{
	internal ref struct IteratorVector
	{
		#region Private Properties

		private VecMath _vecMath;
		private VecMath2C _vecMath2C;

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

		#endregion

		#region Constructors

		public IteratorVector(VecMath unsignedVecMath, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
			: this(unsignedVecMath, GetSignedVecMath(unsignedVecMath, cRs.Length), cRs, cIs, zRs, zIs, zRSqrs, zISqrs)
		{ }

		public IteratorVector(VecMath2C signedVecMath, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
			: this(GetUnsignedVecMath(signedVecMath, cRs.Length), signedVecMath, cRs, cIs, zRs, zIs, zRSqrs, zISqrs)
		{ }

		public IteratorVector(VecMath unsignedVecMath, VecMath2C signedVecMath, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		{
			_vecMath = unsignedVecMath;
			_vecMath2C = signedVecMath;

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

		private static VecMath GetUnsignedVecMath(VecMath2C fPVecMathHelper, int valueCount)
		{
			return new VecMath(fPVecMathHelper.ApFixedPointFormat, valueCount, fPVecMathHelper.Threshold);
		}

		private static VecMath2C GetSignedVecMath(VecMath smxVecMathHelper, int valueCount)
		{
			return new VecMath2C(smxVecMathHelper.ApFixedPointFormat, valueCount, smxVecMathHelper.Threshold);
		}

		#endregion

		#region Public Methods

		public void Iterate()
		{
			try
			{
				// z.r + z.i
				_vecMath.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				_vecMath.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_vecMath.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				_vecMath.Sub(_zIs, _zISqrs, _zIs2);
				_vecMath.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				_vecMath.Sub(_zRSqrs, _zISqrs, _zRs2);
				_vecMath.Add(_zRs2, _cRs, _zRs);

				_vecMath.Square(_zRs, _zRSqrs);
				_vecMath.Square(_zIs, _zISqrs);

				//sumOfSqrs = _smxVecMathHelper.Add(zRSqrs, zISqrs);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		public void IterateSmx2C()
		{
			try
			{
				// z.r + z.i
				_vecMath2C.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				_vecMath2C.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_vecMath2C.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				_vecMath2C.Sub(_zIs, _zISqrs, _zIs2);
				_vecMath2C.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				_vecMath2C.Sub(_zRSqrs, _zISqrs, _zRs2);
				_vecMath2C.Add(_zRs2, _cRs, _zRs);

				_vecMath2C.Square(_zRs, _zRSqrs);
				_vecMath2C.Square(_zIs, _zISqrs);

				//sumOfSqrs = _smxVecMathHelper.Add(zRSqrs, zISqrs);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		#endregion
	}
}
