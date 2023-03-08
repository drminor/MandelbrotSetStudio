using MSS.Types;
using MSS.Types.APValues;
using System;
using System.Collections.Concurrent;

namespace MSS.Common
{
	public class SamplePointCache : IDisposable
	{
		private readonly ConcurrentDictionary<FP31Val, FP31Val[]> _samplePointOffsets;
		private readonly ConcurrentDictionary<int, FP31ScalarMath> _mathImplementations;
		private readonly ConcurrentDictionary<int, IFP31VecMath> _vecMathImplementations;

		#region Constructors

		public SamplePointCache() : this(RMapConstants.BLOCK_SIZE)
		{ }

		public SamplePointCache(SizeInt blockSize)
		{
			BlockSize = blockSize;
			_samplePointOffsets = new ConcurrentDictionary<FP31Val, FP31Val[]>();
			_mathImplementations = new ConcurrentDictionary<int, FP31ScalarMath>();
			_vecMathImplementations = new ConcurrentDictionary<int, IFP31VecMath>();
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }

		#endregion

		#region Sample Point Offsets

		public FP31Val[] GetSamplePointOffsets(FP31Val delta) => _samplePointOffsets.GetOrAdd(delta, BuildSamplePointOffsets);

		private FP31Val[] BuildSamplePointOffsets(FP31Val delta)
		{
			//var strDeltaVal = RValueHelper.ConvertToString(delta.GetRValue());
			//Debug.WriteLine($"Building SampePointOffsets for Delta: {strDeltaVal}.");

			//var limbCount = delta.LimbCount;
			//var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			//var fP31ScalarMath = new FP31ScalarMath(apFixedPointFormat);

			var fP31ScalarMath = GetScalarMath(delta.LimbCount);
			var offsets = new FP31Val[BlockSize.Width];

			for (var i = 0; i < offsets.Length; i++)
			{
				offsets[i] = fP31ScalarMath.Multiply(delta, (byte)i);
			}

			return offsets;
		}

		#endregion

		#region Maths

		public FP31ScalarMath GetScalarMath(int limbCount) => _mathImplementations.GetOrAdd(limbCount, BuildScalarMath);

		private FP31ScalarMath BuildScalarMath(int limbCount) => new FP31ScalarMath(new ApFixedPointFormat(limbCount));

		#endregion

		#region Vector Maths

		public IFP31VecMath GetVectorMath(int limbCount) => _vecMathImplementations.GetOrAdd(limbCount, BuildVectorMath);

		private IFP31VecMath BuildVectorMath(int limbCount)
		{
			return limbCount switch
			{
				1 => new FP31VecMathL1(new ApFixedPointFormat(limbCount)),
				2 => new FP31VecMathL2(new ApFixedPointFormat(limbCount)),
				_ => new FP31VecMath(new ApFixedPointFormat(limbCount))
			};
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
					// Set large fields to null

					_samplePointOffsets.Clear();
					_mathImplementations.Clear();
					_vecMathImplementations.Clear();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region Not Used

		//private Vector256<uint>[] BuildSamplePointOffsets(FP31Val delta)
		//{
		//	var limbCount = delta.LimbCount;
		//	var apFixedPointFormat = new ApFixedPointFormat(limbCount);
		//	var fP31ScalarMath = new FP31ScalarMath(apFixedPointFormat);

		//	var vectorsPerRow = limbCount * _extent;
		//	var offsets = new Vector256<uint> [vectorsPerRow];
		//	var offsetsBack = MemoryMarshal.Cast<Vector256<uint>, uint>(offsets);

		//	var resultPtr = 0;

		//	for (var i = 0; i < _extent; i++)
		//	{
		//		var offset = fP31ScalarMath.Multiply(delta, (byte)i);

		//		for (var limbPtr = 0; limbPtr < limbCount; limbPtr++)
		//		{
		//			offsetsBack[resultPtr++] = offset.Mantissa[limbPtr];
		//		}
		//	}

		//	return offsets;
		//}

		#endregion
	}
}
