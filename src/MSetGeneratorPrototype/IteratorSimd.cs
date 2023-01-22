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

		private uint _threshold;
		private Vector256<int> _thresholdVector;

		private FP31Vectors _zRSqrs;
		private FP31Vectors _zISqrs;

		private FP31Vectors _sumOfSqrs;

		private Vector256<int>[] _escapedFlagVectors;

		private FP31Vectors _zRZiSqrs;
		private FP31Vectors _zRs2;
		private FP31Vectors _zIs2;

		#endregion

		#region Constructor

		public IteratorSimd(VecMath9 vecMath)
		{
			_vecMath = vecMath;
			_escapedFlagVectors = new Vector256<int>[VectorCount];

			Crs = new FP31Vectors(LimbCount, ValueCount);
			Cis = new FP31Vectors(LimbCount, ValueCount);
			Zrs = new FP31Vectors(LimbCount, ValueCount);
			Zis = new FP31Vectors(LimbCount, ValueCount);

			ZValuesAreZero = true;

			_zRSqrs = new FP31Vectors(LimbCount, ValueCount);
			_zISqrs = new FP31Vectors(LimbCount, ValueCount);
			_sumOfSqrs = new FP31Vectors(LimbCount, ValueCount);

			_zRZiSqrs = new FP31Vectors(LimbCount, ValueCount);
			_zRs2 = new FP31Vectors(LimbCount, ValueCount);
			_zIs2 = new FP31Vectors(LimbCount, ValueCount);
		}

		#endregion

		#region Public Properties

		public int LimbCount => _vecMath.LimbCount;
		public int ValueCount => _vecMath.ValueCount;
		public int VectorCount => _vecMath.VectorCount;

		public FP31Vectors Crs { get; init; }
		public FP31Vectors Cis { get; init; }
		public FP31Vectors Zrs { get; init; }
		public FP31Vectors Zis { get; init; }

		public bool ZValuesAreZero { get; set; }

		public uint Threshold
		{
			get => _threshold;
			set
			{
				if (value != _threshold)
				{
					_threshold = value;
					_thresholdVector = _vecMath.CreateVectorForComparison(_threshold);
				}
			}
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
					//Zrs = Crs.Clone();
					//Zis = Cis.Clone();

					Zrs.UpdateFrom(Crs);
					Zis.UpdateFrom(Cis);
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

				_vecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, _escapedFlagVectors, inPlayList);
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
