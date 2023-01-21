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

		private FP31Vectors _zRSqrs;
		private FP31Vectors _zISqrs;

		private FP31Vectors _sumOfSqrs;

		private Vector256<int>[] _escapedFlagVectors;

		private FP31Vectors _zRZiSqrs;
		private FP31Vectors _zRs2;
		private FP31Vectors _zIs2;

		#endregion

		#region Constructor

		public IteratorSimd(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			_vecMath = new VecMath9(apFixedPointFormat, valueCount, threshold);

			var limbCount = _vecMath.LimbCount;
			var vectorCount = _vecMath.VectorCount;

			Crs = new FP31Vectors(limbCount, valueCount);
			Cis = new FP31Vectors(limbCount, valueCount);
			Zrs = new FP31Vectors(limbCount, valueCount);
			Zis = new FP31Vectors(limbCount, valueCount);

			ZValuesAreZero = true;

			_zRSqrs = new FP31Vectors(limbCount, valueCount);
			_zISqrs = new FP31Vectors(limbCount, valueCount);
			_sumOfSqrs = new FP31Vectors(limbCount, valueCount);

			_escapedFlagVectors = new Vector256<int>[vectorCount];

			_zRZiSqrs = new FP31Vectors(limbCount, valueCount);
			_zRs2 = new FP31Vectors(limbCount, valueCount);
			_zIs2 = new FP31Vectors(limbCount, valueCount);
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat => _vecMath.ApFixedPointFormat;

		public FP31Vectors Crs { get; set; }
		public FP31Vectors Cis { get; set; }
		public FP31Vectors Zrs { get; set; }
		public FP31Vectors Zis { get; set; }

		public bool ZValuesAreZero { get; set; }

		public uint Threshold
		{
			get => _vecMath.Threshold;
			set => _vecMath.Threshold = value;
		}

		//public MathOpCounts MathOpCounts => _vecMath.MathOpCounts;

		#endregion

		#region Public Methods

		//public void SetCoords(FP31Deck cRs, FP31Deck cIs, FP31Deck zRs, FP31Deck zIs)
		//{
		//	throw new NotImplementedException();
		//	//_cRs = cRs;
		//	//_cIs = cIs;
		//	//_zRs = zRs;
		//	//_zIs = zIs;

		//	//_zValuesAreZero = zRs.IsZero || zIs.IsZero;

		//	//if (_zValuesAreZero)
		//	//{
		//	//	Debug.Assert(zRs.IsZero && zIs.IsZero, "One of zRs or zIs is zero, but both zRs and zIs are not zero.");
		//	//}
		//}

		//public void SetCoords(FP31Val[] samplePointsX, FP31Val samplePointY)
		//{
		//	//_cRs = cRs;
		//	_cRsTemp.UpdateFrom(samplePointsX);
		//	Crs = new FP31Vectors(_cRsTemp);
			
		//	//_cIs = cIs;
		//	_cIsTemp.UpdateFrom(samplePointY);
		//	Cis = new FP31Vectors(_cIsTemp);

		//	//Zrs = zRs;
		//	//_zIs = zIs;

		//	Zrs.ClearManatissMems();
		//	Zis.ClearManatissMems();

		//	ZValuesAreZero = true;
		//}

		public Vector256<int>[] Iterate(int[] inPlayList, int[] inPlayListNarrow)
		{
			try
			{
				if (ZValuesAreZero)
				{
					// Perform the first iteration. 
					Zrs = Crs.Clone();
					Zis = Cis.Clone();
					ZValuesAreZero = false;
				}
				else
				{
					// square(z.r + z.i)
					_vecMath.AddThenSquare(Zrs, Zis, _zRZiSqrs, inPlayList, inPlayListNarrow);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
					_vecMath.Sub(_zRZiSqrs, _zRSqrs, Zis, inPlayList);
					_vecMath.Sub(Zis, _zISqrs, _zIs2, inPlayList);
					_vecMath.Add(_zIs2, Cis, Zis, inPlayList);

					// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
					_vecMath.Sub(_zRSqrs, _zISqrs, _zRs2, inPlayList);
					_vecMath.Add(_zRs2, Crs, Zrs, inPlayList);
				}

				_vecMath.Square(Zrs, _zRSqrs, inPlayList, inPlayListNarrow);
				_vecMath.Square(Zis, _zISqrs, inPlayList, inPlayListNarrow);
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
