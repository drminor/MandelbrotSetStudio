using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;

namespace ProjectRepo
{
	public class MapSectionReaderWriter : MongoDbCollectionBase<MapSectionRecord>
	{
		private const string COLLECTION_NAME = "MapSections";

		public MapSectionReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public MapSectionRecord Get(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var mapSectionRecord = Collection.Find(filter).FirstOrDefault();

			return mapSectionRecord;
		}

		public ObjectId GetmapSectionId(ObjectId projectId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("ProjectId", projectId);
			var mapSectionRecord = Collection.Find(filter).FirstOrDefault();

			return mapSectionRecord?.Id ?? ObjectId.Empty;
		}

		public ObjectId Insert(MapSectionRecord mapSectionRecord)
		{
			Collection.InsertOne(mapSectionRecord);
			return mapSectionRecord.Id;
		}

		public long? Delete(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}


	}
}
