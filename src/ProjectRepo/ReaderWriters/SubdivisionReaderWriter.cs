using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using ProjectRepo.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ProjectRepo
{
	public class SubdivisonReaderWriter : MongoDbCollectionBase<SubdivisionRecord>
	{
		private const string COLLECTION_NAME = "Subdivisions";

		public SubdivisonReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public IEnumerable<SubdivisionRecord> GetAll()
		{
			var filter = Builders<SubdivisionRecord>.Filter.Empty;
			var result = Collection.Find(filter).ToEnumerable();

			return result;
		}

		public SubdivisionRecord Get(ObjectId subdivisionId)
		{
			var filter = Builders<SubdivisionRecord>.Filter.Eq("_id", subdivisionId);
			var subdivisionRecord = Collection.Find(filter).FirstOrDefault();

			return subdivisionRecord;
		}

		public IList<SubdivisionRecord> Get(RSizeDto samplePointDelta, RVectorDto baseMapPosition)
		{
			var filter1 = Builders<SubdivisionRecord>.Filter.Eq("SamplePointDelta.Size.Width", samplePointDelta.Width);
			//var filter2 = Builders<SubdivisionRecord>.Filter.Eq("SamplePointDelta.Size.Height", samplePointDelta.Height);
			var filter2 = Builders<SubdivisionRecord>.Filter.Eq("SamplePointDelta.Size.Exponent", samplePointDelta.Exponent);

			var filter3 = Builders<SubdivisionRecord>.Filter.Eq("BaseMapPosition.Vector.X", baseMapPosition.X);
			var filter4 = Builders<SubdivisionRecord>.Filter.Eq("BaseMapPosition.Vector.Y", baseMapPosition.Y);
			var filter5 = Builders<SubdivisionRecord>.Filter.Eq("BaseMapPosition.Vector.Exponent", baseMapPosition.Exponent);


			//var filter4 = Builders<SubdivisionRecord>.Filter.Eq("BlockWidth", blockSize.Width);
			//var filter5 = Builders<SubdivisionRecord>.Filter.Eq("BlockHeight", blockSize.Height);

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

		public int GetMinExponent(IEnumerable<ObjectId> subdivisionIds)
		{
			var items = Collection.AsQueryable()
				.Where(c => subdivisionIds.Contains(c.Id)).ToList();

			if (items.Count > 0)
			{
				var result = items.Min(x => x.SamplePointDelta.Size.Exponent);
				return result;
			}
			else
			{
				return 0;
			}	
		}

	}
}
