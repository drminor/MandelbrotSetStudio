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
			var filter = Builders<ProjectRecord>.Filter.Empty;

			var projectRecords = Collection.Find(filter).ToEnumerable();
			return projectRecords;
		}

		public IEnumerable<ObjectId> GetAllIds()
		{
			var projection1 = Builders<ProjectRecord>.Projection.Expression
				(
					p => p.Id
				);

			var filter = Builders<ProjectRecord>.Filter.Empty;
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}

		public ProjectRecord? Get(ObjectId projectId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);
			var projectRecord = Collection.Find(filter).FirstOrDefault();

			return projectRecord;
		}

		public bool TryGet(ObjectId projectId, [MaybeNullWhen(false)] out ProjectRecord projectRecord)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);
			projectRecord = Collection.Find(filter).FirstOrDefault();

			return projectRecord != null;
		}

		public bool TryGet(string name, [MaybeNullWhen(false)] out ProjectRecord projectRecord)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("Name", name);
			projectRecord = Collection.Find(filter).FirstOrDefault();

			return projectRecord != null;
		}

		public bool ProjectExists(string name, [MaybeNullWhen(false)] out ObjectId projectId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("Name", name);
			var result = Collection.Find(filter).FirstOrDefault();
			projectId = result?.Id ?? ObjectId.Empty;

			return result != null;
		}

		public async Task<ProjectRecord> GetAsync(string name)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("Name", name);
			var projectRecord = await Collection.FindAsync(filter);

			return projectRecord.FirstOrDefault();
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

		public void UpdateCurrentJobId(ObjectId projectId, ObjectId? currentJobId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);

			var updateDefinition = Builders<ProjectRecord>.Update
				.Set(u => u.CurrentJobId, currentJobId)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		//public void UpdateCurrentColorBandSetId(ObjectId projectId, ObjectId? currentJobId)
		//{
		//	var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);

		//	var updateDefinition = Builders<ProjectRecord>.Update
		//		.Set(u => u.CurrentJobId, currentJobId)
		//		.Set(u => u.LastSavedUtc, DateTime.UtcNow);

		//	_ = Collection.UpdateOne(filter, updateDefinition);
		//}

		public long? Delete(ObjectId projectId)
		{
			var filter = Builders<ProjectRecord>.Filter.Eq("_id", projectId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		//public long RemoveCurrentColorBandSetProp()
		//{
		//	var filter = Builders<ProjectRecord>.Filter.Empty;
		//	var updateDefinition = Builders<ProjectRecord>.Update
		//		.Unset(f => f.CurrentColorBandSetId);

		//	var options = new UpdateOptions { IsUpsert = false };

		//	var updateResult = Collection.UpdateMany(filter, updateDefinition, options);

		//	return GetReturnCount(updateResult) ?? 0;
		//}
	}
}
