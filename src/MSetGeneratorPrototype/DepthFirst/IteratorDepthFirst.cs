using MSS.Common;
using MSS.Common.MSetGenerator;
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

		private uint _threshold;
		private Vector256<int> _thresholdVector;

		private uint _thresholdForEscVel;
		private Vector256<int> _thresholdVectorForEscVel;


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

			_threshold = 0;
			_thresholdVector = new Vector256<int>();

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

		public uint ThresholdForEscVel
		{
			get => _thresholdForEscVel;
			set
			{
				if (value != _thresholdForEscVel)
				{
					_thresholdForEscVel = value;
					_thresholdVectorForEscVel = _fp31VecMath.CreateVectorForComparison(_thresholdForEscVel);
				}
			}
		}

		public MathOpCounts MathOpCounts => _fp31VecMath.MathOpCounts;

		public Vector256<uint>[] GetModulusSquared(Vector256<uint>[] zrs, Vector256<uint>[] zis)
		{
			var result = FP31VecMathHelper.CreateNewLimbSet(_fp31VecMath.LimbCount);

			_fp31VecMath.Square(zrs, _temp);
			_fp31VecMath.Square(zis, _temp2);

			_fp31VecMath.Add(_temp, _temp2, result);

			return result;
		}

		#endregion

		#region Public Methods - New

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector256<uint>[] IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis)
		{
			if (IncreasingIterations)
			{
				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);

				var result = Iterate(crs, cis, zrs, zis);
				return result;
			}
			else
			{
				try
				{
					Array.Copy(crs, zrs, crs.Length);
					Array.Copy(cis, zis, cis.Length);

					_fp31VecMath.Square(zrs, _zRSqrs);
					_fp31VecMath.Square(zis, _zISqrs);
					_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

					//_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, ref escapedFlagsVec);

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
		public Vector256<uint>[] Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis)
		{
			try
			{
				// square(z.r + z.i)
				_fp31VecMath.Add(zrs, zis, _temp);
				_fp31VecMath.Square(_temp, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
				_fp31VecMath.Sub(zis, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, cis, zis);

				// z.r = zrsqr - zisqr + c.r
				_fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, crs, zrs);

				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);
				_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

				//_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, ref escapedFlagsVec);

				return _sumOfSqrs;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		#endregion

		#region Public Methods - Old

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec)
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
					Array.Copy(crs, zrs, crs.Length);
					Array.Copy(cis, zis, cis.Length);

					_fp31VecMath.Square(zrs, _zRSqrs);
					_fp31VecMath.Square(zis, _zISqrs);
					_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

					_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, ref escapedFlagsVec);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec)
		{
			try
			{
				// square(z.r + z.i)
				_fp31VecMath.Add(zrs, zis, _temp);
				_fp31VecMath.Square(_temp, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
				_fp31VecMath.Sub(zis, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, cis, zis);

				// z.r = zrsqr - zisqr + c.r
				_fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, crs, zrs);

				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);
				_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

				_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, ref escapedFlagsVec);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		#endregion

		#region Public Methods with Large Bailout Support

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec, ref Vector256<int> escapedFlagsLargeBailoutVec)
		{
			if (IncreasingIterations)
			{
				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);

				Iterate(crs, cis, zrs, zis, ref escapedFlagsVec, ref escapedFlagsLargeBailoutVec);
			}
			else
			{
				try
				{
					Array.Copy(crs, zrs, crs.Length);
					Array.Copy(cis, zis, cis.Length);

					_fp31VecMath.Square(zrs, _zRSqrs);
					_fp31VecMath.Square(zis, _zISqrs);
					_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

					_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, ref escapedFlagsVec);
					_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVectorForEscVel, ref escapedFlagsLargeBailoutVec);
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Iterator received exception: {e}.");
					throw;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec, ref Vector256<int> escapedFlagsLargeBailoutVec)
		{
			try
			{
				// square(z.r + z.i)
				_fp31VecMath.Add(zrs, zis, _temp);
				_fp31VecMath.Square(_temp, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i
				_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
				_fp31VecMath.Sub(zis, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, cis, zis);

				// z.r = zrsqr - zisqr + c.r
				_fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp);
				_fp31VecMath.Add(_temp, crs, zrs);

				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);
				_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

				_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVector, ref escapedFlagsVec);

				_fp31VecMath.IsGreaterOrEqThan(_sumOfSqrs, _thresholdVectorForEscVel, ref escapedFlagsLargeBailoutVec);

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
