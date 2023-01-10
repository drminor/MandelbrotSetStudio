using MSS.Common.APValues;
using System;
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

		public void SetCoords(FP31Deck cRs, FP31Deck cIs)
		{
			_cRs = cRs;
			_cIs = cIs;

			_zValuesAreZero = true;
		}

		public void SetCoords(FP31Deck cRs, FP31Deck cIs, FP31Deck zRs, FP31Deck zIs)
		{
			_cRs = cRs;
			_cIs = cIs;
			_zRs = zRs;
			_zIs = zIs;

			_zValuesAreZero = false;
		}

		#endregion

		#region Public Properties

		public bool[] DoneFlags => _vecMath.DoneFlags;
		public int[] InPlayList => _vecMath.InPlayList;
		public long[] UnusedCalcs => _vecMath.UnusedCalcs;

		#endregion

		#region Public Methods

		public int[] Iterate(VecMath9 vecMath)
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
					vecMath.AddThenSquare(_zRs, _zIs, _zRZiSqrs);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
					vecMath.Sub(_zRZiSqrs, _zRSqrs, _zIs);
					vecMath.Sub(_zIs, _zISqrs, _zIs2);
					vecMath.Add(_zIs2, _cIs, _zIs);

					// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
					vecMath.Sub(_zRSqrs, _zISqrs, _zRs2);
					vecMath.Add(_zRs2, _cRs, _zRs);
				}

				vecMath.Square(_zRs, _zRSqrs);
				vecMath.Square(_zIs, _zISqrs);
				vecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

				vecMath.IsGreaterOrEqThanThreshold(_sumOfSqrs, _escapedFlagMemory);

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
