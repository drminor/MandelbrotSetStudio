using MongoDB.Bson;

namespace MSS.Types.MSet
{
	public class SubdivisionInfo
	{
		public SubdivisionInfo(ObjectId id, RValue samplePointDelta)
		{
			Id = id;
			SamplePointDelta = samplePointDelta;
		}

		public ObjectId Id { get; init; }
		public RValue SamplePointDelta { get; init; }

		public string GetValueAsString()
		{
			return $"{SamplePointDelta.Value} / {SamplePointDelta.Exponent * -1}";
		}

	}
}
