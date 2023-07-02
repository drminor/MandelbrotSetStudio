using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using System;

namespace ProjectRepo.Entities
{
	public record JobMapSectionRecord
	(
		ObjectId JobId,
		ObjectId MapSectionId,
		ObjectId SubdivisionId,         // TODO_schema: Rename JobMapSectionRecord.SubdivisionId -> MapSectionSubdivisionId
		JobOwnerType OwnerType,
		bool IsInverted,
		DateTime LastSavedUtc,
		bool RefIsHard					// TODO_schema: Remove field RefIsHard
	)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;
		public DateTime DateCreatedUtc { get; set; }    // TODO_schema: Add DateCreatedUtc to JobMapSectionRecord


		public bool Onfile => Id != ObjectId.Empty;

		public ObjectId OriginalSourceSubdivisionId { get; set; } = ObjectId.Empty; // TODO_schema: Rename JobMapSectionRecord.OriginalSourceSubdivisionId -> JobSubdivisionId

	}
}
