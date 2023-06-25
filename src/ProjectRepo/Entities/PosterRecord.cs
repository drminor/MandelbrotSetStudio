using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using System;

namespace ProjectRepo.Entities
{
	public record PosterRecord(

		string Name, 
		string? Description,
		ObjectId SourceJobId,
		ObjectId CurrentJobId, 

		VectorDblRecord DisplayPosition,
		double DisplayZoom,

		DateTime DateCreatedUtc,
		DateTime LastSavedUtc,
		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0)]
		public int Width { get; set; }

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0)]
		public int Height { get; set; }

		public SizeInt PosterSize
		{
			get
			{
				if (Width == 0 || Height == 0)
				{
					return new SizeInt(1024);
				}
				else
				{
					return new SizeInt(Width, Height);
				}
			}
		}

	}
}
