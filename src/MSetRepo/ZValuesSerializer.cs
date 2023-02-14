using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MSS.Types.MSet;
using System;

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

			//wrtr.WriteStartArray(nameof(value.Zrs));
			wrtr.WriteBytes(nameof(value.Zrs), value.Zrs);
			//_byteArraySerializer.Serialize(context, value.Zrs);
			//wrtr.WriteEndArray();

			wrtr.WriteStartArray(nameof(value.Zis));
			//wrtr.WriteBytes(value.Zis);
			_byteArraySerializer.Serialize(context, value.Zis);
			wrtr.WriteEndArray();

			wrtr.WriteStartArray(nameof(value.HasEscapedFlags));
			//wrtr.WriteBytes(value.HasEscapedFlags);
			_byteArraySerializer.Serialize(context, value.HasEscapedFlags);
			wrtr.WriteEndArray();

			wrtr.WriteStartArray(nameof(value.RowsHasEscaped));
			//wrtr.WriteBytes(value.RowsHasEscaped);
			_byteArraySerializer.Serialize(context, value.RowsHasEscaped);
			wrtr.WriteEndArray();

			context.Writer.WriteEndDocument();
		}

		public ZValues Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var rdr = context.Reader;

			rdr.ReadStartDocument();

			var blockWidth = rdr.ReadInt32(nameof(ZValues.BlockWidth));
			var blockHeight = rdr.ReadInt32(nameof(ZValues.BlockHeight));
			var limbCount = rdr.ReadInt32(nameof(ZValues.LimbCount));

			//rdr.ReadName(nameof(ZValues.Zrs));
			var bType = rdr.ReadBsonType();
			rdr.ReadStartArray();
			var zrs = _byteArraySerializer.Deserialize(context, args);
			rdr.ReadEndArray();

			//rdr.ReadName(nameof(ZValues.Zis));
			bType = rdr.ReadBsonType();
			rdr.ReadStartArray();
			var zis = _byteArraySerializer.Deserialize(context, args);
			rdr.ReadEndArray();

			//rdr.ReadName(nameof(ZValues.HasEscapedFlags));
			bType = rdr.ReadBsonType();
			rdr.ReadStartArray();
			var hasEscapedFlags = _byteArraySerializer.Deserialize(context, args);
			rdr.ReadEndArray();


			//rdr.ReadName(nameof(ZValues.RowsHasEscaped));
			bType = rdr.ReadBsonType();
			rdr.ReadStartArray();
			var rowHasEscaped = _byteArraySerializer.Deserialize(context, args);
			rdr.ReadEndArray();

			rdr.ReadEndDocument();

			var result = new ZValues(new MSS.Types.SizeInt(blockWidth, blockHeight), limbCount, zrs, zis, hasEscapedFlags, rowHasEscaped);

			return result;
		}

		#endregion
	}
}
