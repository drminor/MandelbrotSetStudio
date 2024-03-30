using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,

		ObjectId OwnerId,
		OwnerType OwnerType,

		//ObjectId SubDivisionId,     // TODO_schema: Delete the JobRecord.SubdivisionId
		string Label,
		int TransformType,

		MapAreaInfo2Record MapCenterAndDeltaRecord, // TODO_schema: Rename MapAreaInfo2Record MapCenterAndDeltaRecord
		string TransformTypeString,

		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		//ObjectId ColorBandSetId,
		string ColorBandSetName,
		int? ColorBandSetVersion,
		MapCalcSettings MapCalcSettings,

		DateTime LastSavedUtc,
		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		//public DateTime DateCreated => Id.CreationTime;
		//public OwnerType OwnerType { get; set; } = OwnerType.None;
		//public MapAreaInfo2Record MapCenterAndDeltaRecord { get; set; } = MapAreaInfo2Record.CreateEmpty();

		public DateTime DateCreatedUtc { get; set; }    // TODO_schema: Add DateCreatedUtc to JobRecord
		//public DateTime? LastSaved { get; set; }        // TODO_schema: Remove the LastSaved from all Jobs on file.


		public ObjectId ColorBandSetId { get; set; } = ObjectId.Empty;

		//public string ColorBandSetName { get; set; } = string.Empty;	// TODO_schema: Add ColorBandSetName and Version to JobRecord
		//public int? ColorBandSetVersion { get; set; }                   // And removed the ColorBandSetId

		//public OwnerType JobOwnerType { get; set; } = OwnerType.None;
		//public OwnerType OwnerType { get; set; } = OwnerType.None;


		//public MapAreaInfo2Record? MapAreaInfo2Record { get; set; } = null;

		//public ObjectId? SubDivisionId { get; set; } = ObjectId.Empty;

	}

}
