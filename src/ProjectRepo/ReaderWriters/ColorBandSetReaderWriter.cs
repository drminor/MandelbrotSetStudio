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
		#region Constructor and Collection Support

		private const string COLLECTION_NAME = "ColorBandSets";

		public ColorBandSetReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

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
			colorBandSetRecord.DateRecordLastSavedUtc = DateTime.UtcNow;
			Collection.InsertOne(colorBandSetRecord);
			return colorBandSetRecord.Id;
		}

		public void UpdateName(ObjectId colorBandSetId, string? name)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.Name, name)
				.Set(u => u.DateRecordLastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateDescription(ObjectId colorBandSetId, string? description)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.Description, description)
				.Set(u => u.DateRecordLastSavedUtc, DateTime.UtcNow);


			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateDetails(ColorBandSet colorBandSet)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSet.Id);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.OwnerId, colorBandSet.OwnerId)
				.Set(u => u.ParentId, colorBandSet.ParentId)
				.Set(u => u.TargetIterations, colorBandSet.TargetIterations)
				.Set(u => u.DateCreatedUtc, colorBandSet.DateCreatedUtc)
				.Set(u => u.DateLastUsedUtc, colorBandSet.DateRecordLastUsedUtc)
				.Set(u => u.DateRecordLastSavedUtc, DateTime.UtcNow);


			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateBands(ColorBandSet colorBandSet)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSet.Id);

			var updateDefinition = Builders<ColorBandSetRecord>.Update
				.Set(u => u.ColorBandRecords, colorBandSet.Select(x => new ColorBandRecord(x.Cutoff, x.StartColor.GetCssColor(), x.BlendStyle.ToString(), x.EndColor.GetCssColor(), x.Percentage)).ToArray())
				.Set(u => u.TargetIterations, colorBandSet.TargetIterations)
				.Set(u => u.ReservedColorBandRecords, colorBandSet.GetReservedColorBands().Select(x => new ReservedColorBandRecord(x.StartColor.GetCssColor(), x.BlendStyle.ToString(), x.EndColor.GetCssColor())).ToArray())
				.Set(u => u.UsingPercentages, colorBandSet.UsingPercentages)
				.Set(u => u.DateLastUsedUtc, colorBandSet.DateRecordLastUsedUtc)
				.Set(u => u.DateRecordLastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		//public void UpdateProjectId(ObjectId colorBandSetId, ObjectId projectId)
		//{
		//	var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

		//	var updateDefinition = Builders<ColorBandSetRecord>.Update
		//		.Set(u => u.ProjectId, projectId);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		//public void UpdateParentId(ObjectId colorBandSetId, ObjectId? parentId)
		//{
		//	var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

		//	var updateDefinition = Builders<ColorBandSetRecord>.Update
		//		.Set(u => u.ParentId, parentId);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		//public void UpdateColorBands(ObjectId colorBandSetId, ColorBandRecord[] colorBandsRecords)
		//{
		//	var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);

		//	var updateDefinition = Builders<ColorBandSetRecord>.Update
		//		.Set(u => u.ColorBandRecords, colorBandsRecords);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		public long Delete(ObjectId colorBandSetId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("_id", colorBandSetId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult) ?? 0;
		}

		public IEnumerable<ObjectId> GetColorBandSetIdsForProject(ObjectId projectId)
		{
			var projection1 = Builders<ColorBandSetRecord>.Projection.Expression(p => p.Id);

			var filter = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, projectId);
			var colorBandSetIds = Collection.Find(filter).Project(projection1).ToList();

			return colorBandSetIds;
		}

		public IEnumerable<ColorBandSetRecord> GetColorBandSetsForProject(ObjectId projectId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, projectId);
			var colorBandSets = Collection.Find(filter).ToList();

			return colorBandSets;
		}

		public long DeleteColorBandSetsForProject(ObjectId projectId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, projectId);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult) ?? 0;
		}

		public bool Exists(string name)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("Name", name);
			var colorBandSetRecord = Collection.Find(filter).FirstOrDefault();
			var result = colorBandSetRecord != null;

			return result;
		}

		public bool Exists(ObjectId projectId, string name, int targetIterations)
		{
			var filter1 = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, projectId);
			var filter2 = Builders<ColorBandSetRecord>.Filter.Eq("Name", name);
			var filter3 = Builders<ColorBandSetRecord>.Filter.Eq("TargetIterations", targetIterations);

			var colorBandSetRecord = Collection.Find(filter1 & filter2 & filter3).FirstOrDefault();
			var result = colorBandSetRecord != null;

			return result;
		}

	}
}
