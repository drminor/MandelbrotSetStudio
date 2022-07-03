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

		public IEnumerable<ObjectId> GetJobIds(ObjectId projectId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => p.Id
				);

			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}

		public ObjectId Insert(JobRecord jobRecord)
		{
			Collection.InsertOne(jobRecord);
			return jobRecord.Id;
		}

		public void UpdateJobDetails(JobRecord jobRecord)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobRecord.Id);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.ProjectId, jobRecord.ProjectId)
				.Set(u => u.ParentJobId, jobRecord.ParentJobId)
				.Set(u => u.IsPreferredChild, jobRecord.IsPreferredChild)
				.Set(u => u.MSetInfo, jobRecord.MSetInfo)
				.Set(u => u.ColorBandSetId, jobRecord.ColorBandSetId)
				.Set(u => u.CanvasSizeInBlocks, jobRecord.CanvasSizeInBlocks)
				.Set(u => u.MapBlockOffset, jobRecord.MapBlockOffset)
				.Set(u => u.CanvasControlOffset, jobRecord.CanvasControlOffset)
				.Set(u => u.LastSaved, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateJobsColorBandSet(ObjectId jobId, int targetIterations, ObjectId colorBandSetId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

			var updateDefinition = Builders<JobRecord>.Update
				.Set(u => u.MSetInfo.MapCalcSettings.TargetIterations, targetIterations)
				.Set(u => u.ColorBandSetId, colorBandSetId)
				.Set(u => u.LastSaved, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}


		//public void UpdateJobsProject(ObjectId jobId, ObjectId projectId)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Set(u => u.ProjectId, projectId)
		//		.Set(u => u.LastSaved, DateTime.UtcNow);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		//public void UpdateJobsParent(ObjectId jobId, ObjectId? parentId, bool isPreferredChild)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);

		//	var updateDefinition = Builders<JobRecord>.Update
		//		.Set(u => u.ParentJobId, parentId)
		//		.Set(u => u.IsPreferredChild, isPreferredChild)
		//		.Set(u => u.LastSaved, DateTime.UtcNow);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		public long? Delete(ObjectId jobId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("_id", jobId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		public long? DeleteAllForProject(ObjectId projectId)
		{
			var ids = GetJobIds(projectId);

			// Create an $in filter for those ids
			var idsFilter = Builders<JobRecord>.Filter.In(d => d.Id, ids);

			// Delete the documents using the $in filter
			var deleteResult = Collection.DeleteMany(idsFilter);
			return GetReturnCount(deleteResult);
		}

		#region Aggregate Results

		public IEnumerable<ObjectId> GetAllReferencedColorBandSetIds()
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => p.ColorBandSetId
				);

			//List models = collection.Find(_ => true).Project(projection1).ToList();

			var filter = Builders<JobRecord>.Filter.Empty;
			var colorBandSetIds = Collection.Find(filter).Project(projection1).ToEnumerable().Distinct();

			return colorBandSetIds;
		}

		public IEnumerable<JobInfoRecord> GetJobInfos(ObjectId projectId)
		{
			var projection1 = Builders<JobRecord>.Projection.Expression
				(
					p => new JobInfoRecord(p.Id.CreationTime, p.TransformType, p.SubDivisionId, p.MSetInfo.CoordsRecord.CoordsDto.Exponent)
				);

			//List models = collection.Find(_ => true).Project(projection1).ToList();

			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var jobInfos = Collection.Find(filter).Project(projection1).ToEnumerable();

			return jobInfos;
		}

		//public DateTime GetLastSaveTime(ObjectId projectId)
		//{
		//	var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
		//	var jobs = Collection.Find(filter).ToList();

		//	if (jobs.Count < 1)
		//	{
		//		return DateTime.MinValue;
		//	}
		//	else
		//	{
		//		var result = jobs.Max(x => x.Id.CreationTime);
		//		return result;
		//	}
		//}

		public void AddColorBandSetIdByProject(ObjectId projectId, ObjectId colorBandSetId)
		{
			var filter = Builders<JobRecord>.Filter.Eq("ProjectId", projectId);
			var updateDefinition = Builders<JobRecord>.Update.Set("ColorBandSetId", colorBandSetId);
			var options = new UpdateOptions { IsUpsert = false };

			_ = Collection.UpdateMany(filter, updateDefinition, options);
		}

		public void AddIsPreferredChildToAllJobs()
		{
			var filter = Builders<JobRecord>.Filter.Empty;
			var updateDefinition = Builders<JobRecord>.Update.Set("IsPreferredChild", true);
			var options = new UpdateOptions { IsUpsert = false };

			_ = Collection.UpdateMany(filter, updateDefinition, options);
		}

		#endregion

	}
}
