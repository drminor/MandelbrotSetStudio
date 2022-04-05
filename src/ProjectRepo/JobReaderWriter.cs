using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
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

		public ObjectId Insert(JobRecord jobRecord)
		{
			Collection.InsertOne(jobRecord);
			return jobRecord.Id;
		}

		public void UpdateJobsParent(ObjectId jobId, ObjectId? parentId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.ParentJobId, parentId);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateJobDetails(JobRecord jobRecord)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobRecord.Id);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.MSetInfo, jobRecord.MSetInfo)
				.Set(u => u.CanvasSizeInBlocks, jobRecord.CanvasSizeInBlocks)
				.Set(u => u.MapBlockOffset, jobRecord.MapBlockOffset)
				.Set(u => u.CanvasControlOffset, jobRecord.CanvasControlOffset);

			_ = Collection.UpdateOne(filter, updateDefinition);
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

		#region Aggregate Results

		public IEnumerable<JobModel1> GetJobInfos(ObjectId projectId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => new JobModel1(p.Id.CreationTime, p.TransformType, p.SubDivisionId, p.MSetInfo.CoordsRecord.CoordsDto.Exponent)
				);

			//List models = collection.Find(_ => true).Project(projection1).ToList();

			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var jobInfos = Collection.Find(filter).Project(projection1).ToEnumerable();

			return jobInfos;
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

		#endregion

	}
}
