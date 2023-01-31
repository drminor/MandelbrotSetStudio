using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class ZValues
	{
		private const int VALUE_SIZE = 4;

		#region Constructors

		public ZValues()
		{
			BlockWidth = 0;
			BlockHeight = 0;
			LimbCount = 0;
			Zrs = new byte[0];
			Zis = new byte[0];
			HasEscapedFlags = new byte[0];
		}

		public ZValues(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis, byte[] hasEscapeFlags)
		{
			BlockWidth = blockSize.Width;
			BlockHeight = blockSize.Height;
			LimbCount = limbCount;

			var valueCount = blockSize.NumberOfCells;
			var totalByteCount = blockSize.NumberOfCells * LimbCount * VALUE_SIZE;
			//var bytesPerRow = BlockWidth * LimbCount * VALUE_SIZE;

			Debug.Assert(zrs.Length == totalByteCount, $"The length of zrs does not equal the {valueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");
			Debug.Assert(zis.Length == totalByteCount, $"The length of zis does not equal the {valueCount} * {LimbCount} * {VALUE_SIZE} (values/block) * (limbs/value) x bytes/value).");

			Zrs = zrs;
			Zis = zis;

			HasEscapedFlags = hasEscapeFlags; // new byte[blockSize.NumberOfCells];
		}

		#endregion

		#region Public Properties 

		public int BlockWidth { get; init; }
		public int BlockHeight { get; init; }
		public int LimbCount { get; init; }

		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; set; }

		//// Derived properties
		
		public bool IsEmpty => Zrs.Length == 0;

		private SizeInt? _blockSize = null;

		public SizeInt BlockSize
		{
			get
			{
				if (!_blockSize.HasValue)
				{
					if (BlockWidth == 0 || BlockHeight == 0)
					{
						return new SizeInt(BlockWidth, BlockHeight);
					}
					else
					{
						_blockSize = new SizeInt(BlockWidth, BlockHeight);
						return _blockSize.Value;
					}
				}
				else
				{
					return _blockSize.Value;
				}

			}
		}

		//public SizeInt BlockSize => _blockSize;
		//public int ValueCount => BlockSize.NumberOfCells;
		//public int ValuesPerRow => BlockSize.Width;

		//public int Lanes => _lanes;
		//public int TotalByteCount => _totalByteCount;
		//public int BytesPerRow => _bytesPerRow;

		#endregion
	}
}
