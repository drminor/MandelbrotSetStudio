using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	public record PosterRecord(

		string Name, 
		string? Description,
		ObjectId SourceJobId,
		ObjectId CurrentJobId, 

		VectorIntRecord DisplayPosition,
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
		public int OffsetFromCenterX { get; set; }

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0)]
		public int OffsetFromCenterY { get; set; }

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0)]
		public int Width { get; set; }

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0)]
		public int Height { get; set; }
	}
}
