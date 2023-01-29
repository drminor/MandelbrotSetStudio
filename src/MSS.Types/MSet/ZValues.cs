using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSS.Types.MSet
{
	public class ZValues
	{
		private const int VALUE_SIZE = 4;

		private readonly int _lanes;
		private readonly int _totalByteCount;
		private readonly int _bytesPerRow;

		public ZValues()
		{
			BlockSize = new SizeInt();
			LimbCount = 0;
			Zrs = new byte[0];
			Zis = new byte[0];
			HasEscapedFlags = new byte[0];
		}

		public ZValues(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis)
		{
			BlockSize = blockSize;
			LimbCount = limbCount;

			_lanes = Vector256<uint>.Count;
			_totalByteCount = blockSize.NumberOfCells * LimbCount * VALUE_SIZE;
			_bytesPerRow = ValuesPerRow * LimbCount * VALUE_SIZE;

			Debug.Assert(zrs.Length == TotalByteCount, $"The length of zrs does not equal the {ValueCount} * {LimbCount} * 4 (values/block) * (limbs/value) x bytes/value).");
			Debug.Assert(zis.Length == TotalByteCount, $"The length of zis does not equal the {ValueCount} * {LimbCount} * 4 (values/block) * (limbs/value) x bytes/value).");

			Zrs = zrs;
			Zis = zis;

			HasEscapedFlags = new byte[blockSize.NumberOfCells];
		}

		public bool IsEmpty => Zrs.Length == 0;

		public SizeInt BlockSize { get; init; }
		public int LimbCount { get; init; }

		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; set; }

		public int ValueCount => BlockSize.NumberOfCells;
		public int ValuesPerRow => BlockSize.Width;

		public int Lanes => _lanes;
		public int TotalByteCount => _totalByteCount;
		public int BytesPerRow => _bytesPerRow;
	}
}
