using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common;
using System;

namespace ProjectRepo
{
	public class MongoDbCollectionBase<T> : IMongoDbCollection<T>
    {
		private static readonly long BYTES_MB = 1024 * 1024;

        private readonly DbProvider _dbProvider;
        private readonly string _collectionName;

        private Lazy<IMongoCollection<T>> _collectionLazy;

        public MongoDbCollectionBase(DbProvider dbProvider, string collectionName)
        {
            _dbProvider = dbProvider;
            _collectionName = collectionName;

            _collectionLazy = new Lazy<IMongoCollection<T>>(() => Database.GetCollection<T>(_collectionName), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public IMongoDatabase Database => _dbProvider.Database;
        public IMongoCollection<T> Collection => _collectionLazy.Value;

		public IMongoCollection<BsonDocument> BsonDocumentCollection => Database.GetCollection<BsonDocument>(_collectionName);

		public virtual bool CreateCollection()
		{
			var cNames = _dbProvider.Database.ListCollectionNames().ToList();

			if (!cNames.Contains(_collectionName))
			{
				_dbProvider.Database.CreateCollection(_collectionName);
				return true;
			}
			else
			{
				return false;
			}
		}

		public void DropCollection()
		{
			_dbProvider.Database.DropCollection(_collectionName);
		}

		protected virtual long? GetReturnCount(DeleteResult deleteResult)
		{
			if (deleteResult.IsAcknowledged)
			{
				return deleteResult.DeletedCount;
			}

			return null;
		}

		protected virtual long? GetReturnCount(UpdateResult updateResult)
		{
			if (updateResult.IsAcknowledged)
			{
				return updateResult.ModifiedCount;
			}

			return null;
		}

		public virtual long GetSizeOfCollectionInMB()
		{
			var result = 0L;

			// NOTE: Need to add 22 bytes * total number of documents to get the actual size.
			var sizeOnServer = Collection
				.Aggregate()
				.AppendStage<BsonDocument>("{ $group: { _id : null, collectionSize: { $sum: { $bsonSize: '$$ROOT' } } } }")
				.FirstOrDefault();

			var collectionSize = sizeOnServer["collectionSize"];

			if (collectionSize.IsInt32)
			{
				result = collectionSize.AsInt32 / BYTES_MB;

			}
			else if (collectionSize.IsInt64)
			{
				result = collectionSize.AsInt64 / BYTES_MB;
			}
			else
			{
				result = -1;
			}

			return result;
		}

		public virtual int GetSizeOfDocZero()
		{
			var documentSizeOnServer = Collection
				.Aggregate()
				.AppendStage<BsonDocument>("{ $project : { documentSize : { $bsonSize : '$$ROOT' }, _id : 0 } }")
				.FirstOrDefault();

			var docSize = documentSizeOnServer["documentSize"].AsInt32;
			return docSize;
		}

		#region unused

		//private void RegisterMapIfNeeded<TClass>()
		//{
		//	if (!BsonClassMap.IsClassMapRegistered(typeof(TClass)))
		//	{
		//		BsonClassMap.RegisterClassMap<TClass>();
		//	}
		//}

		//public ObjectId GetProjectId(string name)
		//{
		//	var filter = Builders<BsonDocument>.Filter.Eq("Name", name);
		//	var bsonRecord = Document.Find(filter).FirstOrDefault();

		//	return GetRecordId(bsonRecord);
		//}

		//private IMongoCollection<BsonDocument> Document
		//{
		//	get
		//	{
		//		IMongoDatabase db = _dbProvider.Database;
		//		var document = db.GetCollection<BsonDocument>(COLLECTION_NAME);

		//		return document;
		//	}
		//}

		//private ObjectId GetRecordId(BsonDocument bsonElements)
		//{
		//	ObjectId result = bsonElements == null ? ObjectId.Empty : GetObjectId(bsonElements.GetValue("_id"));
		//	return result;
		//}

		//private ObjectId GetObjectId(BsonValue bsonValue)
		//{
		//	ObjectId result = bsonValue == null ? ObjectId.Empty : bsonValue.AsObjectId;
		//	return result;
		//}

		#endregion
	}
}

