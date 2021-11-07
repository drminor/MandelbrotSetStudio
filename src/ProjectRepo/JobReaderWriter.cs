using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.MSetDatabase;
using System.Linq;

namespace ProjectRepo
{
	public class JobReaderWriter : MongoDbCollectionBase<Job>
	{
		private const string COLLECTION_NAME = "Jobs";

		public JobReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public Job Get(ObjectId jobId)
		{
			var filter = Builders<Job>.Filter.Eq("_id", jobId);
			var job = Collection.Find(filter).FirstOrDefault();

			return job;
		}

		public ObjectId[] GetJobIds(ObjectId projectId)
		{
			var filter = Builders<Job>.Filter.Eq("ProjectId", projectId);
			var jobs = Collection.Find(filter).ToList();

			// Get the _id values of the found documents
			var ids = jobs.Select(d => d.Id).ToArray();

			return ids;
		}

		public ObjectId Insert(Job job)
		{
			Collection.InsertOne(job);
			return job.Id;
		}

		public long? Delete(ObjectId jobId)
		{
			var filter = Builders<Job>.Filter.Eq("_id", jobId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteAllForProject(ObjectId projectId)
		{
			var filter = Builders<Job>.Filter.Eq("ProjectId", projectId);
			var jobs = Collection.Find(filter).ToList();

			// Get the _id values of the found documents
			var ids = jobs.Select(d => d.Id);

			// Create an $in filter for those ids
			var idsFilter = Builders<Job>.Filter.In(d => d.Id, ids);

			// Delete the documents using the $in filter
			var deleteResult = Collection.DeleteMany(idsFilter);
			return GetReturnCount(deleteResult);
		}

	}
}
