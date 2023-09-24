using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobMapSectionRecord
	(
		JobType JobType,
		ObjectId JobId,
		ObjectId MapSectionId,
		SizeIntRecord BlockIndex,
		bool IsInverted,
		DateTime DateCreatedUtc,
		DateTime LastSavedUtc,

		ObjectId MapSectionSubdivisionId,
		ObjectId JobSubdivisionId,
		OwnerType OwnerType
	)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public bool Onfile => Id != ObjectId.Empty;
	}
}
