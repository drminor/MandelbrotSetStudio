using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Subdivision
	{
		public ObjectId Id { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }

		public Subdivision(RSize samplePointDelta, SizeInt blockSize) : this(ObjectId.GenerateNewId(), samplePointDelta, blockSize)
		{ }

		public Subdivision(ObjectId id, RSize samplePointDelta, SizeInt blockSize)
		{
			Id = id;
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta ?? throw new ArgumentNullException(nameof(samplePointDelta));
		}

		public DateTime DateCreated => Id.CreationTime;

		public RPoint Position => RPoint.Zero;

	}
}
