using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				.Ascending(x => x.JobId)
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

		public IEnumerable<JobMapSectionRecord> GetAll()
		{
			var filter = Builders<JobMapSectionRecord>.Filter.Empty;
			var result = Collection.Find(filter).ToEnumerable();

			return result;
		}

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

		public IEnumerable<ObjectId> GetJobMapSectionIds(IEnumerable<ObjectId> mapSectionIds)
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => p.MapSectionId);
			var filter1 = Builders<JobMapSectionRecord>.Filter.In(f => f.MapSectionId, mapSectionIds);

			var foundMapSectionIds = Collection.Find(filter1).Project(projection1).ToList().Distinct();

			return foundMapSectionIds;
		}

		public List<JobMapSectionRecord> GetByJobId(ObjectId jobId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			//var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var jobMapSectionRecords = Collection.Find(filter1).ToList();

			return jobMapSectionRecords;
		}

		public List<ObjectId> GetMapSectionIdsByJobId(ObjectId jobId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			//var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var jobMapSectionRecordIds = Collection.Find(filter1).Project(x => x.MapSectionId).ToList();

			return jobMapSectionRecordIds;
		}

		public int GetCountOfMapSectionsByJobId(ObjectId jobId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			//var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var numberDistinctOfMapSectionsForJob = Collection.Find(filter1).Project(x => x.MapSectionId).ToList().Distinct().Count();

			return numberDistinctOfMapSectionsForJob;
		}

		public async Task<IList<JobMapSectionRecord>> GetByJobIdAsync(ObjectId jobId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			//var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);
			var resultCursor = await Collection.FindAsync(filter1).ConfigureAwait(false);
			var jobMapSectionRecords = await resultCursor.ToListAsync().ConfigureAwait(false);

			return jobMapSectionRecords;
		}

		public async Task<JobMapSectionRecord?> GetByMapAndJobIdAsync(ObjectId mapSectionId, ObjectId jobId, JobType jobType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.MapSectionId, mapSectionId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			var filter3 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobType, jobType);

			var resultCursor = await Collection.FindAsync(filter1 & filter2 & filter3).ConfigureAwait(false);
			var jobMapSectionRecords = await resultCursor.ToListAsync().ConfigureAwait(false);

			Debug.Assert(jobMapSectionRecords.Count < 2, "Found more than one JobMapSectionRecord for the triplet of JobId, MapSectionId and JobType.");

			return jobMapSectionRecords.FirstOrDefault();
		}

		public JobMapSectionRecord? GetByMapAndJobId(ObjectId mapSectionId, ObjectId jobId, JobType jobType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.MapSectionId, mapSectionId);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			var filter3 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobType, jobType);

			var jobMapSectionRecords = Collection.Find(filter1 & filter2 & filter3).ToList();

			Debug.Assert(jobMapSectionRecords.Count < 2, "Found more than one JobMapSectionRecord for the triplet of JobId, MapSectionId and JobType.");

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
			Collection.InsertOne(jobMapSectionRecord);
			return jobMapSectionRecord.Id;
		}

		#endregion

		#region Update 

		public void SetSubdivisionId(ObjectId jobMapSectionId, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId)
		{
			var filter = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, jobMapSectionId);

			var updateDefinition = Builders<JobMapSectionRecord>.Update
				.Set(u => u.MapSectionSubdivisionId, mapSectionSubdivisionId)
				.Set(u => u.JobSubdivisionId, jobSubdivisionId);


			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		//public void SetOriginalSourceSubdivisionId(ObjectId jobMapSectionId, ObjectId originalSourceSubdivisionId)
		//{
		//	var filter = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, jobMapSectionId);

		//	var updateDefinition = Builders<JobMapSectionRecord>.Update
		//		.Set(u => u.OriginalSourceSubdivisionId, originalSourceSubdivisionId);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		#endregion

		#region Delete

		public long? DeleteJobMapSectionById(ObjectId jobMapSectionId)
		{
			var filter = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, jobMapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public async Task<long?> DeleteJobMapSectionsAsync(ObjectId jobId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			//var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var deleteResult = await Collection.DeleteManyAsync(filter1).ConfigureAwait(false);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteJobMapSections(ObjectId jobId)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.JobId, jobId);
			//var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var deleteResult = Collection.DeleteMany(filter1);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteJobMapSectionsInList(IEnumerable<ObjectId> jobMapSectionIds)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.In(f => f.Id, jobMapSectionIds);

			var deleteResult = Collection.DeleteMany(filter1);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteJobMapSectionsByMapSectionId(IEnumerable<ObjectId> mapSectionIds, OwnerType jobOwnerType)
		{
			var filter1 = Builders<JobMapSectionRecord>.Filter.In(f => f.MapSectionId, mapSectionIds);
			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var deleteResult = Collection.DeleteMany(filter1 & filter2);

			return GetReturnCount(deleteResult);
		}


		public long? DeleteJobMapSectionsSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			if (!overrideRecentGuard && DateTime.UtcNow - dateCreatedUtc > TimeSpan.FromHours(3))
			{
				Debug.WriteLine($"Warning: Not deleting JobMapSections created since: {dateCreatedUtc}, {dateCreatedUtc} is longer than 3 hours ago.");
				return 0;
			}

			var filter = Builders<JobMapSectionRecord>.Filter.Gt("DateCreatedUtc", dateCreatedUtc);
			var deleteResult = Collection.DeleteMany(filter);

			return GetReturnCount(deleteResult);
		}

		#endregion

		#region Aggregate

		public IEnumerable<Tuple<ObjectId, ObjectId>> GetMapSectionIdsFromAllJobMapSections()
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => new Tuple<ObjectId, ObjectId>(p.Id, p.MapSectionId));
			var filter1 = Builders<JobMapSectionRecord>.Filter.Empty;

			var mapSectionIds = Collection.Find(filter1).Project(projection1).ToEnumerable();

			return mapSectionIds;
		}

		public IList<ObjectId> GetDistinctJobIdsFromJobMapSections(OwnerType jobOwnerType)
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => p.JobId);
			var filter1 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.OwnerType, jobOwnerType);

			var jobIds = Collection.Find(filter1).Project(projection1).ToList().Distinct().ToList();

			return jobIds;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> GetMapSectionAndSubdivisionIdsForAllJobMapSections()
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => new ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>(p.Id, p.MapSectionId, p.MapSectionSubdivisionId, p.JobSubdivisionId));

			var filter = Builders<JobMapSectionRecord>.Filter.Empty;

			IFindFluent<JobMapSectionRecord, ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> operation = Collection.Find(filter).Project(projection1);

			var itemsFound = operation.ToEnumerable();
			return itemsFound;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> GetJobAndSubdivisionIdsForAllJobMapSections()
		{
			var projection1 = Builders<JobMapSectionRecord>.Projection.Expression(p => new ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>(p.Id, p.JobId, p.MapSectionSubdivisionId, p.JobSubdivisionId));

			var filter = Builders<JobMapSectionRecord>.Filter.Empty;

			IFindFluent<JobMapSectionRecord, ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> operation = Collection.Find(filter).Project(projection1);

			var itemsFound = operation.ToEnumerable();
			return itemsFound;
		}

		#endregion

		#region Maintenance

		//public void AddJobTypeAndBlockIndex()
		//{
		//	var filter1 = Builders<JobMapSectionRecord>.Filter.Empty;

		//	var allRecords = Collection.Find(filter1).ToEnumerable();

		//	foreach (var rec in allRecords)
		//	{
		//		var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, rec.Id);

		//		var updateDefinition = Builders<JobMapSectionRecord>.Update
		//		.Set(f => f.MapSectionSubdivisionId, rec.SubdivisionId)
		//		.Set(f => f.JobSubdivisionId, rec.OriginalSourceSubdivisionId)

		//		.Set(f => f.JobType, JobType.FullScale)
		//		.Set(f => f.BlockIndex, new SizeIntRecord(0, 0))
		//		.Set(f => f.DateCreatedUtc, rec.DateCreated)
		//		.Unset(f => f.RefIsHard)
		//		.Unset(f => f.SubdivisionId)
		//		.Unset(f => f.OriginalSourceSubdivisionId)
		//		.Unset(f => f.OwnerId)
		//		.Unset(f => f.LastSaved);

		//		var options = new UpdateOptions { IsUpsert = false };

		//		_ = Collection.UpdateOne(filter2, updateDefinition, options);
		//	}
		//}


		//public void ReplaceOwnerIdWithJobId()
		//{
		//	//var dt = DateTime.Parse("Feb 20 2023");

		//	//var objectId = new ObjectId("649a4127f54744d2cc227e65");
		//	//var filter1 = Builders<JobMapSectionRecord>.Filter.Lt(f => f.Id, objectId);

		//	var filter1 = Builders<JobMapSectionRecord>.Filter.Empty;

		//	var earlyRecords = Collection.Find(filter1).ToEnumerable();

		//	foreach (var rec in earlyRecords)
		//	{
		//		if (rec.OwnerId != ObjectId.Empty && rec.JobId != rec.OwnerId)
		//		{
		//			rec.JobId = rec.OwnerId;
		//			rec.LastSavedUtc = rec.LastSaved;
		//			rec.RefIsHard = true;

		//			var filter2 = Builders<JobMapSectionRecord>.Filter.Eq(f => f.Id, rec.Id);

		//			var updateDefinition = Builders<JobMapSectionRecord>.Update
		//			.Set(f => f.JobId, rec.OwnerId)
		//			.Set(f => f.LastSavedUtc, rec.LastSaved)
		//			.Set(f => f.RefIsHard, true)
		//			.Unset(f => f.OwnerId)
		//			.Unset(f => f.LastSaved)
		//			.Unset(f => f.MapBlockOffset);

		//			var options = new UpdateOptions { IsUpsert = false };

		//			_ = Collection.UpdateOne(filter2, updateDefinition, options);
		//		}
		//	}
		//}

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
