using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Subdivision : ICloneable
	{
		public ObjectId Id { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }
		public RVector BaseMapPosition { get; init; }

		public Subdivision()
		{
			SamplePointDelta = new RSize();
			BaseMapPosition = new RVector();
		}

		public Subdivision(RSize samplePointDelta, RVector baseMapPositon) : this(ObjectId.GenerateNewId(), samplePointDelta, baseMapPositon, RMapConstants.BLOCK_SIZE)
		{ }

		public Subdivision(RSize samplePointDelta, RVector baseMapPositon, SizeInt blockSize) : this(ObjectId.GenerateNewId(), samplePointDelta, baseMapPositon, blockSize)
		{ }

		public Subdivision(ObjectId id, RSize samplePointDelta, RVector baseMapPosition, SizeInt blockSize)
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
