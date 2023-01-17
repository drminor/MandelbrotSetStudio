using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorSimd : IIterator
	{
		#region Private Properties

		private VecMath9 _vecMath;

		private FP31Deck _cRs;
		private FP31Deck _cIs;
		private FP31Deck _zRs;
		private FP31Deck _zIs;
		private bool _zValuesAreZero;

		private FP31Deck _zRSqrs;
		private FP31Deck _zISqrs;

		private FP31Deck _sumOfSqrs;

		private Vector256<int>[] _escapedFlagVectors;

		private FP31Deck _zRZiSqrs;
		private FP31Deck _zRs2;
		private FP31Deck _zIs2;

		#endregion

		#region Constructor

		public IteratorSimd(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			_vecMath = new VecMath9(apFixedPointFormat, valueCount, threshold);

			var limbCount = _vecMath.LimbCount;
			var vectorCount = _vecMath.VectorCount;

			_cRs = new FP31Deck(limbCount, valueCount);
			_cIs = new FP31Deck(limbCount, valueCount);
			_zRs = new FP31Deck(limbCount, valueCount);
			_zIs = new FP31Deck(limbCount, valueCount);
			_zValuesAreZero = true;


			_zRSqrs = new FP31Deck(limbCount, valueCount);
			_zISqrs = new FP31Deck(limbCount, valueCount);
			_sumOfSqrs = new FP31Deck(limbCount, valueCount);

			_escapedFlagVectors = new Vector256<int>[vectorCount];

			_zRZiSqrs = new FP31Deck(limbCount, valueCount);
			_zRs2 = new FP31Deck(limbCount, valueCount);
			_zIs2 = new FP31Deck(limbCount, valueCount);
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat => _vecMath.ApFixedPointFormat;

		public uint Threshold
		{
			get => _vecMath.Threshold;
			set => _vecMath.Threshold = value;
		}

		public MathOpCounts MathOpCounts => _vecMath.MathOpCounts;

		#endregion

		#region Public Methods

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

		public void SetCoords(FP31Val[] samplePointsX, FP31Val samplePointY)
		{
			//_cRs = cRs;
			_cRs.UpdateFrom(samplePointsX);
			
			//_cIs = cIs;
			_cIs.UpdateFrom(samplePointY);

			//_zRs = zRs;
			//_zIs = zIs;

			_zRs.ClearManatissMems();
			_zIs.ClearManatissMems();

			_zValuesAreZero = true;
		}


		public Vector256<int>[] Iterate(int[] inPlayList)
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

				_vecMath.IsGreaterOrEqThanThreshold(_sumOfSqrs, _escapedFlagVectors, inPlayList);

				return _escapedFlagVectors;
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
