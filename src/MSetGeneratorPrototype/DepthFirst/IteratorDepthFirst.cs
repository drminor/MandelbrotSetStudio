using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorDepthFirst : IIterator
	{
		#region Private Properties

		private IFP31VecMath _fp31VecMath;

		private Vector256<uint>[] _zRZiSqrs;
		private Vector256<uint>[] _temp;
		private Vector256<uint>[] _temp2;

		private Vector256<uint>[] _zRSqrs;
		private Vector256<uint>[] _zISqrs;
		private Vector256<uint>[] _sumOfSqrs;

		#endregion

		#region Constructor

		public IteratorDepthFirst(IFP31VecMath fp31VecMath)
		{
			_fp31VecMath = fp31VecMath;
			var limbCount = fp31VecMath.LimbCount;

			_zRZiSqrs = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_temp = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_temp2 = FP31VecMathHelper.CreateNewLimbSet(limbCount);

			_zRSqrs = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_zISqrs = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_sumOfSqrs = FP31VecMathHelper.CreateNewLimbSet(limbCount);

			//FP31VecMathHelper.ClearLimbSet(_zRZiSqrs);
			//FP31VecMathHelper.ClearLimbSet(_temp);
			//FP31VecMathHelper.ClearLimbSet(_zRSqrs);
			//FP31VecMathHelper.ClearLimbSet(_zISqrs);
			//FP31VecMathHelper.ClearLimbSet(_sumOfSqrs);

		}

		#endregion

		#region Public Properties

		public bool IncreasingIterations { get; set; }

		//public MathOpCounts MathOpCounts => _fp31VecMath.MathOpCounts;

		#endregion

		#region Public Methods

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector256<uint>[] IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> doneFlags)
		{
			if (IncreasingIterations)
			{
				_fp31VecMath.Square(zrs, _zRSqrs, ref doneFlags);
				_fp31VecMath.Square(zis, _zISqrs, ref doneFlags);

				var result = Iterate(crs, cis, zrs, zis, ref doneFlags);
				return result;
			}
			else
			{
				try
				{
					Array.Copy(crs, zrs, crs.Length);
					Array.Copy(cis, zis, cis.Length);

					_fp31VecMath.Square(zrs, _zRSqrs, ref doneFlags);
					_fp31VecMath.Square(zis, _zISqrs, ref doneFlags);
					_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs, ref doneFlags);

					return _sumOfSqrs;
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector256<uint>[] Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> doneFlags)
		{
			try
			{
				// square(z.r + z.i)
				_fp31VecMath.Add(zrs, zis, _temp, ref doneFlags);
				_fp31VecMath.Square(_temp, _zRZiSqrs, ref doneFlags);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis, ref doneFlags);
				_fp31VecMath.Sub(zis, _zISqrs, _temp, ref doneFlags);
				_fp31VecMath.Add(_temp, cis, zis, ref doneFlags);

				// z.r = zrsqr - zisqr + c.r
				_fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp, ref doneFlags);
				_fp31VecMath.Add(_temp, crs, zrs, ref doneFlags);

				_fp31VecMath.Square(zrs, _zRSqrs, ref doneFlags);
				_fp31VecMath.Square(zis, _zISqrs, ref doneFlags);
				_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs, ref doneFlags);

				return _sumOfSqrs;
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
