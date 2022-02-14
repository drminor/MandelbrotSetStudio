using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;
using System;
using System.Linq;

namespace ProjectRepo
{
	public class JobReaderWriter : MongoDbCollectionBase<JobRecord>
	{
		private const string COLLECTION_NAME = "Jobs";

		public JobReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public JobRecord Get(ObjectId jobId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);
			var jobRecord = Collection.Find(filter).FirstOrDefault();

			return jobRecord;
		}

		public ObjectId[] GetJobIds(ObjectId projectId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var jobs = Collection.Find(filter).ToList();

			// Get the _id values of the found documents
			var ids = jobs.Select(d => d.Id).ToArray();

			return ids;
		}

		public DateTime GetLastSaveTime(ObjectId projectId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var jobs = Collection.Find(filter).ToList();

			if (jobs.Count < 1)
			{
				return DateTime.MinValue;
			}
			else
			{
				var result = jobs.Max(x => x.Id.CreationTime);
				return result;
			}
		}

		public ObjectId Insert(JobRecord jobRecord)
		{
			Collection.InsertOne(jobRecord);
			return jobRecord.Id;
		}

		public long? Delete(ObjectId jobId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteAllForProject(ObjectId projectId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var jobs = Collection.Find(filter).ToList();

			// Get the _id values of the found documents
			var ids = jobs.Select(d => d.Id);

			// Create an $in filter for those ids
			var idsFilter = Builders<JobRecord>.Filter.In(d => d.Id, ids);

			// Delete the documents using the $in filter
			var deleteResult = Collection.DeleteMany(idsFilter);
			return GetReturnCount(deleteResult);
		}

	}
}
