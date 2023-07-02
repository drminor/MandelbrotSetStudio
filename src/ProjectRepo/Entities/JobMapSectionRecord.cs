using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
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
		JobOwnerType OwnerType

		//ObjectId SubdivisionId,         // TODO:_schema: Rename JobMapSectionRecord.SubdivisionId -> MapSectionSubdivisionId
		//bool RefIsHard					// TODO:_schema: Remove field RefIsHard
	)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		//public DateTime DateCreated => Id.CreationTime;
		//public DateTime DateCreatedUtc { get; set; }					// TODO_schema: Add DateCreatedUtc to JobMapSectionRecord

		//public JobType JobType { get; set; } = JobType.FullScale;
		//public SizeIntRecord BlockIndex { get; set; } = new SizeIntRecord(0, 0);                    // TODO_schema: Add MapCenterBlockOffset

		//public ObjectId MapSectionSubdivisionId { get; set; } = ObjectId.Empty;
		//public ObjectId JobSubdivisionId { get; set; } = ObjectId.Empty;

		//public ObjectId OriginalSourceSubdivisionId { get; set; } = ObjectId.Empty; // TODO_schema: Rename JobMapSectionRecord.OriginalSourceSubdivisionId -> JobSubdivisionId

		//public ObjectId OwnerId { get; set; } = ObjectId.Empty;
		//public DateTime LastSaved { get; set; }

		public bool Onfile => Id != ObjectId.Empty;


	}
}
