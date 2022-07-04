using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ProjectRepo
{
	public class PosterReaderWriter : MongoDbCollectionBase<PosterRecord>
	{
		private const string COLLECTION_NAME = "Posters";

		public PosterReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public IEnumerable<PosterRecord> GetAll()
		{
			var filter = Builders<PosterRecord>.Filter.Empty;

			var posterRecords = Collection.Find(filter).ToEnumerable();
			return posterRecords;
		}

		public IEnumerable<ObjectId> GetAllIds()
		{
			var projection1 = Builders<PosterRecord>.Projection.Expression
				(
					p => p.Id
				);

			var filter = Builders<PosterRecord>.Filter.Empty;
			var result = Collection.Find(filter).Project(projection1).ToEnumerable();

			return result;
		}


		public PosterRecord Get(ObjectId posterId)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("_id", posterId);
			var posterRecord = Collection.Find(filter).FirstOrDefault();

			return posterRecord;
		}


		public bool TryGet(ObjectId posterId, [MaybeNullWhen(false)] out PosterRecord posterRecord)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("_id", posterId);
			posterRecord = Collection.Find(filter).FirstOrDefault();

			return posterRecord != null;
		}

		public bool TryGet(string name, [MaybeNullWhen(false)] out PosterRecord posterRecord)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("Name", name);
			posterRecord = Collection.Find(filter).FirstOrDefault();

			return posterRecord != null;
		}

		public bool ExistsWithName(string name)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("Name", name);
			var result = Collection.Find(filter).Any();

			return result;
		}

		public ObjectId Insert(PosterRecord posterRecord)
		{
			Collection.InsertOne(posterRecord);
			return posterRecord.Id;
		}

		public void UpdateMapArea(PosterRecord posterRecord)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("_id", posterRecord.Id);

			var updateDefinition = Builders<PosterRecord>.Update
				.Set(u => u.JobAreaInfoRecord, posterRecord.JobAreaInfoRecord)
				.Set(u => u.DisplayPosition, posterRecord.DisplayPosition)
				.Set(u => u.DisplayZoom, posterRecord.DisplayZoom);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateName(ObjectId posterId, string name)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("_id", posterId);

			var updateDefinition = Builders<PosterRecord>.Update
				.Set(u => u.Name, name)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public void UpdateDescription(ObjectId posterId, string? description)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("_id", posterId);

			var updateDefinition = Builders<PosterRecord>.Update
				.Set(u => u.Description, description)
				.Set(u => u.LastSavedUtc, DateTime.UtcNow);

			_ = Collection.UpdateOne(filter, updateDefinition);
		}

		public long? Delete(ObjectId posterId)
		{
			var filter = Builders<PosterRecord>.Filter.Eq("_id", posterId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}



	}
}

