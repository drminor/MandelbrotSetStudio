using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MSS.Common.MSetDatabase;
using MSS.Types;
using MSS.Types.Base;
using System;

namespace ProjectRepo
{
	public class MongoDbCollectionBase<T> : IMongoDbCollection<T>
    {
        private readonly DbProvider _dbProvider;
        private readonly string _collectionName;

        private Lazy<IMongoCollection<T>> _collectionLazy;

        public MongoDbCollectionBase(DbProvider dbProvider, string collectionName)
        {
            _dbProvider = dbProvider;
            _collectionName = collectionName;

            _collectionLazy = new Lazy<IMongoCollection<T>>(() => Database.GetCollection<T>(_collectionName), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

   //         RegisterMapIfNeeded<BWRectangle>();
			//RegisterMapIfNeeded<BigIntegerWrapper>();
		}

		public IMongoDatabase Database => _dbProvider.Database;
        public IMongoCollection<T> Collection => _collectionLazy.Value;

		protected virtual long? GetReturnCount(DeleteResult deleteResult)
		{
			if (deleteResult.IsAcknowledged)
			{
				return deleteResult.DeletedCount;
			}

			return null;
		}

		private void RegisterMapIfNeeded<TClass>()
		{
			if (!BsonClassMap.IsClassMapRegistered(typeof(TClass)))
			{
				BsonClassMap.RegisterClassMap<TClass>();
			}
		}

		#region unused

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

