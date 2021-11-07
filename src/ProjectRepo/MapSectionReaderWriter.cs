using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.MSetDatabase;

namespace ProjectRepo
{
	public class MapSectionReaderWriter : MongoDbCollectionBase<MapSection>
	{
		private const string COLLECTION_NAME = "MapSections";

		public MapSectionReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

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


	}
}
