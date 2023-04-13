using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectRepo
{
	public class JobMapSectionReaderWriter : MongoDbCollectionBase<JobMapSectionRecord>
	{
		#region Constructor and Collection Support

		private const string COLLECTION_NAME = "JobMapSections";

		public JobMapSectionReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		// TODO: Since every JobMapSectionRecord belongs to one and only one Job, do we really need to index on OwnerType? Pretty sure that the answer is no.
		public void CreateOwnerAndTypeIndex()
		{
			var indexKeysDef = Builders<JobMapSectionRecord>.IndexKeys
				.Ascending(x => x.OwnerId)
				.Ascending(x => x.OwnerType);

			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<JobMapSectionRecord>(indexKeysDef, new CreateIndexOptions() { Unique = false, Name = "OwnerAndType" }));
		}

		public void CreateMapSectionIdIndex()
		{
			var indexKeysDef = Builders<JobMapSectionRecord>.IndexKeys
				.Ascending(x => x.MapSectionId);

			var idx = Collection.Indexes.CreateOne(new CreateIndexModel<JobMapSectionRecord>(indexKeysDef, new CreateIndexOptions() { Unique = false, Name = "MapSectionId" }));
		}

		#endregion

		#region Get

		public JobMapSectionRecord? Get(ObjectId jobMapSectionId)
		{
			var filter = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, jobMapSectionId);
			var jobMapSectionRecord = Collection.Find(filter).FirstOrDefault();

			return jobMapSectionRecord;
		}

		public IList<JobMapSectionRecord> GetByMapSectionId(ObjectId mapSectionId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.MapSectionId, mapSectionId);
			var jobMapSectionRecords = Collection.Find(filter1).ToList();

			return jobMapSectionRecords;
		}

		public bool DoesJobMapSectionRecordExist(ObjectId mapSectionId)
		{
			//var countOptions = new CountOptions { Limit = 1 };
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.MapSectionId, mapSectionId);
			//var count = Collection.CountDocuments(filter1, countOptions);
			var count = Collection.CountDocuments(filter1);
			return count > 0;
		}

		public IEnumerable<ObjectId> DoJobMapSectionRecordsExist(IEnumerable<ObjectId> mapSectionIds)
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => p.MapSectionId);
			var filter1 = Builders<JobMapSectionRecord>.Filter.In(f => f.MapSectionId, mapSectionIds);

			var foundMapSectionIds = Collection.Find(filter1).Project(projection1).ToList().Distinct();

			return foundMapSectionIds;
		}

		public IList<JobMapSectionRecord> GetByOwnerId(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var jobMapSectionRecords = Collection.Find(filter1 & filter2).ToList();

			return jobMapSectionRecords;
		}

		public IList<ObjectId> GetMapSectionIdsByOwnerId(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var jobMapSectionRecordIds = Collection.Find(filter1 & filter2).Project(x => x.MapSectionId).ToList();

			return jobMapSectionRecordIds;
		}

		public async Task<IList<JobMapSectionRecord>> GetByOwnerIdAsync(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var resultCursor = await Collection.FindAsync(filter1 & filter2).ConfigureAwait(false);
			var jobMapSectionRecords = await resultCursor.ToListAsync().ConfigureAwait(false);

			return jobMapSectionRecords;
		}

		public async Task<JobMapSectionRecord?> GetByMapAndOwnerIdAsync(ObjectId mapSectionId, ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.MapSectionId, mapSectionId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter3 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var resultCursor = await Collection.FindAsync(filter1 & filter2 & filter3).ConfigureAwait(false);
			var jobMapSectionRecords = await resultCursor.ToListAsync().ConfigureAwait(false);

			return jobMapSectionRecords.FirstOrDefault();
		}

		#endregion

		#region Insert

		public async Task<ObjectId> InsertAsync(JobMapSectionRecord jobMapSectionRecord)
		{
			if (jobMapSectionRecord.Onfile)
			{
				throw new InvalidOperationException("Cannot insert a JobMapSectionRecord that is already OnFile.");
			}

			jobMapSectionRecord.Id = ObjectId.GenerateNewId();
			jobMapSectionRecord.LastSaved = DateTime.UtcNow;

			await Collection.InsertOneAsync(jobMapSectionRecord).ConfigureAwait(false);
			return jobMapSectionRecord.Id;
		}

		public ObjectId Insert(JobMapSectionRecord jobMapSectionRecord)
		{
			if (jobMapSectionRecord.Onfile)
			{
				throw new InvalidOperationException("Cannot insert a JobMapSectionRecord that is already OnFile.");
			}

			jobMapSectionRecord.Id = ObjectId.GenerateNewId();
			jobMapSectionRecord.LastSaved = DateTime.UtcNow;

			Collection.InsertOne(jobMapSectionRecord);
			return jobMapSectionRecord.Id;
		}

		#endregion

		#region Delete

		public long? Delete(ObjectId jobMapSectionId)
		{
			var filter = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, jobMapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public async Task<long?> DeleteJobMapSectionsAsync(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var deleteResult = await Collection.DeleteManyAsync(filter1 & filter2).ConfigureAwait(false);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteJobMapSections(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var deleteResult = Collection.DeleteMany(filter1 & filter2);

			return GetReturnCount(deleteResult);
		}

		#endregion

		#region Aggregate

		public IEnumerable<Tuple<ObjectId, ObjectId>> GetAllMapSectionIdsFromJobMapSections()
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => new Tuple<ObjectId, ObjectId>(p.Id, p.MapSectionId));
			var filter1 = Builders<JobMapSectionRecord>.Filter.Empty;

			var mapSectionIds = Collection.Find(filter1).Project(projection1).ToEnumerable();

			return mapSectionIds;
		}

		public IList<ObjectId> GetDistinctJobIdsFromJobMapSections(JobOwnerType jobOwnerType)
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => p.OwnerId);
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var ownerIds = Collection.Find(filter1).Project(projection1).ToList().Distinct().ToList();

			return ownerIds;
		}

		#endregion

		#region Maintenance

		//public void AddSubdivisionIdToAllRecords()
		//{
		//	var filter = Builders<JobMapSectionRecord>.Filter.Empty;
		//	var updateDefinition = Builders<JobMapSectionRecord>.Update
		//		.Set(f => f.SubdivisionId, ObjectId.GenerateNewId());

		//	var options = new UpdateOptions { IsUpsert = false };

		//	_ = Collection.UpdateMany(filter, updateDefinition, options);
		//}

		//public IEnumerable<JobMapSectionRecord> GetAllJobMapSections()
		//{
		//	var filter = Builders<JobMapSectionRecord>.Filter.Empty;
		//	var jobMapSectionRecords = Collection.Find(filter).ToEnumerable();

		//	return jobMapSectionRecords;
		//}

		//public void SetSubdivisionId(ObjectId mapSectionId, ObjectId subdivisionId)
		//{
		//	var filter = Builders<JobMapSectionRecord>.Filter.Eq("_id", mapSectionId);

		//	var updateDefinition = Builders<JobMapSectionRecord>.Update
		//		.Set(u => u.SubdivisionId, subdivisionId);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		#endregion
	}
}
