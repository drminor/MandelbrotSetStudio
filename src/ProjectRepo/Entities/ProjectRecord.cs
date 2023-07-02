using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(
		string Name, 
		string? Description, 
		ObjectId CurrentJobId,
		DateTime LastSavedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;
		public DateTime DateCreatedUtc { get; set; }    // TODO_schema: Add DateCreatedUtc to ProjectRecord


		public DateTime LastAccessedUtc { get; set; }
	}
}
