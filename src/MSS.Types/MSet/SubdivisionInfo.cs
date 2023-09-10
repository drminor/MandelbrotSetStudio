using MongoDB.Bson;

namespace MSS.Types.MSet
{
	public class SubdivisionInfo
	{
		public SubdivisionInfo(ObjectId id, RValue samplePointDelta, BigVector baseMapPosition)
		{
			Id = id;
			SamplePointDelta = samplePointDelta;
			BaseMapPosition = baseMapPosition;
		}

		public ObjectId Id { get; init; }
		public RValue SamplePointDelta { get; init; }
		public BigVector BaseMapPosition { get; init; }

		//public SizeInt BlockSize => RMapConstants.BLOCK_SIZE;

		public string GetValueAsString()
		{
			if (BaseMapPosition.IsZero())
			{
				return $"{SamplePointDelta.Value} / {SamplePointDelta.Exponent * -1}";
			}
			else
			{
				return $"{SamplePointDelta.Value} / {SamplePointDelta.Exponent * -1} at baseMapPosition: {BaseMapPosition}.";
			}
		}

	}
}
