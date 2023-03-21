using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Subdivision : ICloneable
	{
		public ObjectId Id { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }
		public BigVector BaseMapPosition { get; init; }

		public Subdivision()
		{
			SamplePointDelta = new RSize();
			BaseMapPosition = new BigVector();
		}

		public Subdivision(RSize samplePointDelta, BigVector baseMapPositon) : this(ObjectId.GenerateNewId(), samplePointDelta, baseMapPositon, RMapConstants.BLOCK_SIZE)
		{ }

		public Subdivision(RSize samplePointDelta, BigVector baseMapPositon, SizeInt blockSize) : this(ObjectId.GenerateNewId(), samplePointDelta, baseMapPositon, blockSize)
		{ }

		public Subdivision(ObjectId id, RSize samplePointDelta, BigVector baseMapPosition, SizeInt blockSize)
		{
			Id = id;
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta ?? throw new ArgumentNullException(nameof(samplePointDelta));
			BaseMapPosition = baseMapPosition ?? throw new ArgumentNullException(nameof(baseMapPosition));
		}

		public DateTime DateCreated => Id.CreationTime;

		public RPoint Position => RPoint.Zero;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Subdivision Clone()
		{
			return new Subdivision(Id, SamplePointDelta.Clone(), BaseMapPosition.Clone(), BlockSize);
		}
	}
}
