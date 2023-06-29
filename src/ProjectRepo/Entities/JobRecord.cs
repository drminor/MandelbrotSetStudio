using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,

		ObjectId OwnerId,
		JobOwnerType JobOwnerType,

		ObjectId SubDivisionId,     // This is not used when reading. When writing its value comes from the MapAreaInfo2Record.Subdivision.Id
		string Label,
		int TransformType,

		MapAreaInfo2Record MapAreaInfo2Record,
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

		
		public DateTime? LastSaved { get; set; } // TODO: Remove the LastSaved from all Jobs on file.

		public IterationUpdateRecord[]? IterationUpdates { get; set; }
		public ColorMapUpdateRecord[]? ColorMapUpdates { get; set; }
	}

}
