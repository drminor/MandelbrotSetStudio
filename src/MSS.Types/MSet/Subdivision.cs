using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public class Subdivision : ICloneable, IEquatable<Subdivision?>
	{
		public ObjectId Id { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }
		public BigVector BaseMapPosition { get; init; }

		public bool OnFile { get; init; }
		public DateTime DateCreatedUtc { get; init; }

		public Subdivision() : this(ObjectId.GenerateNewId(), new RSize(), new BigVector(), RMapConstants.BLOCK_SIZE, DateTime.UtcNow)
		{
			OnFile = false;
		}

		public Subdivision(RSize samplePointDelta, BigVector baseMapPositon) : this(ObjectId.GenerateNewId(), samplePointDelta, baseMapPositon, RMapConstants.BLOCK_SIZE, DateTime.UtcNow)
		{
			OnFile = false;
		}

		public Subdivision(ObjectId id, RSize samplePointDelta, BigVector baseMapPosition, SizeInt blockSize, DateTime dateCreatedUtc)
		{
			Id = id;
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta ?? throw new ArgumentNullException(nameof(samplePointDelta));
			BaseMapPosition = baseMapPosition ?? throw new ArgumentNullException(nameof(baseMapPosition));
			OnFile = true;
		}

		public DateTime DateCreated => Id.CreationTime;

		public RPoint Position => RPoint.Zero;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Subdivision Clone()
		{
			var result = new Subdivision(Id, SamplePointDelta.Clone(), BaseMapPosition.Clone(), BlockSize, DateCreatedUtc)
			{
				OnFile = OnFile
			};

			return result;
		}

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as Subdivision);
		}

		public bool Equals(Subdivision? other)
		{
			return other is not null &&
				   BlockSize.Equals(other.BlockSize) &&
				   EqualityComparer<RSize>.Default.Equals(SamplePointDelta, other.SamplePointDelta) &&
				   EqualityComparer<BigVector>.Default.Equals(BaseMapPosition, other.BaseMapPosition);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(BlockSize, SamplePointDelta, BaseMapPosition);
		}

		public static bool operator ==(Subdivision? left, Subdivision? right)
		{
			return EqualityComparer<Subdivision>.Default.Equals(left, right);
		}

		public static bool operator !=(Subdivision? left, Subdivision? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
