using MSS.Common.APValues;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorSimdDepthFirst : IIterator
	{
		#region Private Properties

		private VecMath9 _fp31VecMath;

		private uint _threshold;
		private Vector256<int> _thresholdVector;

		private Vector256<uint>[] _zRSqrs;
		private Vector256<uint>[] _zISqrs;
		private Vector256<uint>[] _sumOfSqrs;

		private Vector256<uint>[] _zRZiSqrs;
		private Vector256<uint>[] _zRs2;
		private Vector256<uint>[] _zIs2;

		#endregion

		#region Constructor

		public IteratorSimdDepthFirst(VecMath9 fp31VecMath, int valueCount)
		{
			_fp31VecMath = fp31VecMath;
			ValueCount = valueCount;

			_threshold = 0;
			_thresholdVector = new Vector256<int>();

			Crs = new FP31Vectors(LimbCount, ValueCount);
			Cis = new FP31Vectors(LimbCount, ValueCount);
			Zrs = new FP31Vectors(LimbCount, ValueCount);
			Zis = new FP31Vectors(LimbCount, ValueCount);

			ZValuesAreZero = true;

			_zRSqrs = Enumerable.Repeat(Vector256<uint>.Zero, LimbCount).ToArray();
			_zISqrs = new Vector256<uint>[LimbCount];
			_sumOfSqrs = new Vector256<uint>[LimbCount];
			_zRZiSqrs = new Vector256<uint>[LimbCount];
			_zRs2 = new Vector256<uint>[LimbCount];
			_zIs2 = new Vector256<uint>[LimbCount];
		}

		#endregion

		#region Public Properties

		public int LimbCount => _fp31VecMath.LimbCount;
		public int ValueCount { get; init; }
		public int VectorCount { get; init; }


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
					_thresholdVector = _fp31VecMath.CreateVectorForComparison(_threshold);
				}
			}
		}

		//public MathOpCounts MathOpCounts => _vecMath.MathOpCounts;

		#endregion

		#region Public Methods

		public Vector256<int>[] Iterate(int[] inPlayList, int[] inPlayListNarrow)
		{
			throw new NotImplementedException();
		}

		public Vector256<int> Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis)
		{
			try
			{
				if (ZValuesAreZero)
				{
					// Perform the first iteration.
					Array.Copy(crs, zrs, crs.Length);
					Array.Copy(cis, zis, cis.Length);
					ZValuesAreZero = false;
				}
				else
				{
					// square(z.r + z.i)
					_fp31VecMath.AddThenSquare(zrs, zis, _zRZiSqrs);

					// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
					_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
					_fp31VecMath.Sub(zis, _zISqrs, _zIs2);
					_fp31VecMath.Add(_zIs2, cis, zis);

					// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
					_fp31VecMath.Sub(_zRSqrs, _zISqrs, _zRs2);
					_fp31VecMath.Add(_zRs2, crs, zrs);
				}

				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);
				_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

				var escapedFlags = _fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs[^1], _thresholdVector);
				return escapedFlags;
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
