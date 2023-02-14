using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Diagnostics;

namespace MSS.Types.MSet
{
	//[BsonSerializer(typeof(ZValuesSerializer))]
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
			RowsHasEscaped = new byte[0];
		}

		public ZValues(SizeInt blockSize, int limbCount, byte[] zrs, byte[] zis, byte[] hasEscapeFlags, byte[] rowHasEscaped)
		{
			BlockWidth = blockSize.Width;
			BlockHeight = blockSize.Height;
			LimbCount = limbCount;

			var valueCount = blockSize.NumberOfCells;
			var totalByteCount = valueCount * LimbCount * VALUE_SIZE;

			Zrs = new byte[totalByteCount];
			Array.Copy(zrs, 0, Zrs, 0, totalByteCount);

			Zis = new byte[totalByteCount];
			Array.Copy(zis, 0, Zis, 0, totalByteCount);

			HasEscapedFlags = hasEscapeFlags;
			RowsHasEscaped = rowHasEscaped;
		}

		#endregion

		#region Public Properties 

		public int BlockWidth { get; init; }
		public int BlockHeight { get; init; }
		public int LimbCount { get; init; }

		//[BsonSerializer(typeof(MapSectionZVectors))]
		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; set; }
		public byte[] RowsHasEscaped { get; init; }

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

		#endregion
	}
}
