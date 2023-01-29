using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSS.Types.MSet
{
	public class ZValues
	{
		private const int VALUE_SIZE = 4;

		#region Constructors

		public ZValues()
		{
			LimbCount = 0;
			Zrs = new byte[0];
			Zis = new byte[0];
			HasEscapedFlags = new byte[0];
		}

		public ZValues(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis)
		{
			BlockSize = blockSize; 
			LimbCount = limbCount;

			var valueCount = blockSize.NumberOfCells;
			var totalByteCount = blockSize.NumberOfCells * LimbCount * VALUE_SIZE;
			//var bytesPerRow = BlockWidth * LimbCount * VALUE_SIZE;

			Debug.Assert(zrs.Length == totalByteCount, $"The length of zrs does not equal the {valueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");
			Debug.Assert(zis.Length == totalByteCount, $"The length of zis does not equal the {valueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");

			Zrs = zrs;
			Zis = zis;

			HasEscapedFlags = new byte[blockSize.NumberOfCells];
		}

		#endregion

		#region Public Properties 

		public SizeInt BlockSize { get; init; }
		public int LimbCount { get; init; }

		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; set; }

		//// Derived properties
		
		public bool IsEmpty => Zrs.Length == 0;

		//public SizeInt BlockSize => _blockSize;
		//public int ValueCount => BlockSize.NumberOfCells;
		//public int ValuesPerRow => BlockSize.Width;

		//public int Lanes => _lanes;
		//public int TotalByteCount => _totalByteCount;
		//public int BytesPerRow => _bytesPerRow;

		#endregion
	}
}
