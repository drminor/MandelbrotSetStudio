using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.MSetDatabase;

namespace ProjectRepo
{
	class MapSectionReaderWriter
	{
		private const string COLLECTION_NAME = "Jobs";

		private readonly DbProvider _dbProvider;

		public MapSectionReaderWriter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

		public MapSection Get(ObjectId mapSectionId)
		{
			var filter = Builders<MapSection>.Filter.Eq("_id", mapSectionId);
			var mapSection = Collection.Find(filter).FirstOrDefault();

			return mapSection;
		}

		public ObjectId GetmapSectionId(ObjectId projectId)
		{
			var filter = Builders<MapSection>.Filter.Eq("ProjectId", projectId);
			var mapSection = Collection.Find(filter).FirstOrDefault();

			return mapSection?.Id ?? ObjectId.Empty;
		}

		public ObjectId Insert(MapSection mapSection)
		{
			Collection.InsertOne(mapSection);
			return mapSection.Id;
		}

		public long? Delete(ObjectId mapSectionId)
		{
			var filter = Builders<MapSection>.Filter.Eq("_id", mapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}

		private IMongoCollection<MapSection> Collection
		{
			get
			{
				IMongoDatabase db = _dbProvider.Database;
				IMongoCollection<MapSection> mapSectionCollection = db.GetCollection<MapSection>(COLLECTION_NAME);

				return mapSectionCollection;
			}
		}

		private long? GetReturnCount(DeleteResult deleteResult)
		{
			if (deleteResult.IsAcknowledged)
			{
				return deleteResult.DeletedCount;
			}

			return null;
		}
	}
}
