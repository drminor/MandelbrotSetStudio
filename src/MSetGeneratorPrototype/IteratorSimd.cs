using MSS.Common.APValues;
using System;
using System.Diagnostics;

namespace MSetGeneratorPrototype
{
	internal ref struct IteratorSimd
	{
		#region Private Properties

		private FP31Deck _cRs;
		private FP31Deck _cIs;
		private FP31Deck _zRs;
		private FP31Deck _zIs;
		private FP31Deck _zRSqrs;
		private FP31Deck _zISqrs;

		private FP31Deck _zRZIs;
		private FP31Deck _zRZiSqrs;

		private FP31Deck _zRs2;
		private FP31Deck _zIs2;

		#endregion

		#region Constructor

		public IteratorSimd(FP31Deck cRs, FP31Deck cIs, FP31Deck zRs, FP31Deck zIs, FP31Deck zRSqrs, FP31Deck zISqrs)
		{
			_cRs = cRs;
			_cIs = cIs;
			_zRs = zRs;
			_zIs = zIs;
			_zRSqrs = zRSqrs;
			_zISqrs = zISqrs;

			_zRZIs = new FP31Deck(_cRs.LimbCount, _cRs.Length);
			_zRZiSqrs = new FP31Deck(_cRs.LimbCount, _cRs.Length);

			_zRs2 = new FP31Deck(_cRs.LimbCount, _cRs.Length);
			_zIs2 = new FP31Deck(_cRs.LimbCount, _cRs.Length);
		}

		#endregion

		#region Public Methods

		public void Iterate(VecMath9 vecMath)
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
