using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectRepo
{
	public class SharedColorBandSetReaderWriter : MongoDbCollectionBase<ColorBandSetRecord>
	{
		private const string COLLECTION_NAME = "SharedColorBandSets";

		#region Constructor and Collection Support

		public SharedColorBandSetReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public void CreateNameAndIterationsIndex()
		{
			var indexKeysDef = Builders<ColorBandSetRecord>.IndexKeys
				.Ascending(x => x.Name)
				.Ascending(x => x.TargetIterations);

			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<ColorBandSetRecord>(indexKeysDef, new CreateIndexOptions() { Unique = true, Name = "NameAndIterations" }));
		}

#endregion

		public IEnumerable<ColorBandSetRecord> GetAll()
		{
			var colorBandSetRecords = Collection.Find(_ => true).ToEnumerable();
			return colorBandSetRecords;
		}

		public ColorBandSetRecord? Get(ObjectId colorBandSetId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			var colorBandSetRecord = Collection.Find(filter).FirstOrDefault();

			return colorBandSetRecord;
		}

		public bool TryGet(ObjectId colorBandSetId, out ColorBandSetRecord colorBandSetRecord)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			colorBandSetRecord = Collection.Find(filter).FirstOrDefault();

			return colorBandSetRecord != null;
		}

		public ObjectId Insert(ColorBandSetRecord colorBandSetRecord)
		{
			Collection.InsertOne(colorBandSetRecord);
			return colorBandSetRecord.Id;
		}

		public void UpdateName(ObjectId colorBandSetId, string? name)
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

		//public void UpdateColorBands(ObjectId colorBandSetId, ColorBandRecord[] colorBandsRecords)
		//{
		//	var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

		//	var updateDefinition = Builders<ColorBandSetRecord>.Update
		//		.Set(u => u.ColorBandRecords, colorBandsRecords);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		public long? Delete(ObjectId colorBandSetId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public bool Exists(string name)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("Name", name);
			var colorBandSetRecord = Collection.Find(filter).FirstOrDefault();
			var result = colorBandSetRecord != null;

			return result;
		}


		//public int UpdateColorBandSetSchema()
		//{
		//	var colorBandSetRecords = GetAll().ToList();

		//	foreach (var cbsRec in colorBandSetRecords)
		//	{

		//		var targetIterations = cbsRec.TargetIterations;
		//		if (targetIterations == 0)
		//		{
		//			targetIterations = cbsRec.ColorBandRecords.Max(x => x.CutOff);
		//			//cbsRec.TargetIterations = targetIterations;
		//		}

		//		var serialNum = cbsRec.ColorBandsSerialNumber;
		//		if (serialNum == Guid.Empty)
		//		{
		//			serialNum = Guid.NewGuid();
		//		}

		//		var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", cbsRec.Id);

		//		var updateDefinition = Builders<ColorBandSetRecord>.Update
		//			.Set(u => u.DateCreatedUtc, cbsRec.Id.CreationTime)
		//			.Set(u => u.TargetIterations, targetIterations)
		//			.Set(u => u.ColorBandsSerialNumber, serialNum);


		//		_ = Collection.UpdateOne(filter, updateDefinition);

		//	}

		//	return colorBandSetRecords.Count;
		//}

		//public int UpdateColorBandSetSchema()
		//{
		//	var colorBandSetRecords = GetAll().ToList();

		//	var filter1 = Builders<ColorBandSetRecord>.Filter.Empty;

		//	var updateDefinition = Builders<ColorBandSetRecord>.Update
		//		.Unset(u => u.ProjectId)
		//		.Unset(u => u.LastAccessed);

		//	_ = Collection.UpdateMany(filter1, updateDefinition);

		//	return colorBandSetRecords.Count;
		//}
	}
}
