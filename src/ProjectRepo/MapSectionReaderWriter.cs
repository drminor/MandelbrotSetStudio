using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProjectRepo
{
	public class MapSectionReaderWriter : MongoDbCollectionBase<MapSectionRecord>
	{
		private const string COLLECTION_NAME = "MapSections";

		public MapSectionReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public override bool CreateCollection()
		{
			if (base.CreateCollection())
			{
				CreateSubAndPosIndex();
				return true;
			}
			else
			{
				return false;
			}
		}

		public void CreateSubAndPosIndex()
		{
			var indexKeysDef = Builders<MapSectionRecord>.IndexKeys.Ascending(x => x.SubdivisionId).Ascending(x => x.BlockPosXLo).Ascending(x => x.BlockPosYLo).Ascending(x => x.BlockPosXHi).Ascending(x => x.BlockPosYHi);
			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<MapSectionRecord>(indexKeysDef, new CreateIndexOptions() { Unique = true, Name = "SubAndPos" }));
		}

		public MapSectionRecord? Get(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var mapSectionRecord = Collection.Find(filter);

			var result = mapSectionRecord.FirstOrDefault();

			if (result != null)
			{
				result.LastAccessed = DateTime.UtcNow;
			}

			return result;
		}

		public async Task<MapSectionRecord?> GetAsync(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var mapSectionRecord = await Collection.FindAsync(filter);

			var result = mapSectionRecord.FirstOrDefault();

			if (result != null)
			{
				result.LastAccessed = DateTime.UtcNow;
			}

			return result;
		}

		public async Task<MapSectionRecord?> GetAsync (ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var filter1 = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			var mapSectionRecord = await Collection.FindAsync(filter1 & filter2 & filter3 & filter4 & filter5);

			var itemsFound = mapSectionRecord.ToList();

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				result.LastAccessed = DateTime.UtcNow;
				return result;
			}
			else
			{
				//Debug.WriteLine("MapSection Not found.");
				return default;
			}
		}

		public MapSectionRecord? Get(ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var filter1 = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			var mapSectionRecord = Collection.Find(filter1 & filter2 & filter3 & filter4 & filter5);

			var itemsFound = mapSectionRecord.ToList();

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				result.LastAccessed = DateTime.UtcNow;
				return result;
			}
			else
			{
				//Debug.WriteLine("MapSection Not found.");
				return default;
			}
		}

		public MapSectionRecordJustCounts? GetJustCounts(ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var projection1 = Builders<MapSectionRecord>.Projection.Expression
				(
					p => new MapSectionRecordJustCounts(p.DateCreatedUtc, p.SubdivisionId, p.BlockPosXHi, p.BlockPosXLo, p.BlockPosYHi, p.BlockPosYLo, p.MapCalcSettings, p.Counts)
				);

			var filter1 = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			var mapSectionRecordJc = Collection.Find(filter1 & filter2 & filter3 & filter4 & filter5).Project(projection1);

			var itemsFound = mapSectionRecordJc.ToList();

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				result.LastAccessed = DateTime.UtcNow;
				return result;
			}
			else
			{
				//Debug.WriteLine("MapSection Not found.");
				return default;
			}
		}

		public async Task<ObjectId> InsertAsync(MapSectionRecord mapSectionRecord)
		{
			mapSectionRecord.LastSavedUtc = DateTime.UtcNow;
			await Collection.InsertOneAsync(mapSectionRecord);
			return mapSectionRecord.Id;
		}

		public async Task<long?> UpdateZValuesAync(ObjectId mapSectionId, int targetIterations, int[] counts, bool[] doneFlags, double[] zValues)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);

			var updateDefinition = Builders<MapSectionRecord>.Update
				.Set(u => u.MapCalcSettings.TargetIterations, targetIterations)
				.Set(u => u.Counts, counts)
				.Set(u => u.DoneFlags, doneFlags)
				.Set(u => u.ZValues, zValues)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			UpdateResult? result = await Collection.UpdateOneAsync(filter, updateDefinition);

			return result?.ModifiedCount;
		}

		public long? Delete(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteAllWithSubId(ObjectId subdivisionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteMapSectionsSince(DateTime lastSaved, bool overrideRecentGuard = false)
		{
			if (!overrideRecentGuard && DateTime.UtcNow - lastSaved > TimeSpan.FromHours(3))
			{
				Debug.WriteLine($"Warning: Not deleting MapSections created since: {lastSaved}, {lastSaved} is longer than 3 hours ago.");
				return 0;
			}

			var filter = Builders<MapSectionRecord>.Filter.Gt("DateCreatedUtc", lastSaved);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult);
		}

		//public void AddCreatedDateToAllRecords()
		//{
		//	var filter = Builders<MapSectionRecord>.Filter.Empty;
		//	var updateDefinition = Builders<MapSectionRecord>.Update
		//		.Set("DateCreatedUtc", DateTime.UtcNow)
		//		.Set("LastSavedUtc", DateTime.MinValue)
		//		.Set("LastAccessed", DateTime.MinValue);
		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

	}
}
