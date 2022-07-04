using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
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

		public async Task<MapSectionRecord?> GetAsync(ObjectId subdivisionId, BigVectorDto blockPosition)
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

		public async Task<bool?> GetIsV1Async(ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var filter1 = Builders<BsonDocument>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<BsonDocument>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<BsonDocument>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<BsonDocument>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<BsonDocument>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			var bDoc = await BsonDocumentCollection.FindAsync(filter1 & filter2 & filter3 & filter4 & filter5);

			var itemsFound = bDoc.ToList();

			if (itemsFound.Count == 1)
			{
				var test = itemsFound[0].GetValue("EscapeVelocities", BsonNull.Value);
				return test.IsBsonNull;
			}
			else if (itemsFound.Count > 1)
			{
				throw new InvalidOperationException("There should only be as single MapSection record.");
			}
			else
			{
				return null;
			}
		}

		public async Task<MapSectionRecordJustCounts?> GetJustCountsAsync(ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var projection1 = Builders<MapSectionRecord>.Projection.Expression
				(
					p => new MapSectionRecordJustCounts(p.Id, p.DateCreatedUtc, p.SubdivisionId, p.BlockPosXHi, p.BlockPosXLo, p.BlockPosYHi, p.BlockPosYLo, p.MapCalcSettings, p.Counts, p.EscapeVelocities, p.DoneFlags)
				);

			var filter1 = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			IFindFluent<MapSectionRecord, MapSectionRecordJustCounts> operation = Collection.Find(filter1 & filter2 & filter3 & filter4 & filter5).Project(projection1);

			var itemsFound = await operation.ToListAsync().ConfigureAwait(false);

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

		public async Task<ZValues?> GetZValuesAsync(ObjectId mapSectionId)
		{
			var projection1 = Builders<MapSectionRecord>.Projection.Expression
				(
					p => new ZValuesRecord(p.ZValues)
				);

			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);

			IFindFluent<MapSectionRecord, ZValuesRecord> operation = Collection.Find(filter).Project(projection1);

			var itemsFound = await operation.ToListAsync().ConfigureAwait(false);

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				return result.ZValues;
			}
			else
			{
				//Debug.WriteLine("MapSection Not found.");
				return default;
			}
		}

		public async Task<ObjectId> InsertAsync(MapSectionRecord mapSectionRecord)
		{
			try
			{
				mapSectionRecord.LastSavedUtc = DateTime.UtcNow;
				await Collection.InsertOneAsync(mapSectionRecord);
				return mapSectionRecord.Id;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception {e}.");
				throw;
			}
		}

		public async Task<long?> UpdateZValuesAync(MapSectionRecord mapSectionRecord)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionRecord.Id);

			UpdateDefinition<MapSectionRecord> updateDefinition;

			if (mapSectionRecord.DoneFlags.Length == 0)
			{
				updateDefinition = Builders<MapSectionRecord>.Update
					.Set(u => u.MapCalcSettings.TargetIterations, mapSectionRecord.MapCalcSettings.TargetIterations)
					.Set(u => u.Counts, mapSectionRecord.Counts)
					.Set(u => u.EscapeVelocities, mapSectionRecord.EscapeVelocities)
					.Set(u => u.LastSavedUtc, DateTime.UtcNow);
			}
			else
			{
				updateDefinition = Builders<MapSectionRecord>.Update
					.Set(u => u.MapCalcSettings.TargetIterations, mapSectionRecord.MapCalcSettings.TargetIterations)
					.Set(u => u.Counts, mapSectionRecord.Counts)
					.Set(u => u.EscapeVelocities, mapSectionRecord.EscapeVelocities)
					.Set(u => u.DoneFlags, mapSectionRecord.DoneFlags)
					.Set(u => u.ZValues, mapSectionRecord.ZValues)
					.Set(u => u.LastSavedUtc, DateTime.UtcNow);
			}

			var result = await Collection.UpdateOneAsync(filter, updateDefinition);

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

		public long? DeleteMapSectionsSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			if (!overrideRecentGuard && DateTime.UtcNow - dateCreatedUtc > TimeSpan.FromHours(3))
			{
				Debug.WriteLine($"Warning: Not deleting MapSections created since: {dateCreatedUtc}, {dateCreatedUtc} is longer than 3 hours ago.");
				return 0;
			}

			var filter = Builders<MapSectionRecord>.Filter.Gt("DateCreatedUtc", dateCreatedUtc);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult);
		}

		public async Task<int> UpdateToNewVersionAsync(MapSectionRecord mapSectionRecord)
		{
			var filter = Builders<BsonDocument>.Filter.Eq("_id", mapSectionRecord.Id);

			var bsonDocCollection = BsonDocumentCollection;
			var records = await bsonDocCollection.FindAsync(filter);

			var mapSectionRecords = records.ToList();

			if (mapSectionRecords.Count == 1)
			{
				var updateDefinition = Builders<BsonDocument>.Update
					.Set("Counts", mapSectionRecord.Counts)
					.Set("EscapeVelocities", mapSectionRecord.EscapeVelocities)
					.Set("DoneFlags", mapSectionRecord.DoneFlags);
					//.Set("ZValues", mapSectionRecord.ZValues);

				_ = await bsonDocCollection.UpdateOneAsync(filter, updateDefinition);

				return 1;
			}
			else if (mapSectionRecords.Count > 1)
			{
				throw new InvalidOperationException($"There should only be one MapSectionRecord with Id: {mapSectionRecord.Id}.");
			}
			else
			{
				return 0;
			}
		}

		public async Task<ObjectId?> GetId(ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var filter1 = Builders<BsonDocument>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<BsonDocument>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<BsonDocument>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<BsonDocument>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<BsonDocument>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			var bDoc = await BsonDocumentCollection.FindAsync(filter1 & filter2 & filter3 & filter4 & filter5);

			var itemsFound = bDoc.ToList();

			if (itemsFound.Count == 1)
			{
				var test = itemsFound[0].GetValue("_id");
				if (test.IsObjectId)
				{
					return test.AsObjectId;
				}
				else
				{
					return null;
				}
			}
			else if (itemsFound.Count > 1)
			{
				throw new InvalidOperationException("There should only be as single MapSection record.");
			}
			else
			{
				return null;
			}
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

		public long RemoveFetchZValuesProp()
		{
			var filter = Builders<MapSectionRecord>.Filter.Empty;
			var updateDefinition = Builders<MapSectionRecord>.Update
				.Unset("MapCalcSettings.FetchZValues")
				.Unset("MapCalcSettings.DontFetchZValuesFromRepo")
				.Unset("MapCalcSettings.DontFetchZValues");
			var options = new UpdateOptions { IsUpsert = false };

			var updateResult = Collection.UpdateMany(filter, updateDefinition, options);

			return GetReturnCount(updateResult) ?? -1;
		}

	}
}
