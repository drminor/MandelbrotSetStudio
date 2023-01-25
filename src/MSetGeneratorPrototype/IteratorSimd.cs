using MSS.Common.APValues;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorSimd //: IIterator
	{
		#region Private Properties

		private FP31VectorsMath _fp31VectorsMath;

		private uint _threshold;
		private Vector256<int> _thresholdVector;
		private Vector256<int>[] _escapedFlagVectors;

		private FP31Vectors _zRSqrs;
		private FP31Vectors _zISqrs;
		private FP31Vectors _sumOfSqrs;

		private FP31Vectors _zRZiSqrs;
		private FP31Vectors _zRs2;
		private FP31Vectors _zIs2;

		#endregion

		#region Constructor

		public IteratorSimd(FP31VectorsMath fp31VectorsMath)
		{
			_fp31VectorsMath = fp31VectorsMath;

			_threshold = 0;
			_thresholdVector = new Vector256<int>();
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

		public int LimbCount => _fp31VectorsMath.LimbCount;
		public int ValueCount => _fp31VectorsMath.ValueCount;
		public int VectorCount => _fp31VectorsMath.VectorCount;

		public FP31Vectors Crs { get; set; }
		public FP31Vectors Cis { get; set; }
		public FP31Vectors Zrs { get; set; }
		public FP31Vectors Zis { get; set; }

		public bool ZValuesAreZero { get; set; }

		public uint Threshold
		{
			get => _threshold;
			set
			{
				if (value != _threshold)
				{
					_threshold = value;
					_thresholdVector = _fp31VectorsMath.CreateVectorForComparison(_threshold);
				}
			}
		}

		//public MathOpCounts MathOpCounts => _vecMath.MathOpCounts;

		#endregion

		#region Public Methods

		public Vector256<int>[] Iterate(int[] inPlayList, int[] inPlayListNarrow)
		{
			try
			{
				if (ZValuesAreZero)
				{
					// Perform the first iteration. 
					Zrs.UpdateFrom(Crs);
					Zis.UpdateFrom(Cis);
					ZValuesAreZero = false;
				}
				else
				{
					// square(z.r + z.i)
					_fp31VectorsMath.AddThenSquare(Zrs, Zis, _zRZiSqrs, inPlayList, inPlayListNarrow);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
					_fp31VectorsMath.Sub(_zRZiSqrs, _zRSqrs, Zis, inPlayList);
					_fp31VectorsMath.Sub(Zis, _zISqrs, _zIs2, inPlayList);
					_fp31VectorsMath.Add(_zIs2, Cis, Zis, inPlayList);

					// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
					_fp31VectorsMath.Sub(_zRSqrs, _zISqrs, _zRs2, inPlayList);
					_fp31VectorsMath.Add(_zRs2, Crs, Zrs, inPlayList);
				}

				_fp31VectorsMath.Square(Zrs, _zRSqrs, inPlayList, inPlayListNarrow);
				_fp31VectorsMath.Square(Zis, _zISqrs, inPlayList, inPlayListNarrow);
				_fp31VectorsMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs, inPlayList);

				_fp31VectorsMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, _escapedFlagVectors, inPlayList);
				return _escapedFlagVectors;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		public Vector256<int> Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
