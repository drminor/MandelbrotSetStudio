using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProjectRepo
{
	public class MapSectionReaderWriterV1 : MongoDbCollectionBase<MapSectionRecordV1>
	{
		private const string COLLECTION_NAME = "MapSections";

		public MapSectionReaderWriterV1(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public async Task<MapSectionRecordV1?> GetAsync (ObjectId subdivisionId, BigVectorDto blockPosition)
		{
			var filter1 = Builders<MapSectionRecordV1>.Filter.Eq("SubdivisionId", subdivisionId);
			var filter2 = Builders<MapSectionRecordV1>.Filter.Eq("BlockPosXLo", blockPosition.X[1]);
			var filter3 = Builders<MapSectionRecordV1>.Filter.Eq("BlockPosYLo", blockPosition.Y[1]);
			var filter4 = Builders<MapSectionRecordV1>.Filter.Eq("BlockPosXHi", blockPosition.X[0]);
			var filter5 = Builders<MapSectionRecordV1>.Filter.Eq("BlockPosYHi", blockPosition.Y[0]);

			var mapSectionRecord = await Collection.FindAsync(filter1 & filter2 & filter3 & filter4 & filter5);

			var itemsFound = mapSectionRecord.ToList();

			if (itemsFound.Count > 0)
			{
				var result = itemsFound[0];
				result.LastAccessed = DateTime.UtcNow;
				return result;
			}
			else
			{
				//Debug.WriteLine("MapSection Not found.");
				return default;
			}
		}



	}
}
