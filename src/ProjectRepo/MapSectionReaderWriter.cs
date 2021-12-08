using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using ProjectRepo.Entities;
using System.Threading.Tasks;

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
			var mapSectionRecord = Collection.Find(filter);

			return mapSectionRecord.FirstOrDefault();
		}

		public async Task<MapSectionRecord> GetAsync(ObjectId mapSectionId)
		{
			var filter = Builders<MapSectionRecord>.Filter.Eq("_id", mapSectionId);
			var mapSectionRecord = await Collection.FindAsync(filter);

			return mapSectionRecord.FirstOrDefault();
		}

		public async Task<MapSectionRecord> GetAsync (ObjectId subdivisionId, PointInt blockPosition)
		{
			var filter1 = Builders<MapSectionRecord>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecord>.Filter.Eq("BlockPositionX", blockPosition.X);
			var filter3 = Builders<MapSectionRecord>.Filter.Eq("BlockPositionY", blockPosition.Y);

			var mapSectionRecord = await Collection.FindAsync(filter1 & filter2 & filter3);

			return mapSectionRecord.FirstOrDefault();
		}

		public async Task<ObjectId> InsertAsync(MapSectionRecord mapSectionRecord)
		{
			await Collection.InsertOneAsync(mapSectionRecord);
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
