using System.Diagnostics;

namespace MSetGenP
{
	internal ref struct IteratorVector
	{
		#region Private Properties

		private VecMath _smxVecMathHelper;
		private VecMath2 _fPVecMathHelper;

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

		public IteratorVector(VecMath smxVecMathHelper, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
			: this(smxVecMathHelper, GetFpVecHelper(smxVecMathHelper, cRs.Length), cRs, cIs, zRs, zIs, zRSqrs, zISqrs)
		{ }

		public IteratorVector(VecMath2 fPVecMathHelper, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
			: this(GetSmxVecHelper(fPVecMathHelper, cRs.Length), fPVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs)
		{ }

		public IteratorVector(VecMath smxVecMathHelper, VecMath2 fPVecMathHelper, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		{
			_smxVecMathHelper = smxVecMathHelper;
			_fPVecMathHelper = new VecMath2(_smxVecMathHelper.ApFixedPointFormat, cRs.Length, _smxVecMathHelper.Threshold);

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

		private static VecMath GetSmxVecHelper(VecMath2 fPVecMathHelper, int valueCount)
		{
			return new VecMath(fPVecMathHelper.ApFixedPointFormat, valueCount, fPVecMathHelper.Threshold);
		}

		private static VecMath2 GetFpVecHelper(VecMath smxVecMathHelper, int valueCount)
		{
			return new VecMath2(smxVecMathHelper.ApFixedPointFormat, valueCount, smxVecMathHelper.Threshold);
		}

		#endregion

		#region Public Methods

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

		public void IterateSmx2C()
		{
			try
			{
				// z.r + z.i
				_fPVecMathHelper.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				_fPVecMathHelper.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_fPVecMathHelper.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				_fPVecMathHelper.Sub(_zIs, _zISqrs, _zIs2);
				_fPVecMathHelper.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				_fPVecMathHelper.Sub(_zRSqrs, _zISqrs, _zRs2);
				_fPVecMathHelper.Add(_zRs2, _cRs, _zRs);

				_fPVecMathHelper.Square(_zRs, _zRSqrs);
				_fPVecMathHelper.Square(_zIs, _zISqrs);

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
