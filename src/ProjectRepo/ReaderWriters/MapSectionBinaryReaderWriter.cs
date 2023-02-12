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

		/// <summary>
		/// Writes a raw BSON array.
		/// </summary>
		/// <param name="slice">The byte buffer containing the raw BSON array.</param>
		public virtual void WriteRawBsonArray(byte[] data)
		{
			// overridden in BsonBinaryWriter to write the raw bytes to the stream
			// for all other streams, deserialize the raw bytes and serialize the resulting array instead

			var documentLength = data.Length + 8;
			using (var memoryStream = new MemoryStream(documentLength))
			{
				var streamWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults);

				streamWriter.WriteInt32(documentLength);
				streamWriter.WriteStartArray("x");
				streamWriter.WriteBytes(data);
				streamWriter.WriteEndArray();

				memoryStream.Position = 0;
				using (var bsonReader = new BsonBinaryReader(memoryStream, BsonBinaryReaderSettings.Defaults))
				{
					var deserializationContext = BsonDeserializationContext.CreateRoot(bsonReader);
					bsonReader.ReadStartDocument();
					bsonReader.ReadName("x");
					var array = deserializationContext.DynamicArraySerializer;
					bsonReader.ReadEndDocument();

					using (var bsonWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
					{
						var serializationContext = BsonSerializationContext.CreateRoot(bsonWriter);
						BsonArraySerializer.Instance.Serialize(serializationContext, array);
					}

				}
			}
		}


	}
}
