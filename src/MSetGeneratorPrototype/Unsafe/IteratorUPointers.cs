using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorUPointers
	{
		#region Private Properties

		private FP31VecMathUPointers _fp31VecMath;

		private uint _threshold;
		private Vector256<int> _thresholdVector;

		private VecBuffer _zRZiSqrs;
		private VecBuffer _temp;

		private VecBuffer _zRSqrs;
		private VecBuffer _zISqrs;
		private VecBuffer _sumOfSqrs;

		#endregion

		#region Constructor

		public IteratorUPointers(FP31VecMathUPointers fp31VecMath)
		{
			_fp31VecMath = fp31VecMath;

			_threshold = 0;
			_thresholdVector = new Vector256<int>();

			_zRZiSqrs = fp31VecMath.CreateNewLimbSet();
			_temp = fp31VecMath.CreateNewLimbSet();

			_zRSqrs = fp31VecMath.CreateNewLimbSet();
			_zISqrs = fp31VecMath.CreateNewLimbSet();
			_sumOfSqrs = fp31VecMath.CreateNewLimbSet();
		}

		#endregion

		#region Public Properties

		public bool IncreasingIterations { get; set; }

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

		public MathOpCounts MathOpCounts => _fp31VecMath.MathOpCounts;

		#endregion

		#region Public Methods

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IterateFirstRound(VecBuffer crs, VecBuffer cis, VecBuffer zrs, VecBuffer zis, ref Vector256<int> escapedFlagsVec)
		{
			if (IncreasingIterations)
			{
				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);

				Iterate(crs, cis, zrs, zis, ref escapedFlagsVec);
			}
			else
			{
				try
				{
					_fp31VecMath.CopyLimbSet(crs, zrs);
					_fp31VecMath.CopyLimbSet(cis, zis);

					_fp31VecMath.Square(zrs, _zRSqrs);
					_fp31VecMath.Square(zis, _zISqrs);
					_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

					_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, ref _thresholdVector, ref escapedFlagsVec);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Iterate(VecBuffer crs, VecBuffer cis, VecBuffer zrs, VecBuffer zis, ref Vector256<int> escapedFlagsVec)
		{
			try
			{
				// square(z.r + z.i)
				_fp31VecMath.Add(zrs, zis, _temp);
				_fp31VecMath.Square(_temp, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
				_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
				_fp31VecMath.Sub(zis, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, cis, zis);

				// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
				_fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, crs, zrs);

				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);
				_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

				_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, ref _thresholdVector, ref escapedFlagsVec);
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
