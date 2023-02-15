
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System.Diagnostics;
using static MongoDB.Driver.WriteConcern;

namespace MSetGeneratorPrototypeTest
{
	public class MapSectionZVectorsBsonSerializationTest
	{
		private const int VALUE_SIZE = 4;

		[Fact]
		public void Serialize_A_ZValues_InstanceToJSON()
		{
			RegisterTheSerializers();

			var blockSize = new SizeInt(10);
			var limbCount = 2;

			var valueCount = blockSize.NumberOfCells;
			var rowCount = blockSize.Height;
			var totalByteCount = valueCount * limbCount * VALUE_SIZE;
			var totalBytesForFlags = valueCount * VALUE_SIZE;

			var zrs = new byte[totalByteCount];
			var zis = new byte[totalByteCount];

			var hasEscapedFlags = new byte[totalBytesForFlags];
			var rowHasEscaped = new byte[rowCount * VALUE_SIZE];

			var zValues = new ZValues(blockSize, limbCount, zrs, zis, hasEscapedFlags, rowHasEscaped);

			var mszvRecord = new MapSectionZValuesRecord(DateTime.UtcNow, ObjectId.GenerateNewId(), zValues);

			var json1 = mszvRecord.ToJson();
			Debug.WriteLine($"The serialized value is {json1}.");

			var json2 = zValues.ToJson();
			var o = BsonSerializer.Deserialize(json2, typeof(ZValues));

			if (o is ZValues zValues1)
			{
				Debug.Write($"Successfull deserialized the ZValues. The length of zrs is {zValues1.Zrs.Length}");
			}
		}

		[Fact]
		public void Serialize_A_ZValues_InstanceToBSON()
		{
			RegisterTheSerializers();

			var blockSize = new SizeInt(10);
			var limbCount = 2;

			var valueCount = blockSize.NumberOfCells;
			var rowCount = blockSize.Height;
			var totalByteCount = valueCount * limbCount * VALUE_SIZE;
			var totalBytesForFlags = valueCount * VALUE_SIZE;

			var zrs = new byte[totalByteCount];
			var zis = new byte[totalByteCount];

			var hasEscapedFlags = new byte[totalBytesForFlags];
			var rowHasEscaped = new byte[rowCount * VALUE_SIZE];

			var zValues = new ZValues(blockSize, limbCount, zrs, zis, hasEscapedFlags, rowHasEscaped);

			var mszvRecord = new MapSectionZValuesRecord(DateTime.UtcNow, ObjectId.GenerateNewId(), zValues);

			var bson1 = mszvRecord.ToBson();
			Debug.WriteLine($"The serialized value is {bson1}.");

			var bson2 = zValues.ToBson();
			var o = BsonSerializer.Deserialize(bson2, typeof(ZValues));

			if (o is ZValues zValues1)
			{
				Debug.Write($"Successfull deserialized the ZValues. The length of zrs is {zValues1.Zrs.Length}");
			}
		}

		private void RegisterTheSerializers()
		{
			BsonSerializer.RegisterSerializer(new ZValuesSerializer());

			//BsonClassMap.RegisterClassMap<ZValues>(cm => {
			//	cm.AutoMap();
			//	cm.GetMemberMap(c => c.Zrs).SetSerializer(new ZValuesArraySerializer());
			//	cm.GetMemberMap(c => c.Zis).SetSerializer(new ZValuesArraySerializer());
			//});

		}


	}
}
