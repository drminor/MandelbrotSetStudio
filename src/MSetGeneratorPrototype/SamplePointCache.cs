using MSS.Types;
using MSS.Types.APValues;
using System.Collections.Concurrent;

namespace MSetGeneratorPrototype
{
	public class SamplePointCache
	{
		private readonly int _extent;
		//private readonly int _lanes;

		private readonly ConcurrentDictionary<FP31Val, FP31Val[]> _samplePointOffsets;

		public SamplePointCache() : this(RMapConstants.BLOCK_SIZE)
		{ }

		public SamplePointCache(SizeInt blockSize)
		{
			_extent = blockSize.Width;
			//_lanes = Vector256<uint>.Count;

			_samplePointOffsets = new ConcurrentDictionary<FP31Val, FP31Val[]>();
		}

		public FP31Val[] GetSamplePointOffsets(FP31Val delta)
		{
			var result = _samplePointOffsets.GetOrAdd(delta, BuildSamplePointOffsets);
			return result;
		}

		private FP31Val[] BuildSamplePointOffsets(FP31Val delta)
		{
			var limbCount = delta.LimbCount;
			var apFixedPointFormat = new ApFixedPointFormat(limbCount);
			var fP31ScalarMath = new FP31ScalarMath(apFixedPointFormat);

			var offsets = new FP31Val[_extent];

			for (var i = 0; i < _extent; i++)
			{
				offsets[i] = fP31ScalarMath.Multiply(delta, (byte)i);
			}

			return offsets;
		}

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
	}
}
