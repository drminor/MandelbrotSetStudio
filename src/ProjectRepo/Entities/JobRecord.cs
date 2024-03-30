using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,

		ObjectId OwnerId,
		OwnerType JobOwnerType,	// TODO_schema: Rename JobOwnerType -> OwnerType

		ObjectId SubDivisionId,     // TODO_schema: Delete the JobRecord.SubdivisionId
		string Label,
		int TransformType,

		MapAreaInfo2Record MapAreaInfo2Record, // TODO_schema: Rename MapAreaInfo2Record MapCenterAndDeltaRecord
		string TransformTypeString,

		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		ObjectId ColorBandSetId,
		MapCalcSettings MapCalcSettings,

		DateTime LastSavedUtc,
		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public DateTime DateCreatedUtc { get; set; }	// TODO_schema: Add DateCreatedUtc to JobRecord
		public DateTime? LastSaved { get; set; }        // TODO_schema: Remove the LastSaved from all Jobs on file.

		public string ColorBandSetName { get; set; } = string.Empty;	// TODO_schema: Add ColorBandSetName and Version to JobRecord
		public int? ColorBandSetVersion { get; set; }					// And removed the ColorBandSetId



	}

}
