﻿using MongoDB.Bson;
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
		private const string COLLECTION_NAME = "JobMapSections";

		public JobMapSectionReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

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

		public IList<JobMapSectionRecord> DoJobMapSectionRecordsExist(IList<ObjectId> jobMapSectionIds)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.In(f => f.MapSectionId, jobMapSectionIds);

			var jobMapSectionRecords = Collection.Find(filter1).ToList();

			return jobMapSectionRecords;
		}

		public IList<JobMapSectionRecord> GetByOwnerId(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var jobMapSectionRecords = Collection.Find(filter1 & filter2).ToList();

			return jobMapSectionRecords;
		}


		public async Task<IList<JobMapSectionRecord>> GetByOwnerIdAsync(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerId, ownerId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var resultCursor = await Collection.FindAsync(filter1 & filter2).ConfigureAwait(false);
			var jobMapSectionRecords = await resultCursor.ToListAsync().ConfigureAwait(false);

			return jobMapSectionRecords;
		}

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

	}
}
