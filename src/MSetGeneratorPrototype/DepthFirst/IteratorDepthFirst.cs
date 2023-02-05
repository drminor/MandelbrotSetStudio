﻿using MSS.Types;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorDepthFirst //: IIterator
	{
		#region Private Properties

		private FP31VecMath _fp31VecMath;

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

		public IteratorDepthFirst(FP31VecMath fp31VecMath)
		{
			_fp31VecMath = fp31VecMath;

			IsReset = true;
			_threshold = 0;
			_thresholdVector = new Vector256<int>();

			_zRSqrs = fp31VecMath.GetNewLimbSet();
			_zISqrs = fp31VecMath.GetNewLimbSet();
			_sumOfSqrs = fp31VecMath.GetNewLimbSet();

			_zRZiSqrs = fp31VecMath.GetNewLimbSet();
			_zRs2 = fp31VecMath.GetNewLimbSet();
			_zIs2 = fp31VecMath.GetNewLimbSet();
		}

		#endregion

		#region Public Properties

		public bool IsReset { get; private set; }
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

		public void Reset()
		{
			IsReset = true;
		}

		public Vector256<int> Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis)
		{
			try
			{
				if (IsReset)
				{
					// Perform the first iteration. 
					if (!IncreasingIterations)
					{
						Array.Copy(crs, zrs, crs.Length);
						Array.Copy(cis, zis, cis.Length);
					}
					//else
					//{
					//	ClearVectors(_zRSqrs);
					//	ClearVectors(_zISqrs);
					//}
					IsReset = false;
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

		private void ClearVectors(Vector256<uint>[] vectors)
		{
			for (var i = 0; i < vectors.Length; i++)
			{
				vectors[i] = Vector256<uint>.Zero;
			}
		}
	}
}
