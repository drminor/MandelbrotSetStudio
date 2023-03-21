using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal class IteratorUPointers : IDisposable
	{
		#region Private Properties

		private FP31VecMathUPointers _fp31VecMath;

		private uint _threshold;
		private Vector256<int> _thresholdVector;

		private byte[] _zRZiSqrs_storage;
		private VecBuffer _zRZiSqrs;

		private byte[] _temp1_storage;
		private byte[] _temp2_storage;
		private byte[] _temp3_storage;

		private VecBuffer _temp1;
		private VecBuffer _temp2;
		private VecBuffer _temp3;

		private byte[] _zRSqrs_storage;
		private byte[] _zISqrs_storage;
		//private byte[] _sumOfSqrs_storage;

		private VecBuffer _zRSqrs;
		private VecBuffer _zISqrs;
		//private VecBuffer _sumOfSqrs;

		#endregion

		#region Constructor

		public IteratorUPointers(FP31VecMathUPointers fp31VecMath)
		{
			_fp31VecMath = fp31VecMath;

			_threshold = 0;
			_thresholdVector = new Vector256<int>();

			var limbCount = fp31VecMath.LimbCount;
			
			_zRZiSqrs_storage = new byte[limbCount * 32];
			Array.Fill<byte>(_zRZiSqrs_storage, 0);
			_zRZiSqrs = new VecBuffer(_zRZiSqrs_storage);

			_temp1_storage = new byte[limbCount * 32];
			Array.Fill<byte>(_temp1_storage, 0);
			_temp1 = new VecBuffer(_temp1_storage);

			_temp2_storage = new byte[limbCount * 32];
			Array.Fill<byte>(_temp2_storage, 0);
			_temp2 = new VecBuffer(_temp2_storage);

			_temp3_storage = new byte[limbCount * 32];
			Array.Fill<byte>(_temp3_storage, 0);
			_temp3 = new VecBuffer(_temp3_storage);

			_zRSqrs_storage = new byte[limbCount * 32];
			Array.Fill<byte>(_zRSqrs_storage, 0);
			_zRSqrs = new VecBuffer(_zRSqrs_storage);

			_zISqrs_storage = new byte[limbCount * 32];
			Array.Fill<byte>(_zISqrs_storage, 0);
			_zISqrs = new VecBuffer(_zISqrs_storage);

			//_sumOfSqrs_storage = new byte[limbCount * 32];
			//Array.Fill<byte>(_sumOfSqrs_storage, 0);
			//_sumOfSqrs = new VecBuffer(_sumOfSqrs_storage);
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
					//_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);

					var sumOfSquaresMsl = _fp31VecMath.GetMslOfSum(_zRSqrs, _zISqrs);

					_fp31VecMath.IsGreaterOrEqThan(ref sumOfSquaresMsl, ref _thresholdVector, ref escapedFlagsVec);
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
				_fp31VecMath.Add(zrs, zis, _temp1);
				_fp31VecMath.Square(_temp1, _zRZiSqrs);

				// z.i = square(z.r + z.i) - zrsqr - zisqr + c.i	TODO: Create a method: SubSubAdd		
				_fp31VecMath.Sub(_zRZiSqrs, _zRSqrs, zis);
				_fp31VecMath.Sub(zis, _zISqrs, _temp2);
				_fp31VecMath.Add(_temp2, cis, zis);

				// z.r = zrsqr - zisqr + c.r						TODO: Create a method: SubAdd
				_fp31VecMath.Sub(_zRSqrs, _zISqrs, _temp3);
				_fp31VecMath.Add(_temp3, crs, zrs);

				_fp31VecMath.Square(zrs, _zRSqrs);
				_fp31VecMath.Square(zis, _zISqrs);

				//_fp31VecMath.Add(_zRSqrs, _zISqrs, _sumOfSqrs);
				var sumOfSquaresMsl = _fp31VecMath.GetMslOfSum(_zRSqrs, _zISqrs);

				_fp31VecMath.IsGreaterOrEqThan(ref sumOfSquaresMsl, ref _thresholdVector, ref escapedFlagsVec);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Iterator received exception: {e}.");
				throw;
			}
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					_temp1.Dispose();
					_temp2.Dispose();
					_temp3.Dispose();
					_zISqrs.Dispose();
					_zRSqrs.Dispose();
					_zRZiSqrs.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~IteratorUPointers()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
