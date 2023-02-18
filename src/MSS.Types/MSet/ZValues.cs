using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class ZValues
	{
		private const int VALUE_SIZE = 4;

		#region Constructors

		public ZValues()
		{
			BlockWidth = 128;
			BlockHeight = 128;
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

			if (zrs.Length != totalByteCount)
			{
				//Debug.WriteLine($"WARNING: While constructing the ZValues, creating new byte arrays since the incomming arrays are too long. Incoming: {zrs.Length}, Required: {totalByteCount}.");
				Zrs = new byte[totalByteCount];
				Array.Copy(zrs, 0, Zrs, 0, totalByteCount);

				Zis = new byte[totalByteCount];
				Array.Copy(zis, 0, Zis, 0, totalByteCount);
			}
			else
			{
				Zrs = zrs;
				Zis = zis;
			}

			HasEscapedFlags = hasEscapeFlags;
			RowsHasEscaped = rowHasEscaped;
		}

		#endregion

		#region Public Properties 

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(128)]
		public int BlockWidth { get; init; } = 128;

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(128)]
		public int BlockHeight { get; init; } = 128;

		public int LimbCount { get; init; }

		public byte[] Zrs { get; private set; }
		public byte[] Zis { get; private set; }
		public byte[] HasEscapedFlags { get; set; }
		public byte[] RowsHasEscaped { get; init; }

		// Derived properties
		public bool IsEmpty => Zrs.Length == 0;

		[BsonIgnore]
		public SizeInt BlockSize => new SizeInt(BlockWidth, BlockHeight);
		//{
		//	get
		//	{
		//		if (BlockWidth == 0 || BlockHeight == 0)
		//		{
		//			return RMapConstants.BLOCK_SIZE;
		//		}
		//		else
		//		{
		//			return new SizeInt(BlockWidth, BlockHeight);
		//		}
		//	}
		//}

		#endregion
	}
}
