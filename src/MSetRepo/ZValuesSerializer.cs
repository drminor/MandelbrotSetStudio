using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MSS.Types.MSet;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.Intrinsics.Arm;

namespace MSetRepo
{
	public class ZValuesSerializer : IBsonSerializer<ZValues>
	{
		//private readonly BsonInt32Serializer _int32Serializer;
		private readonly ByteArraySerializer _byteArraySerializer;


		#region Constructor

		//private readonly MapSectionZVectorsPool _mMapSectionZVectorsPool;

		//public ZValuesSerializer(MapSectionZVectorsPool mapSectionZVectorsPool)
		//{
		//	_mMapSectionZVectorsPool = mapSectionZVectorsPool;
		//}

		public ZValuesSerializer()
		{
			//_int32Serializer = BsonInt32Serializer.Instance;
			_byteArraySerializer = new ByteArraySerializer(BsonType.Binary);
		}

		#endregion

		#region Public Properties

		public Type ValueType => typeof(ZValues);

		#endregion

		#region Public Methods

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
		{
			if (value is ZValues zValues)
			{
				Serialize(context, args, zValues);
			}
			else
			{
				throw new InvalidOperationException("The object is not of type: ZValues.");
			}
		}

		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var result = Deserialize(context, args);
			return result;
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ZValues value)
		{
			var wrtr = context.Writer;

			wrtr.WriteStartDocument();

			wrtr.WriteInt32(nameof(value.BlockWidth), value.BlockWidth);
			wrtr.WriteInt32(nameof(value.BlockHeight), value.BlockHeight);
			wrtr.WriteInt32(nameof(value.LimbCount), value.LimbCount);

			var zByteCount = value.BlockSize.NumberOfCells * value.LimbCount * 4;

			// Zrs
			//var sl = new Span<byte>(value.Zrs).Slice(0, zByteCount);

			//wrtr.WriteBytes(nameof(value.Zrs), value.Zrs.AsSpan().Slice(0, zByteCount).ToArray());

			var sl = new ByteArrayBuffer(value.Zrs, zByteCount, isReadOnly: true);
			//wrtr.WriteBytes(sl.GetBytes(0,))
			wrtr.WriteRawBsonArray(nameof(value.Zrs), sl);

			// Zis
			//wrtr.WriteBytes(nameof(value.Zis), value.Zrs.AsSpan().Slice(0, zByteCount).ToArray());

			sl = new ByteArrayBuffer(value.Zis, zByteCount, isReadOnly: true);
			wrtr.WriteRawBsonArray(nameof(value.Zis), sl);

			// HasEscapedFlags
			wrtr.WriteName(nameof(value.HasEscapedFlags));
			_byteArraySerializer.Serialize(context, value.HasEscapedFlags);

			// RowHasEscaped
			wrtr.WriteName(nameof(value.RowsHasEscaped));
			_byteArraySerializer.Serialize(context, value.RowsHasEscaped);

			context.Writer.WriteEndDocument();
		}

		public ZValues Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			ZValues result;

			var rdr = context.Reader;

			rdr.ReadStartDocument();

			var blockWidth = rdr.ReadInt32(nameof(ZValues.BlockWidth));
			var blockHeight = rdr.ReadInt32(nameof(ZValues.BlockHeight));
			var limbCount = rdr.ReadInt32(nameof(ZValues.LimbCount));

			var zByteCount = blockWidth * blockHeight * limbCount * 4;

			// Zrs
			_ = rdr.ReadBsonType();
			rdr.ReadName();
			rdr.ReadStartArray();
			var dd = rdr.ReadRawBsonArray();
			rdr.ReadEndArray();
			var zrs = ArrayPool<byte>.Shared.Rent(zByteCount);
			EfficientCopyTo(dd, zrs);

			// Zis
			_ = rdr.ReadBsonType();
			rdr.ReadName();
			rdr.ReadStartArray();
			dd = rdr.ReadRawBsonArray();
			rdr.ReadEndArray();
			var zis = ArrayPool<byte>.Shared.Rent(zByteCount);
			EfficientCopyTo(dd, zis);

			// Has Escaped Flags
			_ = rdr.ReadBsonType();
			rdr.ReadName(nameof(ZValues.HasEscapedFlags));
			var hasEscapedFlags = _byteArraySerializer.Deserialize(context, args);

			// RowHasEscaped
			_ = rdr.ReadBsonType();
			rdr.ReadName(nameof(ZValues.RowsHasEscaped));
			var rowHasEscaped = _byteArraySerializer.Deserialize(context, args);

			rdr.ReadEndDocument();
			result = new ZValues(new MSS.Types.SizeInt(blockWidth, blockHeight), limbCount, zrs, zis, hasEscapedFlags, rowHasEscaped);

			return result;
		}

		#endregion

		#region Private Methods

		public void EfficientCopyTo(IByteBuffer buffer, byte[] destination)
		{
			var position = 0;
			long remainingCount;

			while ((remainingCount = buffer.Length - position) > 0)
			{
				var segment = buffer.AccessBackingBytes(position);
				var count = (int)Math.Min(segment.Count, remainingCount);

				segment.CopyTo(destination, position);

				//destination.Write(segment.Array, segment.Offset, count);
				position += count;
			}
		}


		#endregion
	}
}
