using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectRepo.ReaderWriters
{
	internal class MapSectionBinaryReaderWriter
	{

		///// <summary>
		///// Writes a raw BSON array.
		///// </summary>
		///// <param name="slice">The byte buffer containing the raw BSON array.</param>
		//public virtual void WriteRawBsonArray(IByteBuffer slice)
		//{
		//	// overridden in BsonBinaryWriter to write the raw bytes to the stream
		//	// for all other streams, deserialize the raw bytes and serialize the resulting array instead

		//	var documentLength = slice.Length + 8;
		//	using (var memoryStream = new MemoryStream(documentLength))
		//	{
		//		// wrap the array in a fake document so we can deserialize it
		//		var streamWriter = new BsonStreamWriter(memoryStream, Utf8Helper.StrictUtf8Encoding);
		//		streamWriter.WriteInt32(documentLength);
		//		streamWriter.WriteBsonType(BsonType.Array);
		//		streamWriter.WriteByte((byte)'x');
		//		streamWriter.WriteByte(0);
		//		slice.WriteTo(streamWriter.BaseStream);
		//		streamWriter.WriteByte(0);

		//		memoryStream.Position = 0;
		//		using (var bsonReader = new BsonBinaryReader(memoryStream, BsonBinaryReaderSettings.Defaults))
		//		{
		//			var deserializationContext = BsonDeserializationContext.CreateRoot<BsonDocument>(bsonReader);
		//			bsonReader.ReadStartDocument();
		//			bsonReader.ReadName("x");
		//			var array = deserializationContext.DeserializeWithChildContext(BsonArraySerializer.Instance);
		//			bsonReader.ReadEndDocument();

		//			var serializationContext = BsonSerializationContext.CreateRoot<BsonArray>(this);
		//			BsonArraySerializer.Instance.Serialize(serializationContext, array);
		//		}
		//	}
		//}


	}
}
