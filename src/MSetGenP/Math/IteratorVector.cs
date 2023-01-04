using System.Diagnostics;

namespace MSetGenP
{
	internal ref struct IteratorVector
	{
		#region Private Properties

		//private VecMath _vecMath;
		//private VecMath2C _vecMath2C;

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

		//public IteratorVector(VecMath vecMath, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		//	: this(vecMath, GetVecMath2C(vecMath, cRs.Length), cRs, cIs, zRs, zIs, zRSqrs, zISqrs)
		//{ }

		//public IteratorVector(VecMath2C vecMath2C, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		//	: this(GetVecMath(vecMath2C, cRs.Length), vecMath2C, cRs, cIs, zRs, zIs, zRSqrs, zISqrs)
		//{ }

		//public IteratorVector(ApFixedPointFormat fpFormat, int valueCount, uint threshold, FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		//{
		//	//_vecMath = new VecMath(fpFormat, valueCount, threshold);
		//	//_vecMath2C = new VecMath2C(fpFormat, valueCount, threshold);

		//	_cRs = cRs;
		//	_cIs = cIs;
		//	_zRs = zRs;
		//	_zIs = zIs;
		//	_zRSqrs = zRSqrs;
		//	_zISqrs = zISqrs;

		//	_zRZIs = new FPValues(_cRs.LimbCount, _cRs.Length);
		//	_zRZiSqrs = new FPValues(_cRs.LimbCount, _cRs.Length);

		//	_zRs2 = new FPValues(_cRs.LimbCount, _cRs.Length);
		//	_zIs2 = new FPValues(_cRs.LimbCount, _cRs.Length);
		//}

		public IteratorVector(FPValues cRs, FPValues cIs, FPValues zRs, FPValues zIs, FPValues zRSqrs, FPValues zISqrs)
		{
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


		//private static VecMath GetVecMath(VecMath2C vecMath2C, int valueCount)
		//{
		//	return new VecMath(vecMath2C.ApFixedPointFormat, valueCount, vecMath2C.Threshold);
		//}

		//private static VecMath2C GetVecMath2C(VecMath vecMath, int valueCount)
		//{
		//	return new VecMath2C(vecMath.ApFixedPointFormat, valueCount, vecMath.Threshold);
		//}

		#endregion

		#region Public Methods

		public void IterateSmx(VecMath vecMath)
		{
			try
			{
				// z.r + z.i
				vecMath.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				vecMath.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				vecMath.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				vecMath.Sub(_zIs, _zISqrs, _zIs2);
				vecMath.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				vecMath.Sub(_zRSqrs, _zISqrs, _zRs2);
				vecMath.Add(_zRs2, _cRs, _zRs);

				vecMath.Square(_zRs, _zRSqrs);
				vecMath.Square(_zIs, _zISqrs);

				//sumOfSqrs = _smxVecMathHelper.Add(zRSqrs, zISqrs);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		public void IterateSmx2C(VecMath2C vecMath2C)
		{
			try
			{
				// z.r + z.i
				vecMath2C.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				vecMath2C.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				vecMath2C.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				vecMath2C.Sub(_zIs, _zISqrs, _zIs2);
				vecMath2C.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				vecMath2C.Sub(_zRSqrs, _zISqrs, _zRs2);
				vecMath2C.Add(_zRs2, _cRs, _zRs);

				vecMath2C.Square(_zRs, _zRSqrs);
				vecMath2C.Square(_zIs, _zISqrs);

				//sumOfSqrs = _smxVecMathHelper.Add(zRSqrs, zISqrs);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		public void Iterate(IVecMath vecMath)
		{
			try
			{
				// z.r + z.i
				vecMath.Add(_zRs, _zIs, _zRZIs);

				// square(z.r + z.i)
				vecMath.Square(_zRZIs, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				vecMath.Sub(_zRZiSqrs, _zRSqrs, _zIs);
				vecMath.Sub(_zIs, _zISqrs, _zIs2);
				vecMath.Add(_zIs2, _cIs, _zIs);

				// z.r = zrsqr - zisqr + c.r
				vecMath.Sub(_zRSqrs, _zISqrs, _zRs2);
				vecMath.Add(_zRs2, _cRs, _zRs);

				vecMath.Square(_zRs, _zRSqrs);
				vecMath.Square(_zIs, _zISqrs);

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
