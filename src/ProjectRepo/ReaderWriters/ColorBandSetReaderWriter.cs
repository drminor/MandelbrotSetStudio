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

		public void CreateOwnerNameIterationsAndVersionIndex()
		{
			var indexKeysDef = Builders<ColorBandSetRecord>.IndexKeys
				.Ascending(x => x.OwnerId)
				.Ascending(x => x.Name)
				.Ascending(x => x.TargetIterations)
				.Ascending(x => x.Version);

			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<ColorBandSetRecord>(indexKeysDef, new CreateIndexOptions() { Unique = true, Name = "OwnerNameIterationsAndVersion" }));
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
			try
			{
				colorBandSetRecord.DateRecordLastSavedUtc = DateTime.UtcNow;
				Collection.InsertOne(colorBandSetRecord);
				return colorBandSetRecord.Id;
			}
			catch
			{
				throw;
			}
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
				.Set(u => u.ColorBandRecords, colorBandSet.Select(x => CreateColorBandRecord(x)).ToArray())
				.Set(u => u.TargetIterations, colorBandSet.TargetIterations)
				.Set(u => u.ReservedColorBandRecords, colorBandSet.GetReservedColorBands().Select(x => CreateReservedColorBandRecord(x)).ToArray())
				.Set(u => u.UsingPercentages, colorBandSet.UsingPercentages)
				.Set(u => u.DateLastUsedUtc, colorBandSet.DateRecordLastUsedUtc)
				.Set(u => u.DateRecordLastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		private ColorBandRecord CreateColorBandRecord(ColorBand colorBand)
		{
			var result = new ColorBandRecord(colorBand.Cutoff, colorBand.StartColor.GetCssColor(), colorBand.BlendStyle.ToString(), colorBand.EndColor.GetCssColor(), colorBand.Percentage);
			result.BlendMethod = colorBand.BlendMethod.ToString();

			return result;
		}

		private ReservedColorBandRecord CreateReservedColorBandRecord(ReservedColorBand reservedColorBand)
		{
			var result = new ReservedColorBandRecord(reservedColorBand.StartColor.GetCssColor(), reservedColorBand.BlendStyle.ToString(), reservedColorBand.EndColor.GetCssColor());
			result.BlendMethod = reservedColorBand.BlendMethod.ToString();
			return result;
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

		public IEnumerable<ObjectId> GetColorBandSetIdsForOwner(ObjectId ownerId)
		{
			var projection1 = Builders<ColorBandSetRecord>.Projection.Expression(p => p.Id);

			var filter = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, ownerId);
			var colorBandSetIds = Collection.Find(filter).Project(projection1).ToList();

			return colorBandSetIds;
		}

		public IEnumerable<ColorBandSetRecord> GetColorBandSetsForOwner(ObjectId ownerId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, ownerId);
			var colorBandSets = Collection.Find(filter).ToList();

			return colorBandSets;
		}

		public long DeleteColorBandSetsForOwner(ObjectId ownerId)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, ownerId);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult) ?? 0;
		}

		public long DeleteColorBandSetsByVersion(ObjectId ownerId, string name, int targetIterations, int numberToKeep)
		{
			var filter1 = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, ownerId);
			var filter2 = Builders<ColorBandSetRecord>.Filter.Eq(u => u.Name, name);
			var filter3 = Builders<ColorBandSetRecord>.Filter.Eq(u => u.TargetIterations, targetIterations);

			var colorBandSets = Collection.Find(filter1 & filter2 & filter3).ToEnumerable().OrderByDescending(x => x.Version).Skip(numberToKeep);

			var numberDeleted = 0L;

			foreach(var cb in colorBandSets)
			{
				numberDeleted += Delete(cb.Id);
			}

			return numberDeleted;
		}

		public bool Exists(string name)
		{
			var filter = Builders<ColorBandSetRecord>.Filter.Eq("Name", name);
			var colorBandSetRecord = Collection.Find(filter).FirstOrDefault();
			var result = colorBandSetRecord != null;

			return result;
		}

		public bool Exists(ObjectId ownerId, string name, int targetIterations)
		{
			var filter1 = Builders<ColorBandSetRecord>.Filter.Eq(u => u.OwnerId, ownerId);
			var filter2 = Builders<ColorBandSetRecord>.Filter.Eq("Name", name);
			var filter3 = Builders<ColorBandSetRecord>.Filter.Eq("TargetIterations", targetIterations);

			var colorBandSetRecord = Collection.Find(filter1 & filter2 & filter3).FirstOrDefault();
			var result = colorBandSetRecord != null;

			return result;
		}

		//public int UpdateColorBandSetSchema()
		//{
		//	var colorBandSetRecords = GetAll().ToList();

		//	var filter1 = Builders<ColorBandSetRecord>.Filter.Empty;

		//	var updateDefinition = Builders<ColorBandSetRecord>.Update
		//		.Unset(u => u.ProjectId)
		//		.Unset(u => u.LastAccessed)
		//		.Set("Version", 0);

		//	_ = Collection.UpdateMany(filter1, updateDefinition);

		//	return colorBandSetRecords.Count;
		//}

	}
}
