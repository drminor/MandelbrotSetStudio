using MSS.Common;
using MSS.Common.APValues;
using System.Diagnostics;

namespace MSetGeneratorPrototype
{
	internal ref struct IteratorSimd
	{
		#region Private Properties

		private VecMath9 _vecMath;
		private bool _zValuesAreZero;

		private FP31Deck _cRs;
		private FP31Deck _cIs;
		private FP31Deck _zRs;
		private FP31Deck _zIs;

		private FP31Deck _zRSqrs;
		private FP31Deck _zISqrs;

		private FP31Deck _sumOfSqrs;

		private int[] _escapedFlagsBackingArray;
		private Memory<int> _escapedFlagMemory;

		private FP31Deck _zRZiSqrs;
		private FP31Deck _zRs2;
		private FP31Deck _zIs2;

		#endregion

		#region Constructor

		public IteratorSimd(VecMath9 vecMath)
		{
			_vecMath = vecMath;
			_zValuesAreZero = true;

			var limbCount = vecMath.LimbCount;
			var valueCount = vecMath.ValueCount;

			_cRs = new FP31Deck(limbCount, valueCount);
			_cIs = new FP31Deck(limbCount, valueCount);
			_zRs = new FP31Deck(limbCount, valueCount);
			_zIs = new FP31Deck(limbCount, valueCount);

			_zRSqrs = new FP31Deck(limbCount, valueCount);
			_zISqrs = new FP31Deck(limbCount, valueCount);
			_sumOfSqrs = new FP31Deck(limbCount, valueCount);

			_escapedFlagsBackingArray = new int[valueCount];
			_escapedFlagMemory = new Memory<int>(_escapedFlagsBackingArray);

			_zRZiSqrs = new FP31Deck(limbCount, valueCount);
			_zRs2 = new FP31Deck(limbCount, valueCount);
			_zIs2 = new FP31Deck(limbCount, valueCount);
		}

		public ApFixedPointFormat ApFixedPointFormat => _vecMath.ApFixedPointFormat;


		#region Public Methods

		//public void SetCoords(FP31Deck cRs, FP31Deck cIs)
		//{
		//	_cRs = cRs;
		//	_cIs = cIs;

		//	_zValuesAreZero = true;
		//}

		public void SetCoords(FP31Deck cRs, FP31Deck cIs, FP31Deck zRs, FP31Deck zIs)
		{
			_cRs = cRs;
			_cIs = cIs;
			_zRs = zRs;
			_zIs = zIs;

			_zValuesAreZero = zRs.IsZero || zIs.IsZero;

			if (_zValuesAreZero)
			{
				Debug.Assert(zRs.IsZero && zIs.IsZero, "One of zRs or zIs is zero, but both zRs and zIs are not zero.");
			}
		}

		#endregion

		public int[] Iterate(int[] inPlayList, out FP31Deck sumOfSquares)
		{
			try
			{
				if (_zValuesAreZero)
				{
					// Perform the first iteration. 
					_zRs = _cRs.Clone();
					_zIs = _cIs.Clone();
					_zValuesAreZero = false;
				}
				else
				{
					// square(z.r + z.i)
					_vecMath.AddThenSquare(_zRs, _zIs, _zRZiSqrs, inPlayList);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
					_vecMath.Sub(_zRZiSqrs, _zRSqrs, _zIs, inPlayList);
					_vecMath.Sub(_zIs, _zISqrs, _zIs2, inPlayList);
					_vecMath.Add(_zIs2, _cIs, _zIs, inPlayList);

					// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
					_vecMath.Sub(_zRSqrs, _zISqrs, _zRs2, inPlayList);
					_vecMath.Add(_zRs2, _cRs, _zRs, inPlayList);
				}

				_vecMath.Square(_zRs, _zRSqrs, inPlayList);
				_vecMath.Square(_zIs, _zISqrs, inPlayList);
				_vecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs, inPlayList);

				_vecMath.IsGreaterOrEqThanThreshold(_sumOfSqrs, _escapedFlagMemory, inPlayList);

				sumOfSquares = _sumOfSqrs;
				return _escapedFlagsBackingArray;
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
