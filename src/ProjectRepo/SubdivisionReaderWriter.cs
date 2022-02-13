using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using MSS.Types.DataTransferObjects;
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

		public IList<SubdivisionRecord> Get(RPointDto _, RSizeDto samplePointDelta, SizeInt blockSize)
		{
			var filter1 = Builders<SubdivisionRecord>.Filter.Eq("SamplePointDelta.SizeDto.Exponent", samplePointDelta.Exponent);
			var filter2 = Builders<SubdivisionRecord>.Filter.Eq("SamplePointDelta.SizeDto.Width", samplePointDelta.Width);
			var filter3 = Builders<SubdivisionRecord>.Filter.Eq("SamplePointDelta.SizeDto.Height", samplePointDelta.Height);

			var filter4 = Builders<SubdivisionRecord>.Filter.Eq("BlockWidth", blockSize.Width);
			var filter5 = Builders<SubdivisionRecord>.Filter.Eq("BlockHeight", blockSize.Height);

			var subdivisionRecords = Collection.Find(filter1 & filter2 & filter3 & filter4 & filter5).ToList();

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
