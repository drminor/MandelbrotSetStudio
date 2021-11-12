using MongoDB.Bson;
using MongoDB.Driver;
using ProjectRepo.Entities;
using System.Collections.Generic;

namespace ProjectRepo
{
	public class SubdivisonReaderWriter : MongoDbCollectionBase<SubdivisionRecord>
	{
		private const string COLLECTION_NAME = "SubDivisions";

		public SubdivisonReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public SubdivisionRecord Get(ObjectId subdivisionId)
		{
			var filter = Builders<SubdivisionRecord>.Filter.Eq("_id", subdivisionId);
			var subdivisionRecord = Collection.Find(filter).FirstOrDefault();

			return subdivisionRecord;
		}

		public IList<SubdivisionRecord> Get(int scale)
		{
			var filter = Builders<SubdivisionRecord>.Filter.Eq("Position.PointDto.Exponent", scale);
			var subdivisionRecords = Collection.Find(filter).ToList();

			return subdivisionRecords;
		}

		public ObjectId Insert(SubdivisionRecord subdivisionRecord)
		{
			Collection.InsertOne(subdivisionRecord);
			return subdivisionRecord.Id;
		}

		public long? Delete(ObjectId subDivisionId)
		{
			var filter = Builders<SubdivisionRecord>.Filter.Eq("_id", subDivisionId);
			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}
	}
}
