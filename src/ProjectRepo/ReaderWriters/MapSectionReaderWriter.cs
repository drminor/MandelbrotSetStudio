﻿using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectRepo
{
	public class MapSectionReaderWriter : MongoDbCollectionBase<MapSectionRecord>
	{
		#region Constructor and Collection Support

		private const string COLLECTION_NAME = "MapSections";

		public MapSectionReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public override bool CreateCollection()
		{
			if (base.CreateCollection())
			{
				//CreateSubAndPosIndex();
				return true;
			}
			else
			{
				return false;
			}
		}

		public void CreateSubAndPosIndex()
		{
			var indexKeysDef = Builders<MapSectionRecord>.IndexKeys
				.Ascending(x => x.SubdivisionId)
				.Ascending(x => x.BlockPosXLo)
				.Ascending(x => x.BlockPosYLo)
				.Ascending(x => x.BlockPosXHi)
				.Ascending(x => x.BlockPosYHi);

			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<MapSectionRecord>(indexKeysDef, new CreateIndexOptions() { Unique = true, Name = "SubAndPos" }));
		}

		#endregion

		public async Task<MapSectionRecord?> GetAsync(ObjectId subdivisionId, BigVectorDto blockPosition, CancellationToken ct)
		{
			var filter1 = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<MapSectionRecord>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<MapSectionRecord>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);


			var mapSectionRecord = await Collection.FindAsync(filter1 & filter2 & filter3 & filter4 & filter5, options: null, ct);

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
				//Debug.WriteLine("MapSection Not found.");
				return default;
			}
		}

		public async Task<ObjectId> InsertAsync(MapSectionRecord mapSectionRecord)
		{
			try
			{
				var blockPos = GetBlockPos(mapSectionRecord);
				var id = await GetId(mapSectionRecord.SubdivisionId, blockPos);
				if (id != null)
				{
					Debug.WriteLine($"Not Inserting MapSectionRecord with BlockPos: {blockPos}. A record already exists for this block position with Id: {id}.");
					return id.Value;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception {e} while Calling GetId on Inserting a MapSectionRecord.");
				throw;
			}

			try
			{
				mapSectionRecord.LastSavedUtc = DateTime.UtcNow;
				await Collection.InsertOneAsync(mapSectionRecord);
				return mapSectionRecord.Id;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception {e} on Inserting a MapSectionRecord.");
				throw;
			}
		}

		public async Task<long?> UpdateCountValuesAync(MapSectionRecord mapSectionRecord)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionRecord.Id);

			UpdateDefinition<MapSectionRecord> updateDefinition;

			updateDefinition = Builders<MapSectionRecord>.Update
				.Set(u => u.MapCalcSettings.TargetIterations, mapSectionRecord.MapCalcSettings.TargetIterations)
				.Set(u => u.Counts, mapSectionRecord.Counts)
				//.Set(u => u.EscapeVelocities, mapSectionRecord.EscapeVelocities)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);
			var result = await Collection.UpdateOneAsync(filter, updateDefinition);

			return result?.ModifiedCount;
		}

		public long? Delete(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? Delete(IList<ObjectId> mapSectionIds)
		{
			var filter = Builders<MapSectionRecord>.Filter.In(u => u.Id, mapSectionIds);
			var deleteResult = Collection.DeleteMany(filter);

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

		public IEnumerable<ObjectId> GetAllMapSectionIds()
		{
			var projection1 = Builders<MapSectionRecord>.Projection.Expression(p => p.Id);

			var filter = Builders<MapSectionRecord>.Filter.Empty;

			IFindFluent<MapSectionRecord, ObjectId> operation = Collection.Find(filter).Project(projection1);

			var itemsFound = operation.ToEnumerable();
			return itemsFound;
		}

		private BigVectorDto GetBlockPos(MapSectionRecord msr)
		{
			var blockPosition = new BigVectorDto(new long[][]
				{
					new long[] { msr.BlockPosXHi, msr.BlockPosXLo },
					new long[] { msr.BlockPosYHi, msr.BlockPosYLo }
				});

			return blockPosition;
		}

		//public void RemoveEscapeVelsFromMapSectionRecords()
		//{
		//	var filter = Builders<MapSectionRecord>.Filter.Empty;

		//	var updateDefinition = Builders<MapSectionRecord>.Update
		//		.Unset(f => f.MapCalcSettings.UseEscapeVelocities);

		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

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

		//public long RemoveFetchZValuesProp()
		//{
		//	var filter = Builders<MapSectionRecord>.Filter.Empty;
		//	var updateDefinition = Builders<MapSectionRecord>.Update
		//		.Unset("MapCalcSettings.FetchZValues")
		//		.Unset("MapCalcSettings.DontFetchZValuesFromRepo")
		//		.Unset("MapCalcSettings.DontFetchZValues");
		//	var options = new UpdateOptions { IsUpsert = false };

		//	var updateResult = Collection.UpdateMany(filter, updateDefinition, options);

		//	return GetReturnCount(updateResult) ?? -1;
		//}

	}
}
