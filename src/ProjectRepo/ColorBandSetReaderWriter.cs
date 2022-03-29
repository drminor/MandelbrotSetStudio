using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;

namespace ProjectRepo
{
	public class ColorBandSetReaderWriter : MongoDbCollectionBase<ColorBandSetRecord>
	{
		private const string COLLECTION_NAME = "ColorBandSets";

		public ColorBandSetReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public IEnumerable<ColorBandSetRecord> GetAll()
		{
			var colorBandSetRecords = Collection.Find(_ => true).ToEnumerable();
			return colorBandSetRecords;
		}

		public ColorBandSetRecord Get(ObjectId colorBandSetId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			var colorBandSetRecord = Collection.Find(filter).FirstOrDefault();

			return colorBandSetRecord;
		}

		public ColorBandSetRecord Get(Guid colorBandSetSerialNumber)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("SerialNumber", colorBandSetSerialNumber.ToByteArray());
			var colorBandSetRecord = Collection.Find(filter).FirstOrDefault();

			return colorBandSetRecord;
		}

		public bool TryGet(ObjectId colorBandSetId, out ColorBandSetRecord colorBandSetRecord)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			colorBandSetRecord = Collection.Find(filter).FirstOrDefault();

			return colorBandSetRecord != null;
		}

		public bool TryGet(Guid colorBandSetSerialNumber, out ColorBandSetRecord colorBandSetRecord)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("SerialNumber", colorBandSetSerialNumber.ToByteArray());
			colorBandSetRecord = Collection.Find(filter).FirstOrDefault();

			return colorBandSetRecord != null;
		}

		public ObjectId Insert(ColorBandSetRecord colorBandSetRecord)
		{
			Collection.InsertOne(colorBandSetRecord);
			return colorBandSetRecord.Id;
		}

		public void UpdateName(ObjectId colorBandSetId, string name)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.Name, name);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateDescription(ObjectId colorBandSetId, string description)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.Description, description);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateVersionNumber(ObjectId colorBandSetId, int versionNumber)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.VersionNumber, versionNumber);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateColorBands(ObjectId colorBandSetId, ColorBandRecord[] colorBandsRecords)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.ColorBandRecords, colorBandsRecords);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public long? Delete(ObjectId colorBandSetId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}
	}
}
