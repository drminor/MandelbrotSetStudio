using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(
		RSizeRecord SamplePointDelta,
		RVectorRecord BaseMapPosition,
		SizeIntRecord BlockSize
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;
	}
}
