using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectRepo
{
	public class MapSectionZValuesReaderWriter : MongoDbCollectionBase<MapSectionZValuesRecord>
	{
		#region Constructor and Collection Support

		private const string COLLECTION_NAME = "MapSectionZValues";

		public MapSectionZValuesReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public void CreateSectionIdIndex()
		{
			var indexKeysDef = Builders<MapSectionZValuesRecord>.IndexKeys
				.Ascending(x => x.MapSectionId);

			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<MapSectionZValuesRecord>(indexKeysDef, new CreateIndexOptions() { Unique = true, Name = "SectionId" }));
		}

		#endregion

		public async Task<MapSectionZValuesRecord?> GetBySectionIdAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var filter = Builders<MapSectionZValuesRecord>.Filter.Eq("MapSectionId", mapSectionId);

			var mapSectionRecord = await Collection.FindAsync(filter, options: null, ct);

			var itemsFound = mapSectionRecord.ToList();

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				result.LastAccessed = DateTime.UtcNow;
				return result;
			}
			else
			{
				// Log: MapSection Not Found
				//Debug.WriteLine("The MapSectionZValues record could not be found using the MapSectionId.");
				return default;
			}
		}

		public async Task<MapSectionZValuesRecord?> GetAsync(ObjectId mapSectionZValuesId, CancellationToken ct)
		{
			var filter = Builders<MapSectionZValuesRecord>.Filter.Eq("_id", mapSectionZValuesId);

			var mapSectionRecord = await Collection.FindAsync(filter, options: null, ct);

			var itemsFound = mapSectionRecord.ToList();

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				result.LastAccessed = DateTime.UtcNow;
				return result;
			}
			else
			{
				// Log: MapSection Not Found
				//Debug.WriteLine("The MapSectionZValues record could not be found by id.");
				return default;
			}
		}

		public async Task<ObjectId> InsertAsync(MapSectionZValuesRecord mapSectionZValuesRecord)
		{
			//if (mapSectionZValuesRecord.ZValues == null)
			//{
			//	Debug.WriteLine("Inserting a MapSectionRecord that has a null ZValue.");
			//}

			try
			{
				mapSectionZValuesRecord.LastSavedUtc = DateTime.UtcNow;
				await Collection.InsertOneAsync(mapSectionZValuesRecord);
				return mapSectionZValuesRecord.Id;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception {e} on Inserting a MapSectionZValuesRecord.");
				throw;
			}
		}

		public async Task<long?> UpdateZValuesByMapSectionIdAync(MapSectionZValuesRecord mapSectionRecord, ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionZValuesRecord>.Filter.Eq("MapSectionId", mapSectionId);

			UpdateDefinition<MapSectionZValuesRecord> updateDefinition;

			updateDefinition = Builders<MapSectionZValuesRecord>.Update
				.Set(u => u.ZValues, mapSectionRecord.ZValues)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			var result = await Collection.UpdateOneAsync(filter, updateDefinition);

			return result?.ModifiedCount;
		}

		public long? Delete(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionZValuesRecord>.Filter.Eq("_id", mapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? Delete(IList<ObjectId> mapSectionIds)
		{
			var filter = Builders<MapSectionZValuesRecord>.Filter.In(u => u.Id, mapSectionIds);
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

			var filter = Builders<MapSectionZValuesRecord>.Filter.Gt("DateCreatedUtc", dateCreatedUtc);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult);
		}



	}
}
