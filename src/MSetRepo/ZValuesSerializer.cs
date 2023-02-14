using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MSS.Types;
using System;

namespace MSetRepo
{
	public class ZValuesSerializer : IBsonSerializer<byte[]>
	{
		//#region Constructor

		//private readonly MapSectionZVectorsPool _mMapSectionZVectorsPool;

		//public ZValuesSerializer(MapSectionZVectorsPool mapSectionZVectorsPool)
		//{
		//	_mMapSectionZVectorsPool = mapSectionZVectorsPool;
		//}

		//#endregion

		#region Public Properties

		public Type ValueType => typeof(byte[]);

		#endregion

		#region Public Methods

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
		{
			if (value is byte[] bArray)
			{
				Serialize(context, args, bArray);
			}
			else
			{
				throw new InvalidOperationException("The object value is not a byte array.");
			}
		}

		object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var result = Deserialize(context, args);
			return result;
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, byte[] value)
		{
			var byteArraySerializer = new ByteArraySerializer(BsonType.Binary);
			byteArraySerializer.Serialize(context, args, value);
		}

		//public byte[] Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		//{
		//	var byteArraySerializer = new ByteArraySerializer(BsonType.Binary);
		//	var result = byteArraySerializer.Deserialize(context, args);

		//	return result;
		//}

		public byte[] Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var byteArraySerializer = new ByteArraySerializer(BsonType.Binary);
			var result = byteArraySerializer.Deserialize(context, args);

			return result;
		}


		#endregion
	}
}
