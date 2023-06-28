using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using System;

namespace ProjectRepo.Entities
{
	public record JobMapSectionRecord 
	(
		ObjectId MapSectionId,
		ObjectId SubdivisionId,
		ObjectId JobId,
		JobOwnerType OwnerType,
		bool IsInverted,
		DateTime LastSavedUtc,
		bool RefIsHard
	)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;
	}
}
