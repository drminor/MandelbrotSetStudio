using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

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

		public void UpdateParentId(ObjectId colorBandSetId, ObjectId? parentId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.ParentId, parentId);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateName(ObjectId colorBandSetId, string name)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.Name, name);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateDescription(ObjectId colorBandSetId, string? description)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.Description, description);

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

		public IEnumerable<ObjectId> GetColorBandSetIds(ObjectId projectId)
		{
			// TODO: Use Projection
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("ProjectId", projectId);
			var colorBandSets = Collection.Find(filter).ToList();

			// Get the _id values of the found documents
			var ids = colorBandSets.Select(d => d.Id);

			return ids;
		}

		public DateTime GetLastSaveTime(ObjectId projectId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("ProjectId", projectId);
			var cbSetRecs = Collection.Find(filter).ToList();

			if (cbSetRecs.Count < 1)
			{
				return DateTime.MinValue;
			}
			else
			{
				var result = cbSetRecs.Max(x => x.Id.CreationTime);
				return result;
			}
		}

	}
}
