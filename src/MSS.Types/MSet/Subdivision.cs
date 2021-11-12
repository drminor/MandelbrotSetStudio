using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Subdivision
	{
		public ObjectId Id { get; init; }
		public RPoint Position { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }

		public Subdivision(ObjectId id, RPoint position, SizeInt blockSize, RSize samplePointDelta)
		{
			Id = id;
			Position = position ?? throw new ArgumentNullException(nameof(position));
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta ?? throw new ArgumentNullException(nameof(samplePointDelta));
		}

		public DateTime DateCreated => Id.CreationTime;

	}
}
