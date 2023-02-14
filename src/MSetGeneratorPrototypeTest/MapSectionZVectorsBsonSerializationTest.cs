
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
		public void Serialize_A_ZValues_Instance()
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

			var tt = mszvRecord.ToJson();

			Debug.WriteLine($"The serialized value is {tt}.");
		}

		private void RegisterTheSerializers()
		{
			//BsonSerializer.RegisterSerializer(new ZValuesSerializer());

			BsonClassMap.RegisterClassMap<ZValues>(cm => {
				cm.AutoMap();
				cm.GetMemberMap(c => c.Zrs).SetSerializer(new ZValuesSerializer());
				cm.GetMemberMap(c => c.Zis).SetSerializer(new ZValuesSerializer());
			});

		}


	}
}
