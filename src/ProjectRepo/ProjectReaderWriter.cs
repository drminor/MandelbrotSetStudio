using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ProjectRepo
{
	public class ProjectReaderWriter : MongoDbCollectionBase<ProjectRecord>
	{
		private const string COLLECTION_NAME = "Projects";

		public ProjectReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public IEnumerable<ProjectRecord> GetAll()
		{
			var projectRecords = Collection.Find(_ => true).ToEnumerable();
			return projectRecords;
		}

		public ProjectRecord Get(ObjectId projectId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);
			var projectRecord = Collection.Find(filter).FirstOrDefault();

			return projectRecord;
		}

		public ProjectRecord Get(string name)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("Name", name);
			var projectRecord = Collection.Find(filter).FirstOrDefault();

			return projectRecord;
		}

		public bool TryGet(string name, [MaybeNullWhen(false)] out ProjectRecord projectRecord)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("Name", name);
			projectRecord = Collection.Find(filter).FirstOrDefault();

			return projectRecord != null;
		}

		public async Task<ProjectRecord> GetAsync(string name)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("Name", name);
			var projectRecord = await Collection.FindAsync(filter);

			return projectRecord.FirstOrDefault();
		}

		public ObjectId GetProjectId(string name)
		{
			var projectRecord = Get(name);

			return projectRecord?.Id ?? ObjectId.Empty;
		}

		public ObjectId Insert(ProjectRecord projectRecord)
		{
			Collection.InsertOne(projectRecord);
			return projectRecord.Id;
		}

		public void UpdateName(ObjectId projectId, string name)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);

			var updateDefinition = Builders<ProjectRecord>.Update
				.Set(u => u.Name, name)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateDescription(ObjectId projectId, string? description)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);

			var updateDefinition = Builders<ProjectRecord>.Update
				.Set(u => u.Description, description)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateCurrentCbsId(ObjectId projectId, ObjectId currentColorBandSetId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);

			var updateDefinition = Builders<ProjectRecord>.Update
				.Set(u => u.CurrentColorBandSetId, currentColorBandSetId)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateCurrentJobId(ObjectId projectId, ObjectId? currentJobId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);

			var updateDefinition = Builders<ProjectRecord>.Update
				.Set(u => u.CurrentJobId, currentJobId)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public long? Delete(ObjectId projectId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}
	}
}
